using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    [Serializable]
    public enum BuildOption
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
        public Action<BuildOption> OnBuildTypeChanged;

        [Header("Build Objects")]
        [SerializeField] private List<BuildPiece> _platformObjects = new();
        [SerializeField] private List<BuildPiece> _wallObjects = new();
        [SerializeField] private List<BuildPiece> _stairsObjects = new();

        [Header("Build Settings")]
        [field: SerializeField] 
        public BuildWheelUI BuildWheelUI { get; private set; }
        
        [SerializeField] private BuildOption _currentBuildType;
        public BuildOption CurrentBuildType => _currentBuildType;
        
        [SerializeField] private LayerMask _connectorLayerMask;
        [SerializeField] private LayerMask _playerLayerMask;
        [SerializeField] private float _buildRange = 4f;
        [SerializeField] private float _destroyDuration = 0.5f;
        [SerializeField] private float _placeCooldown = 0.15f;
        [SerializeField] private InventoryItem[] _itemsNeededForBuilding;
        public InventoryItem[] ItemsNeededForBuilding => _itemsNeededForBuilding;

        [SerializeField] private InventoryItem[] _itemsNeededForRepairing;
        public InventoryItem[] ItemsNeededForRepairing => _itemsNeededForRepairing;

        private Connector _lastSnappedConnector = null; // Tracks the last connector the ghost was snapped to, for snap/unsnap events
        private List<Material> _lastHitMaterials = new();

        [Header("Ghost Settings")]
        [SerializeField] private Material _ghostMaterialValid;
        [SerializeField] private Material _ghostMaterialInvisible;
        [SerializeField] private Material _ghostMaterialInvalid;
        [SerializeField] private float _connectorOverlapRadius = 1f;
        [SerializeField] private float _maxGroundAngle = 90f;

        [SerializeField] private int _currentBuildIndex;

        private BuildPiece _ghostBuildPiece;
        private bool _isGhostInValidPosition = false;
        private Transform _modelParent = null;
        private bool _isHoldingHammar, _clickedThisFrame;
        private Timer _destroyTimer;
        public Timer DestroyTimer => _destroyTimer;
        private Timer _placeCooldownTimer;
        
        private bool _isDestroying = false, _usingUpstairs = true;
        private Transform _currentDestroyTarget = null;

        private void Awake()
        {
            Instance = this;

            _destroyTimer = new(_destroyDuration);
            _destroyTimer.OnTimerEnd += OnDestroyTimerEnd;
            _placeCooldownTimer = new Timer(_placeCooldown);
            _placeCooldownTimer.SubtractTime(_placeCooldown); // start at 0 so we can place immediately
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

            _placeCooldownTimer.Tick(Time.deltaTime);
        
            if ((_currentBuildType == BuildOption.Platform || _currentBuildType == BuildOption.Fence || _currentBuildType == BuildOption.Stairs) && 
                Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking && !CraftingManager.Instance.IsCraftingUIOpen && !Player.Instance.PauseMenuUI.PauseMenuOpen)
            {
                HandleGhostBuild();
                
                switch(_currentBuildType)
                {
                    case BuildOption.Platform:
                    case BuildOption.Fence:
                    case BuildOption.Stairs:
                    if (GameInput.Instance.IsHoldingDownPrimaryInteract)
                    {
                        PlaceBuild();
                    }
                    break;
                }
            }
            else if (_ghostBuildPiece != null)
            {
                Destroy(_ghostBuildPiece.gameObject);
                _ghostBuildPiece = null;
            }

            if (_currentBuildType == BuildOption.DestroyMode && !CraftingManager.Instance.IsCraftingUIOpen && !Player.Instance.PauseMenuUI.PauseMenuOpen && !BuildWheelUI.BuildWheelUIOpen)
            {
                GhostDestroy();
                HandleDestroyTimer();
            }
        }

        #region Input

        private void GameInput_CycleBuildVariant(object sender, InputAction.CallbackContext e)
        {
            if (_isHoldingHammar && _currentBuildType == BuildOption.Stairs && e.started)
            {
                _usingUpstairs = !_usingUpstairs; // SUPER tentative only used for stairs for now
                // Debug.Log($"Using upstairs: {_usingUpstairs}");
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
        
        public void SetBuildType(BuildOption buildType)
        {
            switch (buildType)
            {
                case BuildOption.Platform:
                    _currentBuildType = BuildOption.Platform;
                    ResetGhosts();
                    break;
                case BuildOption.Fence:
                    _currentBuildType = BuildOption.Fence;
                    ResetGhosts();
                    break;
                case BuildOption.Stairs:
                    _currentBuildType = BuildOption.Stairs;
                    ResetGhosts();
                    break;
                case BuildOption.DestroyMode:
                    _currentBuildType = BuildOption.DestroyMode;
                    break;
                case BuildOption.RepairMode:
                    _currentBuildType = BuildOption.RepairMode;
                    ResetGhosts();
                    break;
            }

            OnBuildTypeChanged?.Invoke(_currentBuildType);
        }
        
        private void ResetGhosts()
        {
            if (_ghostBuildPiece != null)
            {
                Destroy(_ghostBuildPiece.gameObject);
                _ghostBuildPiece = null;
            }

            if (_currentDestroyTarget != null)
            {
                ResetCurrentDestroyTarget();
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

                if (_ghostBuildPiece != null)
                {
                    Destroy(_ghostBuildPiece.gameObject);
                    _ghostBuildPiece = null;
                }

                if (_currentDestroyTarget != null)
                {
                    ResetCurrentDestroyTarget();
                }
            }
        }

        #endregion

        #region Building

        private void HandleGhostBuild()
        {
            BuildPiece currentBuild = GetCurrentBuild();
            CreateGhostPrefab(currentBuild);

            MoveGhostPrefabToRaycast();
            CheckBuildPieceValidity();
        }

        private void PlaceBuild()
        {
            if (_placeCooldownTimer.RemainingSeconds > 0f)
                return;

            if (!InventoryManager.Instance.InventoryHasItems(_itemsNeededForBuilding) || CraftingManager.Instance.IsCraftingUIOpen)
                return;

            if (_ghostBuildPiece != null && _isGhostInValidPosition)
            {
                BuildPiece newBuildPiece = Instantiate(GetCurrentBuild(), _ghostBuildPiece.transform.position, _ghostBuildPiece.transform.rotation);

                Destroy(_ghostBuildPiece.gameObject);
                _ghostBuildPiece = null;

                foreach (Connector connector in newBuildPiece.GetComponentsInChildren<Connector>())
                {
                    connector.EstablishConnection(true);
                }
                
                BuildPieceIntegrityManager.Instance.RegisterBuildPiece(newBuildPiece);
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StructureBuiltSFX, transform.position);
                InventoryManager.Instance.RemoveItems(_itemsNeededForBuilding);
                _placeCooldownTimer.Reset();
            }
        }

        private BuildPiece GetCurrentBuild()
        {
            switch (_currentBuildType)
            {
                case BuildOption.Platform:
                    return _platformObjects[_currentBuildIndex];
                case BuildOption.Fence:
                    return _wallObjects[_currentBuildIndex];
                case BuildOption.Stairs:
                    return _stairsObjects[_currentBuildIndex];
                default:
                    return null;
            }
        }

        private void CreateGhostPrefab(BuildPiece currentBuild)
        {
            if (_ghostBuildPiece == null)
            {
                _ghostBuildPiece = Instantiate(currentBuild);

                if (_ghostBuildPiece.TryGetComponent(out Platform floor))
                {
                    floor.enabled = false;
                    floor.DecalProjector.enabled = false;
                    floor.DecalProjector.gameObject.SetActive(false);
                }

                _modelParent = _ghostBuildPiece.transform.GetChild(0);

                // if(_ghostBuildGameObject.transform.GetChild(2).gameObject != null)
                //     _ghostBuildGameObject.transform.GetChild(2).GetComponent<BoxCollider>().enabled = false;


                GhostifyModel(_modelParent, _ghostMaterialInvisible); // Sets the correct material
                GhostifyModel(_ghostBuildPiece.transform); // Disables colliders on the ghostbuild so it doesn't affect the other colliders near it
            }
        }

        private void CheckBuildPieceValidity()
        {
            Collider[] connectorColliders = Physics.OverlapSphere(_ghostBuildPiece.transform.position, _connectorOverlapRadius, _connectorLayerMask);
            if (connectorColliders.Length > 0)
            {
                // Trying to connect to a prefab that already exists in the scene
                GhostConnectBuild(connectorColliders);
            }
            else
            {
                // Trying to create a brand new build piece
                GhostSeparateBuild();

                if (_isGhostInValidPosition)
                {
                    Collider[] overlapColliders = Physics.OverlapBox(_ghostBuildPiece.transform.position, new Vector3(1f, 1f, 1f), _ghostBuildPiece.transform.rotation);
                    foreach (Collider overlapCollider in overlapColliders)
                    {
                        if (overlapCollider.gameObject != _ghostBuildPiece && overlapCollider.transform.root.CompareTag("BuildPiece"))
                        {
                            GhostifyModel(_modelParent, _ghostMaterialInvisible);
                            _isGhostInValidPosition = false;
                            return;
                        }
                    }
                }
            }
        }

        private void GhostConnectBuild(Collider[] connectorColliders)
        {
            Connector closestConnector = null;
            float closestDistance = float.MaxValue;
            foreach (Collider collider in connectorColliders)
            {
                Connector connector = collider.GetComponent<Connector>();
                
                float distance = Vector3.Distance(_ghostBuildPiece.transform.position, connector.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestConnector = connector;
                }
            }

            if(closestConnector == null || !closestConnector.CanConnectTo(_currentBuildType))
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
            
            SnapGhostPrefabToConnector(closestConnector);
        }

        private void SnapGhostPrefabToConnector(Connector closestConnector)
        {
            if(_ghostBuildPiece == null) return;
        
            // Find the correct connector on the ghost prefab to snap to, and snap it to it
            Transform ghostConnector = FindCorrectSnapConnectorOnGhost(closestConnector.transform, _ghostBuildPiece.transform.GetChild(1));
            
            if(ghostConnector == null)
            {
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
                return;
            }
            
            _ghostBuildPiece.transform.position = closestConnector.transform.position - (ghostConnector.position - _ghostBuildPiece.transform.position);

            // Trigger OnGhostSnap action only when snapping to a new connector
            if (_lastSnappedConnector != closestConnector)
            {
                OnGhostSnap?.Invoke();
                _lastSnappedConnector = closestConnector;
            }

            // If Fence or Stairs, rotate it so it like faces away like Minecraft stairs
            if (_currentBuildType == BuildOption.Fence || _currentBuildType == BuildOption.Stairs)
            {
                // First: rotate to match the connector
                Quaternion newRotation = _ghostBuildPiece.transform.rotation;
                newRotation.eulerAngles = new Vector3(
                    newRotation.eulerAngles.x,
                    closestConnector.transform.rotation.eulerAngles.y,
                    newRotation.eulerAngles.z
                );
                _ghostBuildPiece.transform.rotation = newRotation;

                // Second: ensure the forward faces away from the player camera
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();

                Vector3 ghostForward = _ghostBuildPiece.transform.forward;
                ghostForward.y = 0f;
                ghostForward.Normalize();

                // If ghost is facing generally the opposite direction from the camera forward vector, flip it
                float dot = Vector3.Dot(ghostForward, cameraForward);
                bool wantsNormalFacing = true;

                if (_currentBuildType == BuildOption.Stairs)
                {
                    wantsNormalFacing = _usingUpstairs;
                }

                bool isFacingAwayFromCamera = dot < 0f;

                if (wantsNormalFacing && isFacingAwayFromCamera || !wantsNormalFacing && !isFacingAwayFromCamera)
                {
                    _ghostBuildPiece.transform.Rotate(0f, 180f, 0f);
                }
            }

            if (!GhostConnectorOverlapsWithConnector(ghostConnector, closestConnector)) // This is such a weird bug to fix but basically makes sure the ghost connector is overlapping the real connector which should have been already handled in the above code but whatever
            {
                GhostifyModel(_modelParent, _ghostMaterialInvisible);
                _isGhostInValidPosition = false;
                return;
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

        private bool GhostConnectorOverlapsWithConnector(Transform ghostConnector, Connector closestConnector)
        {
            Collider[] overlappingColliders = Physics.OverlapSphere(ghostConnector.position, closestConnector.ConnectorCollider.radius, _connectorLayerMask);

            foreach (Collider c in overlappingColliders)
            {
                if(c.GetInstanceID() == closestConnector.ConnectorCollider.GetInstanceID())
                {
                    return true;
                }
            }

            return false;
        }

        private bool GhostStairsOverlappingExistingStairs()
        {
            if(_currentBuildType != BuildOption.Stairs) return false;

            SphereCollider stairCenterCollider = _ghostBuildPiece.transform.GetChild(3).GetComponent<SphereCollider>();
            Collider[] overlappingColliders = Physics.OverlapSphere(stairCenterCollider.bounds.center, stairCenterCollider.radius);

            foreach (Collider c in overlappingColliders)
            {
                if (c.gameObject.transform.root.gameObject != _ghostBuildPiece && c.gameObject.CompareTag("StairCenterCollider"))
                {
                    return true;
                }
            }

            return false;
        }

        private Transform FindCorrectSnapConnectorOnGhost(Transform bestConnectorTf, Transform ghostConnectorParent)
        {
            ConnectorPosition oppositeConnectorTag = DetermineGhostConnectorPosition(bestConnectorTf.GetComponent<Connector>());

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
        private ConnectorPosition DetermineGhostConnectorPosition(Connector bestConnector)
        {
            ConnectorPosition position = bestConnector.ConnectorPosition;

            // If we trying to build a fence and looking at a floor GO, the only thing the fence can connect to is the bottom connector of the floor
            if (_currentBuildType == BuildOption.Fence && bestConnector.BuildPiece.BuildType == BuildOption.Platform)
            {
                return ConnectorPosition.Bottom;
            }
            else if(_currentBuildType == BuildOption.Stairs && (bestConnector.BuildPiece.BuildType == BuildOption.Platform || bestConnector.BuildPiece.BuildType == BuildOption.Stairs))
            {
                return _usingUpstairs ? ConnectorPosition.Bottom : ConnectorPosition.Top; // Top for down stairs and bottom for up stairs
            }
            
            // If i'm trying to place down a platform on a stair
            if(_currentBuildType == BuildOption.Platform && bestConnector.BuildPiece.BuildType == BuildOption.Stairs)
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
                if (_currentBuildType == BuildOption.Fence || _currentBuildType == BuildOption.Stairs)
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

            _ghostBuildPiece.transform.position = foundValidHit ? validHit.point : ray.origin + ray.direction * _buildRange;
        }

        #endregion

        #region Destorying  

        private void HandleDestroyTimer()
        {
            // If we're not holding interact or not hovering anything, just stop destroying
            if (!GameInput.Instance.IsHoldingDownPrimaryInteract || _currentDestroyTarget == null)
            {
                _isDestroying = false;
                _destroyTimer.Reset();
                return;
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

        private void GhostDestroy()
        {
            // Raycast out and find any build objects we can destroy
            Camera cam = Camera.main;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, _buildRange);

            // Sort hits by distance (RaycastAll doesn’t guarantee order)
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Always clear any previous ghosted target when starting a new evaluation
            bool foundValidHit = false;
            RaycastHit validHit = default;

            foreach (RaycastHit hit in hits)
            {
                // Skip if it's in the connector layer
                if (((1 << hit.transform.gameObject.layer) & _connectorLayerMask) != 0)
                    continue;

                if (hit.transform.root.CompareTag("BuildPiece") || hit.transform.root.CompareTag("Placeable"))
                {
                    // Found the first valid target
                    validHit = hit;
                    foundValidHit = true;
                    break;
                }
            }

            // Guarantee only ONE object is ever ghosted at a time
            if (foundValidHit)
            {
                // If we are hovering a new target, reset the old one first
                if (_currentDestroyTarget != null && validHit.transform.root != _currentDestroyTarget)
                {
                    ResetCurrentDestroyTarget();
                }

                if (_currentDestroyTarget == null)
                {
                    _currentDestroyTarget = validHit.transform.root;
                    _lastHitMaterials.Clear();

                    foreach (MeshRenderer lastHitMeshRenderers in _currentDestroyTarget.GetComponentsInChildren<MeshRenderer>())
                    {
                        _lastHitMaterials.Add(lastHitMeshRenderers.material);
                    }

                    GhostifyModel(_currentDestroyTarget.GetChild(0), _ghostMaterialInvalid);
                }
            }
            else
            {
                // No valid hit at all, ensure nothing stays ghosted
                if (_currentDestroyTarget != null)
                {
                    ResetCurrentDestroyTarget();
                }
            }
        }

        // Loops through all mesh renderers that are currently red and reset them to their original materials
        private void ResetCurrentDestroyTarget()
        {
            int counter = 0;
            foreach (MeshRenderer lastHitMeshRenderers in _currentDestroyTarget.GetComponentsInChildren<MeshRenderer>())
            {
                lastHitMeshRenderers.material = _lastHitMaterials[counter];
                counter++;
            }
            _currentDestroyTarget = null;
        }

        private void DestroyBuild()
        {
            // When we do left click while in destroy mode, destroy the build object we are looking at
            if (_currentDestroyTarget)
            {
                foreach (Connector connector in _currentDestroyTarget.GetComponentsInChildren<Connector>())
                {
                    connector.CleanupConnections();
                    connector.gameObject.SetActive(false);
                }

                var buildPiece = _currentDestroyTarget.GetComponent<BuildPiece>();
                Destroy(_currentDestroyTarget.gameObject);

                _currentDestroyTarget = null;
                InventoryManager.Instance.AddItems(_itemsNeededForBuilding);
                BuildPieceIntegrityManager.Instance.UnregisterBuildPiece(buildPiece);
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodDestroyedSFX, transform.position);
            }
        }

        #endregion
    }
}
