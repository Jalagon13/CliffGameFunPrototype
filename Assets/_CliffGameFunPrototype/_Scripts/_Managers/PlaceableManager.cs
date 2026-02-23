using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class PlaceableManager : MonoBehaviour
    {
        public static PlaceableManager Instance;

        [Header("Build Settings")]
        [SerializeField] private LayerMask _connectorLayerMask;
        [SerializeField] private LayerMask _interactorLayerMask;
        [SerializeField] private LayerMask _buildableSurfaceMask;
        [SerializeField] private LayerMask _cliffSurfaceMask;
        [SerializeField] private float _buildRange = 4f;

        [Header("Ghost Settings")]
        [SerializeField] private Material _ghostMaterialValid;
        [SerializeField] private Material _ghostMaterialInvalid;
        [SerializeField] private Material _ghostMaterialInvisible;
        [SerializeField] private float _maxGroundAngle = 45f;

        private PlaceableItemSO _currentPlaceableItemSO;
        private bool _isBuilding = false;
        private bool _clickedThisFrame = false;
        private GameObject _ghostPlaceableGameObject;
        private PlaceableItemSO _previousStructureItemSO = null;
        private Transform _modelParent = null;
        private bool _isGhostInValidPosition = false;
        private BuildPieceDurability _hoveredPlatform = null;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += OnSelectedSlotChanged;
            InventoryManager.Instance.OnInventoryUpdated += CheckIfHoldingStructureItem;
            GameInput.Instance.OnPrimaryInteract += OnPrimaryInteract;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= OnSelectedSlotChanged;
            InventoryManager.Instance.OnInventoryUpdated -= CheckIfHoldingStructureItem;
            GameInput.Instance.OnPrimaryInteract -= OnPrimaryInteract;
        }

        private void Update()
        {
            if (_isBuilding && Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking && !CraftingManager.Instance.IsCraftingUIOpen && !Player.Instance.PauseMenuUI.IsPauseMenuOpen)
            {
                GhostPlaceableHandle();

                if (_clickedThisFrame)
                {
                    PlaceStructure();
                    _clickedThisFrame = false;
                }
            }
            else if (_ghostPlaceableGameObject != null)
            {
                Destroy(_ghostPlaceableGameObject);
                _ghostPlaceableGameObject = null;
            }
        }

        #region Input

        private void OnSelectedSlotChanged(int arg1, InventoryItem item)
        {
            _currentPlaceableItemSO = null;

            if (item.Item is PlaceableItemSO structureData)
            {
                _isBuilding = true;
                _currentPlaceableItemSO = structureData;
            }
            else
            {
                _isBuilding = false;
                _currentPlaceableItemSO = null;
            }
        }

        private void OnPrimaryInteract(object sender, InputAction.CallbackContext e)
        {
            if (!e.started || !_isBuilding) return;

            if (CraftingManager.Instance.IsCraftingUIOpen || Player.Instance.PauseMenuUI.IsPauseMenuOpen) return;

            _clickedThisFrame = true;
        }

        private void CheckIfHoldingStructureItem(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            if (InventoryManager.Instance.SelectedInventoryItem.Item is PlaceableItemSO structureData)
            {
                _isBuilding = true;
                _currentPlaceableItemSO = structureData;
            }
            else
            {
                _isBuilding = false;
                _currentPlaceableItemSO = null;
            }
        }

        #endregion

        #region Placing
        
        private void GhostPlaceableHandle()
        {
            CreateGhostPlaceablePrefab();
            MoveGhostPrefabToRaycast();
            CheckBuildValidity();
        }

        private void PlaceStructure()
        {
            if (_ghostPlaceableGameObject != null && _isGhostInValidPosition)
            {
                GameObject placedGO = Instantiate(_currentPlaceableItemSO.PlaceablePrefab, _ghostPlaceableGameObject.transform.position, _ghostPlaceableGameObject.transform.rotation);
                if(!_currentPlaceableItemSO.IsCliffPlaceable)
                {
                    placedGO.transform.GetComponent<Placeable>().SetSupportedBy(_hoveredPlatform);
                }

                Destroy(_ghostPlaceableGameObject);
                _ghostPlaceableGameObject = null;
                _hoveredPlatform = null;

                InventoryManager.Instance.RemoveItem(_currentPlaceableItemSO, 1);
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StructureBuiltSFX, Player.Instance.transform.position);
            }
        }

        private void CreateGhostPlaceablePrefab()
        {
            if (_ghostPlaceableGameObject == null || _currentPlaceableItemSO != _previousStructureItemSO)
            {
                if (_ghostPlaceableGameObject != null)
                {
                    Destroy(_ghostPlaceableGameObject);
                }

                _ghostPlaceableGameObject = Instantiate(_currentPlaceableItemSO.PlaceablePrefab);
                _ghostPlaceableGameObject.GetComponent<Placeable>().OnSpawnAsGhost();

                _modelParent = _ghostPlaceableGameObject.transform.GetChild(0);

                GhostifyModel(_modelParent, _ghostMaterialInvisible); // Sets the correct material
                GhostifyModel(_ghostPlaceableGameObject.transform); // Disables colliders on the ghostbuild so it doesn't affect the other colliders near it

                _previousStructureItemSO = _currentPlaceableItemSO;
            }
        }

        private void MoveGhostPrefabToRaycast()
        {
            if (_ghostPlaceableGameObject == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // Sort hits by distance (RaycastAll doesn’t guarantee order)

            bool foundValidHit = false;
            RaycastHit validHit = default;
            foreach (RaycastHit hit in hits)
            {
                // Skip if collider is in the connector or interactable layer
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0)
                    continue;

                if (((1 << hit.transform.gameObject.layer) & _interactorLayerMask) != 0)
                    continue;

                // Use this hit
                validHit = hit;
                foundValidHit = true;
                break;
            }

            if (foundValidHit)
            {
                _ghostPlaceableGameObject.transform.position = validHit.point;

                if (_currentPlaceableItemSO != null && _currentPlaceableItemSO.IsCliffPlaceable && Mathf.Abs(Vector3.Dot(validHit.normal, Vector3.up)) < 0.99f)
                {
                    _ghostPlaceableGameObject.transform.rotation = Quaternion.LookRotation(Vector3.up, validHit.normal);
                }
                else if (_currentPlaceableItemSO != null && !_currentPlaceableItemSO.IsCliffPlaceable)
                {
                    Vector3 dirToPlayer = Player.Instance.transform.position - validHit.point;
                    Vector3 forward = Vector3.ProjectOnPlane(dirToPlayer, validHit.normal).normalized;
                    if (forward != Vector3.zero)
                        _ghostPlaceableGameObject.transform.rotation = Quaternion.LookRotation(forward, validHit.normal);
                    else
                        _ghostPlaceableGameObject.transform.up = validHit.normal;
                }
                else
                {
                    _ghostPlaceableGameObject.transform.up = validHit.normal;
                }
            }
            else
            {
                _ghostPlaceableGameObject.transform.position = ray.origin + ray.direction * _buildRange;
                _ghostPlaceableGameObject.transform.up = Vector3.up;
            }
        }

        private void CheckBuildValidity()
        {
            if (_ghostPlaceableGameObject == null) return;

            // Trying to create a brand new build piece
            GhostSeparateBuild();

            if (_isGhostInValidPosition)
            {
                // Get the BoxCollider of the structure model
                BoxCollider boxCollider = _ghostPlaceableGameObject.transform.GetChild(0).GetChild(0).GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    // Calculate world-space center of the box
                    Vector3 worldCenter = boxCollider.transform.TransformPoint(boxCollider.center);
                    Vector3 halfSize = boxCollider.size * 0.5f; // Half-size for OverlapBox
                    Quaternion rotation = boxCollider.transform.rotation;

                    Collider[] overlapColliders = Physics.OverlapBox(worldCenter, halfSize, rotation);
                    foreach (Collider overlapCollider in overlapColliders)
                    {
                        if (overlapCollider.gameObject == _ghostPlaceableGameObject) continue;
                        
                        bool isConnector = ((1 << overlapCollider.gameObject.layer) & _connectorLayerMask) != 0;
                        if (isConnector) continue;
                        
                        if(((1 << overlapCollider.gameObject.layer) & _buildableSurfaceMask) != 0)
                        {
                            GhostifyModel(_modelParent, _ghostMaterialInvalid);
                            _isGhostInValidPosition = false;
                            return;
                        }
                        

                        if (overlapCollider.transform.root.CompareTag("Placeable") || overlapCollider.transform.CompareTag("Resource"))
                        {
                            GhostifyModel(_modelParent, _ghostMaterialInvalid);
                            _isGhostInValidPosition = false;
                            return;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Missing boxCollider for {_ghostPlaceableGameObject.name}");
                }
            }
        }

        private void GhostSeparateBuild()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // Sort hits by distance (RaycastAll doesn’t guarantee order)

            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0)
                    continue;

                if (hit.transform.gameObject == _ghostPlaceableGameObject) continue;

                float angleToUp = Vector3.Angle(hit.normal, Vector3.up);
                float angleToForward = Vector3.Angle(hit.normal, Player.Instance.transform.forward);

                bool isBuildableSurface = ((1 << hit.transform.gameObject.layer) & _buildableSurfaceMask) != 0;
                bool isCliffSurface = ((1 << hit.transform.gameObject.layer) & _cliffSurfaceMask) != 0;
                bool isPlatform = hit.transform.gameObject.TryGetComponent(out BuildPieceDurability platformComponent);

                if(_currentPlaceableItemSO.IsCliffPlaceable && _currentPlaceableItemSO.IsPlacementAngleValid(angleToUp) && isCliffSurface)
                {
                    GhostifyModel(_modelParent, _ghostMaterialValid);
                    _isGhostInValidPosition = true;
                    _hoveredPlatform = platformComponent;
                }
                else if (_currentPlaceableItemSO.IsPlacementAngleValid(angleToUp) && isBuildableSurface && isPlatform)
                {
                    GhostifyModel(_modelParent, _ghostMaterialValid);
                    _isGhostInValidPosition = true;
                    _hoveredPlatform = platformComponent;
                }
                else
                {
                    GhostifyModel(_modelParent, _ghostMaterialInvalid);
                    _isGhostInValidPosition = false;
                    _hoveredPlatform = null;
                }

                return;
            }

            // If no valid surface was found within range
            GhostifyModel(_modelParent, _ghostMaterialInvalid);
            _isGhostInValidPosition = false;
            _hoveredPlatform = null;
        }

        private void GhostifyModel(Transform modelParent, Material ghostMaterial = null)
        {
            if (ghostMaterial != null)
            {
                foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
                {
                    meshRenderer.material = ghostMaterial;
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

        #endregion

    }
}
