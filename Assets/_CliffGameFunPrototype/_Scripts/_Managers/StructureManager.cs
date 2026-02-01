using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class StructureManager : MonoBehaviour
    {
        public static StructureManager Instance;

        [Header("Build Settings")]
        [SerializeField] private LayerMask _connectorLayerMask;
        [SerializeField] private LayerMask _buildableSurfaceMask;
        [SerializeField] private float _buildRange = 4f;

        [Header("Ghost Settings")]
        [SerializeField] private Material _ghostMaterialValid;
        [SerializeField] private Material _ghostMaterialInvalid;
        [SerializeField] private float _maxGroundAngle = 45f;

        private StructureItemSO _currentStructureItemSO;
        private bool _isBuilding = false;
        private bool _clickedThisFrame = false;
        private GameObject _ghostStructureGameObject;
        private StructureItemSO _previousStructureItemSO = null;
        private Transform _modelParent = null;
        private bool _isGhostInValidPosition = false;

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
            if (_isBuilding)
            {
                GhostStructure();

                if (_clickedThisFrame)
                {
                    PlaceStructure();
                    _clickedThisFrame = false;
                }
            }
            else if (_ghostStructureGameObject != null)
            {
                Destroy(_ghostStructureGameObject);
                _ghostStructureGameObject = null;
            }
        }

        private void PlaceStructure()
        {
            if (_ghostStructureGameObject != null && _isGhostInValidPosition)
            {
                Instantiate(_currentStructureItemSO.StructurePrefab, _ghostStructureGameObject.transform.position, _ghostStructureGameObject.transform.rotation);

                Destroy(_ghostStructureGameObject);
                _ghostStructureGameObject = null;

                InventoryManager.Instance.RemoveItem(_currentStructureItemSO, 1);
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StructureBuiltSFX, Player.Instance.transform.position);
            }
        }

        private void GhostStructure()
        {
            CreateGhostStructurePrefab();
            MoveGhostPrefabToRaycast();
            CheckBuildValidity();
        }

        private void CreateGhostStructurePrefab()
        {
            if (_ghostStructureGameObject == null || _currentStructureItemSO != _previousStructureItemSO)
            {
                if (_ghostStructureGameObject != null)
                {
                    Destroy(_ghostStructureGameObject);
                }

                _ghostStructureGameObject = Instantiate(_currentStructureItemSO.StructurePrefab);

                _modelParent = _ghostStructureGameObject.transform.GetChild(0);

                GhostifyModel(_modelParent, _ghostMaterialInvalid); // Sets the correct material
                GhostifyModel(_ghostStructureGameObject.transform); // Disables colliders on the ghostbuild so it doesn't affect the other colliders near it

                _previousStructureItemSO = _currentStructureItemSO;
            }
        }

        private void MoveGhostPrefabToRaycast()
        {
            if (_ghostStructureGameObject == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // Sort hits by distance (RaycastAll doesn’t guarantee order)

            bool foundValidHit = false;
            RaycastHit validHit = default;
            foreach (RaycastHit hit in hits)
            {
                // Skip if collider is in the connector layer mask
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0)
                    continue;

                // Use this hit
                validHit = hit;
                foundValidHit = true;
                break;
            }

            _ghostStructureGameObject.transform.position = foundValidHit ? validHit.point : ray.origin + ray.direction * _buildRange;
        }

        private void CheckBuildValidity()
        {
            if (_ghostStructureGameObject == null) return;

            // Trying to create a brand new build piece
            GhostSeparateBuild();

            if (_isGhostInValidPosition)
            {
                // Get the BoxCollider of the structure model
                BoxCollider boxCollider = _ghostStructureGameObject.transform.GetChild(0).GetChild(0).GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    // Calculate world-space center of the box
                    Vector3 worldCenter = boxCollider.transform.TransformPoint(boxCollider.center);
                    Vector3 halfSize = boxCollider.size * 0.5f; // Half-size for OverlapBox
                    Quaternion rotation = boxCollider.transform.rotation;

                    Collider[] overlapColliders = Physics.OverlapBox(worldCenter, halfSize, rotation);
                    foreach (Collider overlapCollider in overlapColliders)
                    {
                        bool isConnector = ((1 << overlapCollider.gameObject.layer) & _connectorLayerMask) != 0;
                        if (isConnector) continue;
                        if (overlapCollider.gameObject == _ghostStructureGameObject) continue;

                        if (overlapCollider.gameObject != _ghostStructureGameObject &&
                            overlapCollider.transform.root.CompareTag("Placeable")/*  &&
                            !isConnector */) // <-- notice the NOT here
                        {
                            GhostifyModel(_modelParent, _ghostMaterialInvalid);
                            _isGhostInValidPosition = false;
                            return;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Missing boxCollider for {_ghostStructureGameObject.name}");
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

                if (hit.transform.gameObject == _ghostStructureGameObject) continue;

                float angleToUp = Vector3.Angle(hit.normal, Vector3.up);
                float angleToForward = Vector3.Angle(hit.normal, Player.Instance.transform.forward);

                bool isBuildableSurface = ((1 << hit.transform.gameObject.layer) & _buildableSurfaceMask) != 0;

                if (angleToUp < _maxGroundAngle && isBuildableSurface)
                {
                    GhostifyModel(_modelParent, _ghostMaterialValid);
                    _isGhostInValidPosition = true;
                }
                else
                {
                    GhostifyModel(_modelParent, _ghostMaterialInvalid);
                    _isGhostInValidPosition = false;
                }

                return;
            }

            // If no valid surface was found within range
            GhostifyModel(_modelParent, _ghostMaterialInvalid);
            _isGhostInValidPosition = false;
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

        private void OnPrimaryInteract(object sender, InputAction.CallbackContext e)
        {
            if (!e.started || !_isBuilding) return;
            _clickedThisFrame = true;
        }

        private void CheckIfHoldingStructureItem(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            if(InventoryManager.Instance.SelectedInventoryItem.Item is StructureItemSO structureData)
            {
                _isBuilding = true;
                _currentStructureItemSO = structureData;
            }
            else
            {
                _isBuilding = false;
                _currentStructureItemSO = null;
            }
        }

        private void OnSelectedSlotChanged(int arg1, InventoryItem item)
        {
            _currentStructureItemSO = null;

            if (item.Item is StructureItemSO structureData)
            {
                _isBuilding = true;
                _currentStructureItemSO = structureData;
            }
            else
            {
                _isBuilding = false;
                _currentStructureItemSO = null;
            }
        }
    }
}
