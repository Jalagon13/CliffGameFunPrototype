using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    [Serializable]
    public enum SelectedBuildType
    {
        Platform,
        Fence,
        Stairs,
        DestroyMode,
        RepairMode,
    }

    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance;

        public Action OnGhostSnap;
        public Action OnGhostUnsnap;
        public Action<SelectedBuildType> OnBuildTypeChanged;

        [Header("Build Objects")]
        [SerializeField] private List<GameObject> _floorObjects = new();
        [SerializeField] private List<GameObject> _wallObjects = new();
        [SerializeField] private List<GameObject> _stairsObjects = new();

        [Header("Build Settings")]
        [field: SerializeField] 
        public BuildWheelUI BuildWheelUI { get; private set; }
        
        [SerializeField] private SelectedBuildType _currentBuildType;
        public SelectedBuildType CurrentBuildType => _currentBuildType;
        
        [SerializeField] private LayerMask _connectorLayerMask;
        [SerializeField] private LayerMask _playerLayerMask;
        [SerializeField] private float _buildRange = 4f;
        [SerializeField] private float _destroyDuration = 0.5f;
        [SerializeField] private InventoryItem[] _itemsNeededForBuilding;
        public InventoryItem[] ItemsNeededForBuilding => _itemsNeededForBuilding;

        [SerializeField] private InventoryItem[] _itemsNeededForRepairing;
        public InventoryItem[] ItemsNeededForRepairing => _itemsNeededForRepairing;

        private Connector _lastSnappedConnector = null; // Tracks the last connector the ghost was snapped to, for snap/unsnap events
        private Transform _lastHitDestroyTransform;
        private List<Material> _lastHitMaterials = new();

        [Header("Ghost Settings")]
        [SerializeField] private Material _ghostMaterialValid;
        [SerializeField] private Material _ghostMaterialInvisible;
        [SerializeField] private Material _ghostMaterialInvalid;
        [SerializeField] private float _connectorOverlapRadius = 1f;
        [SerializeField] private float _maxGroundAngle = 90f;

        [SerializeField] private int _currentBuildIndex;

        private GameObject _ghostBuildGameObject;
        private bool _isGhostInValidPosition = false;
        private Transform _modelParent = null;
        private bool _isHoldingHammar, _clickedThisFrame;
        private Timer _destroyTimer;
        public Timer DestroyTimer => _destroyTimer;
        
        private bool _isDestroying = false, _usingUpstairs = true;
        private Transform _currentDestroyTarget = null;

        private void Awake()
        {
            Instance = this;

            _destroyTimer = new(_destroyDuration);
            _destroyTimer.OnTimerEnd += OnDestroyTimerEnd;
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += OnSelectedSlotChanged_CheckForHammer;
            GameInput.Instance.OnPrimaryInteract += GameInput_OnPrimaryInteract;
            GameInput.Instance.OnCycleBuildVariant += GameInput_CycleBuildVariant;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= OnSelectedSlotChanged_CheckForHammer;
            GameInput.Instance.OnPrimaryInteract -= GameInput_OnPrimaryInteract;
            GameInput.Instance.OnCycleBuildVariant -= GameInput_CycleBuildVariant;
            
            _destroyTimer.OnTimerEnd -= OnDestroyTimerEnd;
        }

        private void Update()
        {
            if(!_isHoldingHammar) return;
        
            if ((_currentBuildType == SelectedBuildType.Platform || _currentBuildType == SelectedBuildType.Fence || _currentBuildType == SelectedBuildType.Stairs) && 
                Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking && !CraftingManager.Instance.IsCraftingUIOpen && !Player.Instance.PauseMenuUI.PauseMenuOpen)
            {
                HandleGhostBuild();
                
                switch(_currentBuildType)
                {
                    case SelectedBuildType.Platform:
                    if (GameInput.Instance.IsHoldingDownPrimaryInteract)
                    {
                        PlaceBuild();
                    }
                    break;

                    case SelectedBuildType.Fence:
                    case SelectedBuildType.Stairs:
                    if (_clickedThisFrame)
                    {
                        PlaceBuild();
                        _clickedThisFrame = false;
                    }
                    break;
                }
            }
            else if (_ghostBuildGameObject != null)
            {
                Destroy(_ghostBuildGameObject);
                _ghostBuildGameObject = null;
            }

            if (_currentBuildType == SelectedBuildType.DestroyMode)
            {
                GhostDestroy();
                HandleDestroyTimer();
            }
        }

        #region Input

        private void GameInput_CycleBuildVariant(object sender, InputAction.CallbackContext e)
        {
            if (_isHoldingHammar && _currentBuildType == SelectedBuildType.Stairs && e.started)
            {
                _usingUpstairs = !_usingUpstairs; // SUPER tentative only used for stairs for now
                Debug.Log($"Using upstairs: {_usingUpstairs}");
            }
        }

        private void GameInput_OnPrimaryInteract(object sender, InputAction.CallbackContext e)
        {
            if(!_isHoldingHammar || BuildWheelUI.BuildWheelUIOpen || Player.Instance.PauseMenuUI.PauseMenuOpen) return;
            
            if(e.started)
            {
                _clickedThisFrame = true;
            }
        }
        
        public void SetBuildType(SelectedBuildType buildType)
        {
            switch (buildType)
            {
                case SelectedBuildType.Platform:
                    _currentBuildType = SelectedBuildType.Platform;
                    ResetGhosts();
                    break;
                case SelectedBuildType.Fence:
                    _currentBuildType = SelectedBuildType.Fence;
                    ResetGhosts();
                    break;
                case SelectedBuildType.Stairs:
                    _currentBuildType = SelectedBuildType.Stairs;
                    ResetGhosts();
                    break;
                case SelectedBuildType.DestroyMode:
                    _currentBuildType = SelectedBuildType.DestroyMode;
                    break;
                case SelectedBuildType.RepairMode:
                    _currentBuildType = SelectedBuildType.RepairMode;
                    ResetGhosts();
                    break;
            }

            OnBuildTypeChanged?.Invoke(_currentBuildType);
        }
        
        private void ResetGhosts()
        {
            if (_ghostBuildGameObject != null)
            {
                Destroy(_ghostBuildGameObject);
                _ghostBuildGameObject = null;
            }

            if (_lastHitDestroyTransform != null)
            {
                ResetLastHitDestroyTransform();
            }
        }

        private void OnSelectedSlotChanged_CheckForHammer(int arg1, InventoryItem item)
        {
            if (item.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer)
            {
                _isHoldingHammar = true;
            }
            else
            {
                _isHoldingHammar = false;
                OnGhostUnsnap?.Invoke();

                if (_ghostBuildGameObject != null)
                {
                    Destroy(_ghostBuildGameObject);
                    _ghostBuildGameObject = null;
                }

                if (_lastHitDestroyTransform != null)
                {
                    ResetLastHitDestroyTransform();
                }
            }
        }

        #endregion

        #region Building

        private void HandleGhostBuild()
        {
            GameObject currentBuild = GetCurrentBuild();
            CreateGhostPrefab(currentBuild);

            MoveGhostPrefabToRaycast();
            CheckBuildValidity();
        }

        private void PlaceBuild()
        {
            if (!InventoryManager.Instance.InventoryHasItems(_itemsNeededForBuilding) || CraftingManager.Instance.IsCraftingUIOpen)
                return;

            if (_ghostBuildGameObject != null && _isGhostInValidPosition)
            {
                GameObject newBuild = Instantiate(GetCurrentBuild(), _ghostBuildGameObject.transform.position, _ghostBuildGameObject.transform.rotation);

                Destroy(_ghostBuildGameObject);
                _ghostBuildGameObject = null;

                foreach (Connector connector in newBuild.GetComponentsInChildren<Connector>())
                {
                    connector.UpdateConnectors(true);
                }
                
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StructureBuiltSFX, transform.position);
                InventoryManager.Instance.RemoveItems(_itemsNeededForBuilding);
            }
        }

        private GameObject GetCurrentBuild()
        {
            switch (_currentBuildType)
            {
                case SelectedBuildType.Platform:
                    return _floorObjects[_currentBuildIndex];
                case SelectedBuildType.Fence:
                    return _wallObjects[_currentBuildIndex];
                case SelectedBuildType.Stairs:
                    return _stairsObjects[_currentBuildIndex];
                default:
                    return null;
            }
        }

        private void CreateGhostPrefab(GameObject currentBuild)
        {
            if (_ghostBuildGameObject == null)
            {
                _ghostBuildGameObject = Instantiate(currentBuild);

                if (_ghostBuildGameObject.TryGetComponent(out Platform floor))
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

            if (bestConnector == null || _currentBuildType == SelectedBuildType.Platform && (bestConnector.IsConnectedToFloor || bestConnector.IsConnectedToStairs) || 
                _currentBuildType == SelectedBuildType.Fence && bestConnector.IsConnectedToFence || _currentBuildType == SelectedBuildType.Stairs && bestConnector.IsConnectedToStairs)
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

        private void SnapGhostPrefabToConnector(Connector bestConnector)
        {
            if(_ghostBuildGameObject == null) return;
        
            // Find the correct connector on the ghost prefab to snap to, and snap it to it
            Transform ghostConnector = FindCorrectSnapConnectorOnGhost(bestConnector.transform, _ghostBuildGameObject.transform.GetChild(1));
            
            if(ghostConnector == null)
            {
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
                return;
            }
            
            _ghostBuildGameObject.transform.position = bestConnector.transform.position - (ghostConnector.position - _ghostBuildGameObject.transform.position);

            // Trigger OnGhostSnap action only when snapping to a new connector
            if (_lastSnappedConnector != bestConnector)
            {
                OnGhostSnap?.Invoke();
                _lastSnappedConnector = bestConnector;
            }

            if (_currentBuildType == SelectedBuildType.Fence || _currentBuildType == SelectedBuildType.Stairs)
            {
                // First: rotate to match the connector
                Quaternion newRotation = _ghostBuildGameObject.transform.rotation;
                newRotation.eulerAngles = new Vector3(
                    newRotation.eulerAngles.x,
                    bestConnector.transform.rotation.eulerAngles.y,
                    newRotation.eulerAngles.z
                );
                _ghostBuildGameObject.transform.rotation = newRotation;

                // Second: ensure the forward faces away from the player camera
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();

                Vector3 ghostForward = _ghostBuildGameObject.transform.forward;
                ghostForward.y = 0f;
                ghostForward.Normalize();

                // If ghost is facing generally the opposite direction from the camera forward vector, flip it
                float dot = Vector3.Dot(ghostForward, cameraForward);
                bool wantsNormalFacing = true;

                if (_currentBuildType == SelectedBuildType.Stairs)
                {
                    wantsNormalFacing = _usingUpstairs;
                }

                bool isFacingAwayFromCamera = dot < 0f;

                if (wantsNormalFacing && isFacingAwayFromCamera || !wantsNormalFacing && !isFacingAwayFromCamera)
                {
                    _ghostBuildGameObject.transform.Rotate(0f, 180f, 0f);
                }
            }

            if (!InventoryManager.Instance.InventoryHasItems(_itemsNeededForBuilding))
            {
                GhostifyModel(_modelParent, _ghostMaterialInvalid);
                _isGhostInValidPosition = false;
                return;
            }
            
            if(GhostStairsOverlappingExistingStairs())
            {
                GhostifyModel(_modelParent, _ghostMaterialInvalid);
                _isGhostInValidPosition = false;
                return;
            }

            GhostifyModel(_modelParent, _ghostMaterialValid);
            _isGhostInValidPosition = true;
        }

        private bool GhostStairsOverlappingExistingStairs()
        {
            if(_currentBuildType != SelectedBuildType.Stairs) return false;

            SphereCollider stairCenterCollider = _ghostBuildGameObject.transform.GetChild(3).GetComponent<SphereCollider>();
            Collider[] overlappingColliders = Physics.OverlapSphere(stairCenterCollider.bounds.center, stairCenterCollider.radius);

            foreach (Collider c in overlappingColliders)
            {
                if (c.gameObject.transform.root.gameObject != _ghostBuildGameObject && c.gameObject.CompareTag("StairCenterCollider"))
                {
                    return true;
                }
            }

            return false;
        }

        private Transform FindCorrectSnapConnectorOnGhost(Transform bestConnectorTf, Transform ghostConnectorParent)
        {
            ConnectorPosition oppositeConnectorTag = GetBestGhostConnector(bestConnectorTf.GetComponent<Connector>());

            foreach (Connector connector in ghostConnectorParent.GetComponentsInChildren<Connector>())
            {
                if (connector.ConnectorPosition == oppositeConnectorTag)
                {
                    return connector.transform;
                }
            }

            return null;
        }

        // Important for choosing which connector on the ghost prefab to snap to the best connector found
        private ConnectorPosition GetBestGhostConnector(Connector bestConnector)
        {
            ConnectorPosition position = bestConnector.ConnectorPosition;

            // If we trying to build a fence and looking at a floor GO, the only thing the fence can connect to is the bottom connector of the floor
            if (_currentBuildType == SelectedBuildType.Fence && bestConnector.ConnectorParentType == SelectedBuildType.Platform)
            {
                return ConnectorPosition.Bottom;
            }
            else if(_currentBuildType == SelectedBuildType.Stairs && (bestConnector.ConnectorParentType == SelectedBuildType.Platform || bestConnector.ConnectorParentType == SelectedBuildType.Stairs))
            {
                return _usingUpstairs ? ConnectorPosition.Bottom : ConnectorPosition.Top; // Top for down stairs and bottom for up stairs
            }
            
            // If i'm trying to place down a platform on a stair
            if(_currentBuildType == SelectedBuildType.Platform && bestConnector.ConnectorParentType == SelectedBuildType.Stairs)
            {
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();
                
                Vector3 connectorForward = bestConnector.transform.forward;
                connectorForward.y = 0f;
                connectorForward.Normalize();
                
                float dot = Vector3.Dot(cameraForward, connectorForward);
                
                if (dot > 0f)
                {
                    // Determine the cardinal direction the connector itself is facing (world +Z = North)
                    // This is so we can return the correct opposite connector on the ghost prefab since for now the platforms are fixed in world space
                    Vector3 facing = bestConnector.transform.forward;
                    facing.y = 0f;
                    facing.Normalize();

                    float north = Vector3.Dot(facing, Vector3.forward); // +Z
                    float south = Vector3.Dot(facing, Vector3.back);    // -Z
                    float east  = Vector3.Dot(facing, Vector3.right);   // +X
                    float west  = Vector3.Dot(facing, Vector3.left);    // -X

                    float max = Mathf.Max(north, south, east, west);

                    switch (max)
                    {
                        case var _ when max == north:
                            return ConnectorPosition.Bottom;
                        case var _ when max == south:
                            return ConnectorPosition.Top;
                        case var _ when max == east:
                            return ConnectorPosition.Left;
                        default:
                            return ConnectorPosition.Right;
                    }
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
                // Only place fences on floors
                if (_currentBuildType == SelectedBuildType.Fence || _currentBuildType == SelectedBuildType.Stairs)
                {
                    // If we try to place a fence or stair, but haven't snapped it to anything, we won't be able to place it
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
            if(BuildWheelUI.BuildWheelUIOpen) return;
        
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

        private void HandleDestroyTimer()
        {
            // Must be holding interact and hovering a valid target
            if (!GameInput.Instance.IsHoldingDownPrimaryInteract || _lastHitDestroyTransform == null)
            {
                CancelDestroy();
                return;
            }

            // New target → reset timer
            if (_currentDestroyTarget != _lastHitDestroyTransform)
            {
                _currentDestroyTarget = _lastHitDestroyTransform;
                _destroyTimer.Reset();
            }

            _isDestroying = true;
            _destroyTimer.Tick(Time.deltaTime);
        }

        private void OnDestroyTimerEnd(object sender, EventArgs e)
        {
            if (!_isDestroying || _currentDestroyTarget == null)
                return;

            DestroyBuild();

            // Prepare for next object (Raft-style chaining)
            _destroyTimer.Reset();
            _currentDestroyTarget = null;
            _isDestroying = false;
        }

        private void CancelDestroy()
        {
            if (!_isDestroying) return;

            _isDestroying = false;
            _currentDestroyTarget = null;
            _destroyTimer.Reset();
        }

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
