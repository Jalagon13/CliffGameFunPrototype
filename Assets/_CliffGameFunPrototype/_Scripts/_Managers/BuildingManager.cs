using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    [Serializable]
    public enum SelectedBuildType
    {
        Floor,
        Wall,
    }

    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance;

        public Action OnGhostSnap;
        public Action OnGhostUnsnap;

        [Header("Build Objects")]
        [SerializeField] private List<GameObject> _floorObjects = new();
        [SerializeField] private List<GameObject> _wallObjects = new();

        [Header("Build Settings")]
        [SerializeField] private SelectedBuildType _currentBuildType;
        [SerializeField] private LayerMask _connectorLayerMask;
        [SerializeField] private LayerMask _playerLayerMask;
        [SerializeField] private float _buildRange = 4f;
        [SerializeField] private InventoryItem[] _itemsNeededForBuilding;
        private Connector _lastSnappedConnector = null; // Tracks the last connector the ghost was snapped to, for snap/unsnap events

        [Header("Destroy Settings")]
        [SerializeField] private bool _isDestroying = false;
        private Transform _lastHitDestroyTransform;
        private List<Material> _lastHitMaterials = new();

        [Header("Ghost Settings")]
        [SerializeField] private Material _ghostMaterialValid;
        [SerializeField] private Material _ghostMaterialInvisible;
        [SerializeField] private Material _ghostMaterialInvalid;
        [SerializeField] private float _connectorOverlapRadius = 1f;
        [SerializeField] private float _maxGroundAngle = 90f;

        [Header("Internal State")]
        [SerializeField] private bool _isBuilding = false;
        public bool IsBuilding => _isBuilding;

        [SerializeField] private int _currentBuildIndex;

        private GameObject _ghostBuildGameObject;
        private bool _isGhostInValidPosition = false;
        private Transform _modelParent = null;
        private bool _clickedThisFrame = false;
        private bool _isInWallMode = false;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += OnSelectedSlotChanged_CheckForHammer;
            GameInput.Instance.OnBuildPlaced += GameInput_OnBuildPlaced;
            GameInput.Instance.OnToggleDestroyMode += GameInput_OnToggleDestroyMode;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= OnSelectedSlotChanged_CheckForHammer;
            GameInput.Instance.OnBuildPlaced -= GameInput_OnBuildPlaced;
            GameInput.Instance.OnToggleDestroyMode -= GameInput_OnToggleDestroyMode;
        }

        private void Update()
        {
            if (_isBuilding && !_isDestroying && Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking)
            {
                GhostBuild();

                if (_clickedThisFrame)
                {
                    PlaceBuild();
                    _clickedThisFrame = false;
                }
            }
            else if (_ghostBuildGameObject != null)
            {
                Destroy(_ghostBuildGameObject);
                _ghostBuildGameObject = null;
            }

            if (_isDestroying)
            {
                GhostDestroy();

                if (_clickedThisFrame)
                {
                    DestroyBuild();
                    _clickedThisFrame = false;
                }
            }
        }

        #region Input

        private void GameInput_OnBuildPlaced(object sender, InputAction.CallbackContext e)
        {
            if(!_isBuilding) return;
            
            _clickedThisFrame = true;
        }

        private void GameInput_OnToggleDestroyMode(object sender, InputAction.CallbackContext e)
        {
            if (!e.started || !_isBuilding) return;
            
            _isDestroying = !_isDestroying;
            Debug.Log($"Destroy Mode: {_isDestroying}");

            if (!_isDestroying)
            {
                if (_lastHitDestroyTransform != null)
                {
                    ResetLastHitDestroyTransform();
                }
            }
        }

        private void OnSelectedSlotChanged_CheckForHammer(int arg1, InventoryItem item)
        {
            if (item.Item is HammerItemSO hammerData)
            {
                Debug.Log($"Building Mode Enabled");
                _isBuilding = true;
                _isDestroying = false;
            }
            else
            {
                Debug.Log($"Building Mode Disabled");
                _isBuilding = false;
                OnGhostUnsnap?.Invoke();

                if (_isDestroying)
                {
                    if (_lastHitDestroyTransform != null)
                    {
                        ResetLastHitDestroyTransform();
                    }

                    _isDestroying = false;
                }
            }
        }

        #endregion



        #region Building

        private void GhostBuild()
        {
            GameObject currentBuild = GetCurrentBuild();
            CreateGhostPrefab(currentBuild);

            MoveGhostPrefabToRaycast();
            CheckBuildValidity();
        }

        private void PlaceBuild()
        {
            if (_ghostBuildGameObject != null && _isGhostInValidPosition)
            {
                GameObject newBuild = Instantiate(GetCurrentBuild(), _ghostBuildGameObject.transform.position, _ghostBuildGameObject.transform.rotation);

                Destroy(_ghostBuildGameObject);
                _ghostBuildGameObject = null;

                foreach (Connector connector in newBuild.GetComponentsInChildren<Connector>())
                {
                    connector.UpdateConnectors(true);
                }
                // AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodKnockSFX, transform.position);
                InventoryManager.Instance.RemoveItems(_itemsNeededForBuilding);
                // _hammer.PlayHammerSwing();
            }
        }

        private GameObject GetCurrentBuild()
        {
            switch (_currentBuildType)
            {
                case SelectedBuildType.Floor:
                    return _floorObjects[_currentBuildIndex];
                case SelectedBuildType.Wall:
                    return _wallObjects[_currentBuildIndex];
                default:
                    return null;
            }
        }

        private void CreateGhostPrefab(GameObject currentBuild)
        {
            if (_ghostBuildGameObject == null)
            {
                _ghostBuildGameObject = Instantiate(currentBuild);

                if (_ghostBuildGameObject.TryGetComponent(out Floor floor))
                {
                    floor.enabled = false;
                    floor.DecalProjector.enabled = false;
                    floor.DecalProjector.gameObject.SetActive(false);
                }

                _modelParent = _ghostBuildGameObject.transform.GetChild(0);

                // if(_ghostBuildGameObject.transform.GetChild(2).gameObject != null)
                //     _ghostBuildGameObject.transform.GetChild(2).GetComponent<BoxCollider>().enabled = false;


                GhostifyModel(_modelParent, _ghostMaterialInvisible); // Sets the correct material
                GhostifyModel(_ghostBuildGameObject.transform); // Disables colliders on the ghostbuild so it doesn't affect the other colliders near it
            }
        }

        private void CheckBuildValidity()
        {
            Collider[] colliders = Physics.OverlapSphere(_ghostBuildGameObject.transform.position, _connectorOverlapRadius, _connectorLayerMask);
            if (colliders.Length > 0)
            {
                // Trying to connect to a prefab that already exists in the scene
                GhostConnectBuild(colliders);
            }
            else
            {
                // Trying to create a brand new build piece
                GhostSeparateBuild();

                if (_isGhostInValidPosition)
                {
                    Collider[] overlapColliders = Physics.OverlapBox(_ghostBuildGameObject.transform.position, new Vector3(1f, 1f, 1f), _ghostBuildGameObject.transform.rotation);
                    foreach (Collider overlapCollider in overlapColliders)
                    {
                        if (overlapCollider.gameObject != _ghostBuildGameObject && overlapCollider.transform.root.CompareTag("Buildables"))
                        {
                            GhostifyModel(_modelParent, _ghostMaterialInvisible);
                            _isGhostInValidPosition = false;
                            return;
                        }
                    }
                }
            }
        }

        private void GhostConnectBuild(Collider[] colliders)
        {
            Connector bestConnector = null;
            float closestDistance = float.MaxValue;
            foreach (Collider collider in colliders)
            {
                Connector connector = collider.GetComponent<Connector>();
                if (connector.CanConnectTo)
                {
                    float distance = Vector3.Distance(_ghostBuildGameObject.transform.position, connector.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestConnector = connector;
                    }
                }
            }

            if (bestConnector == null || _currentBuildType == SelectedBuildType.Floor && bestConnector.IsConnectedToFloor || _currentBuildType == SelectedBuildType.Wall && bestConnector.IsConnectedToWall)
            {
                // We have nothing to connect to
                if (_lastSnappedConnector != null)
                {
                    OnGhostUnsnap?.Invoke();
                    _lastSnappedConnector = null;
                }
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
                return;
            }

            SnapGhostPrefabToConnector(bestConnector);
        }

        private void SnapGhostPrefabToConnector(Connector connector)
        {
            // Find the correct connector on the ghost prefab to snap to, and snap it to it
            Transform ghostConnector = FindSnapConnector(connector.transform, _ghostBuildGameObject.transform.GetChild(1));
            _ghostBuildGameObject.transform.position = connector.transform.position - (ghostConnector.position - _ghostBuildGameObject.transform.position);

            // Trigger OnGhostSnap action only when snapping to a new connector
            if (_lastSnappedConnector != connector)
            {
                OnGhostSnap?.Invoke();
                _lastSnappedConnector = connector;
            }

            if (_currentBuildType == SelectedBuildType.Wall)
            {
                // Will rotate the wall to match the rotation of the connector it's snapping to
                Quaternion newRotation = _ghostBuildGameObject.transform.rotation;
                newRotation.eulerAngles = new Vector3(newRotation.eulerAngles.x, connector.transform.rotation.eulerAngles.y, newRotation.eulerAngles.z);
                _ghostBuildGameObject.transform.rotation = newRotation;
            }

            GhostifyModel(_modelParent, _ghostMaterialValid);
            _isGhostInValidPosition = true;
        }

        private Transform FindSnapConnector(Transform snapConnector, Transform ghostConnectorParent)
        {
            ConnectorPosition oppositeConnectorTag = GetOppositePosition(snapConnector.GetComponent<Connector>());

            foreach (Connector connector in ghostConnectorParent.GetComponentsInChildren<Connector>())
            {
                if (connector.ConnectorPosition == oppositeConnectorTag)
                {
                    return connector.transform;
                }
            }

            return null;
        }

        private ConnectorPosition GetOppositePosition(Connector connector)
        {
            ConnectorPosition position = connector.ConnectorPosition;

            // If we trying to build a wall and looking at a floor GO, the only thing the wall can connect to is the bottom connector of the floor
            if (_currentBuildType == SelectedBuildType.Wall && connector.ConnectorParentType == SelectedBuildType.Floor)
            {
                return ConnectorPosition.Bottom;
            }

            // If we are trying to build a floor on the top section of a wall, make sure to return the correct position
            if (_currentBuildType == SelectedBuildType.Floor && connector.ConnectorParentType == SelectedBuildType.Wall && connector.ConnectorPosition == ConnectorPosition.Top)
            {
                if (connector.transform.root.rotation.y == 0)
                {
                    return GetConnectorClosestToPlayer(true);
                }
                else
                {
                    return GetConnectorClosestToPlayer(false);
                }
            }

            switch (position)
            {
                case ConnectorPosition.Left:
                    return ConnectorPosition.Right;
                case ConnectorPosition.Right:
                    return ConnectorPosition.Left;
                case ConnectorPosition.Top:
                    return ConnectorPosition.Bottom;
                case ConnectorPosition.Bottom:
                    return ConnectorPosition.Top;
                default:
                    return ConnectorPosition.Bottom;
            }
        }

        private ConnectorPosition GetConnectorClosestToPlayer(bool topBottom)
        {
            // Takes camera position and make sure the floor we are tring to place on top of the wall is facing the player instead of in a random position, QOL feature
            Transform cameraTransform = Camera.main.transform;

            if (topBottom)
            {
                return cameraTransform.position.z >= _ghostBuildGameObject.transform.position.z ? ConnectorPosition.Bottom : ConnectorPosition.Top;
            }
            else
            {
                return cameraTransform.position.x >= _ghostBuildGameObject.transform.position.x ? ConnectorPosition.Left : ConnectorPosition.Right;
            }
        }

        private void GhostSeparateBuild()
        {
            if (_lastSnappedConnector != null)
            {
                OnGhostUnsnap?.Invoke();
                _lastSnappedConnector = null;
            }

            // If it does not have wood to place make it invalid
            if (!InventoryManager.Instance.InventoryHasItems(_itemsNeededForBuilding))
            {
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundValidHit = false;
            RaycastHit validHit = default;
            foreach (RaycastHit hit in hits)
            {
                // Skip if collider is in the connector layer mask
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0 || ((1 << hit.transform.gameObject.layer) & _playerLayerMask) != 0)
                    continue;

                if (hit.transform.root.TryGetComponent(out Player player))
                    continue;

                validHit = hit;
                foundValidHit = true;
                break;
            }

            if (foundValidHit)
            {
                // Only place walls on floors
                if (_currentBuildType == SelectedBuildType.Wall)
                {
                    // If we try to place a wall, but haven't snapped it to anything, we won't be able to place it
                    GhostifyModel(_modelParent, _ghostMaterialInvisible);
                    _isGhostInValidPosition = false;
                    return;
                }

                // Only place on valid angles we set
                // NTFS: Disabling this for now so you can ONLY place it on another platform MIGHT change this later
                if (Vector3.Angle(validHit.normal, Vector3.up) < _maxGroundAngle)
                {
                    GhostifyModel(_modelParent, _ghostMaterialValid);
                    _isGhostInValidPosition = false;
                }
                else
                {
                    GhostifyModel(_modelParent, _ghostMaterialInvisible);
                    _isGhostInValidPosition = false;
                }
            }
            else
            {
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
            }
        }

        private void GhostifyModel(Transform modelParent, Material ghostMaterial = null)
        {
            if (ghostMaterial != null)
            {
                foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
                {
                    if (ghostMaterial != null)
                    {
                        Material[] ghostMaterials = new Material[meshRenderer.materials.Length];
                        for (int i = 0; i < ghostMaterials.Length; i++)
                        {
                            ghostMaterials[i] = ghostMaterial;
                        }
                        meshRenderer.materials = ghostMaterials;
                    }
                    else
                    {
                        foreach (Collider modelCollider in modelParent.GetComponentsInChildren<Collider>())
                        {
                            modelCollider.enabled = false;
                        }
                    }
                }
            }
            else
            {
                foreach (Collider modelCollider in modelParent.GetComponentsInChildren<Collider>())
                {
                    modelCollider.enabled = false;
                }
            }
        }

        private void MoveGhostPrefabToRaycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // Sort hits by distance (RaycastAll doesn’t guarantee order)

            bool foundValidHit = false;
            RaycastHit validHit = default;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform)

                    // Skip if collider is in the connector layer mask
                    if (/* ((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0 ||  */((1 << hit.transform.gameObject.layer) & _playerLayerMask) != 0)
                        continue;

                if (hit.transform.root.TryGetComponent(out Player player))
                    continue;

                // Use this hit
                validHit = hit;
                foundValidHit = true;
                break;
            }

            _ghostBuildGameObject.transform.position = foundValidHit ? validHit.point : ray.origin + ray.direction * _buildRange;
        }

        // Loops through all mesh renderers that are currently red and reset them to their original materials
        private void ResetLastHitDestroyTransform()
        {
            int counter = 0;
            foreach (MeshRenderer lastHitMeshRenderers in _lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
            {
                lastHitMeshRenderers.material = _lastHitMaterials[counter];
                counter++;
            }

            _lastHitDestroyTransform = null;
        }

        #endregion

        #region Destorying

        private void GhostDestroy()
        {
            // Raycast out and find any build objects we can destroy
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);

            // Sort hits by distance (RaycastAll doesn’t guarantee order)
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundValidHit = false;
            RaycastHit validHit = default;

            foreach (RaycastHit hit in hits)
            {
                // Skip if it's in the connector layer
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0)
                    continue;

                if (hit.transform.root.CompareTag("Buildables") || hit.transform.root.CompareTag("Structure"))
                {
                    // if (hit.transform.root.CompareTag("Structure"))
                    // {
                    //     if (hit.transform.root.TryGetComponent(out CookingStation cookingStation))
                    //     {
                    //         if (cookingStation.IsCooking || cookingStation.HasFoodToPickUp)
                    //         {
                    //             continue;
                    //         }
                    //     }
                    // }

                    // Found the first valid target
                    validHit = hit;
                    foundValidHit = true;
                    break;
                }
            }

            if (foundValidHit)
            {
                if (_lastHitDestroyTransform == null)
                {
                    _lastHitDestroyTransform = validHit.transform.root;
                    _lastHitMaterials.Clear();

                    foreach (MeshRenderer lastHitMeshRenderers in _lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
                    {
                        _lastHitMaterials.Add(lastHitMeshRenderers.material);
                    }

                    GhostifyModel(_lastHitDestroyTransform.GetChild(0), _ghostMaterialInvalid);
                }
                else if (validHit.transform.root != _lastHitDestroyTransform)
                {
                    ResetLastHitDestroyTransform();
                }
            }
            else if (_lastHitDestroyTransform != null)
            {
                ResetLastHitDestroyTransform();
            }
        }

        private void DestroyBuild()
        {
            // When we do left click while in destroy mode, destroy the build object we are looking at
            if (_lastHitDestroyTransform)
            {
                bool isBuilding = false;
                foreach (Connector connector in _lastHitDestroyTransform.GetComponentsInChildren<Connector>())
                {
                    isBuilding = true;
                    connector.gameObject.SetActive(false);
                    connector.UpdateConnectors(true);
                }

                Destroy(_lastHitDestroyTransform.gameObject);

                _lastHitDestroyTransform = null;

                if (isBuilding)
                {
                    InventoryManager.Instance.AddItems(_itemsNeededForBuilding);
                }
                else
                {
                    Debug.Log($"Destroyed cooking stations");
                    // InventoryManager.Instance.AddItem(StructureManager.Instance.CurrentStructureItemSO, 1);
                }

                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodDestroyedSFX, transform.position);
                // _hammer.PlayHammerSwing();
            }
        }

        #endregion
    }
}
