using System;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance;

        [SerializeField]
        private float _interactSearchDistance = 2f;

        [SerializeField]
        private LayerMask _interactLayer, _buildLayer;

        private int _playerLayer;

        private IInteractable _currentlyHoveredInteractable;
        public IInteractable CurrentlyHoveredInteractable => _currentlyHoveredInteractable;
        
        private GameObject _currentlyHoveredBuildable;
        public GameObject CurrentlyHoveredBuildable => _currentlyHoveredBuildable;

        public BuildPiece CurrentlyHoveredBuildPiece { get; private set; }

        private void Awake()
        {
            Instance = this;
            _playerLayer = LayerMask.NameToLayer("Player");
        }
        
        private void Start()
        {
            Player.Instance.ToolHolder.OnToolSwingDown += TryToHitInteractable;
            Player.Instance.ToolHolder.OnToolSwingDown += TryToRepairInteractable;
            GameInput.Instance.OnInteract += Interact;
        }
        
        private void OnDestroy()
        {
            Player.Instance.ToolHolder.OnToolSwingDown -= TryToHitInteractable;
            Player.Instance.ToolHolder.OnToolSwingDown -= TryToRepairInteractable;
            GameInput.Instance.OnInteract -= Interact;
        }

        private void Update()
        {
            if (GameInput.Instance == null) return;

            SearchForInteractable();
            SearchForBuildPiece();
        }

        private void Interact(object sender, InputAction.CallbackContext e)
        {
            if(_currentlyHoveredInteractable != null && e.started)
            {
                _currentlyHoveredInteractable.OnInteractWith();
            }
        }

        private void TryToHitInteractable()
        {
            if(_currentlyHoveredInteractable != null)
            {
                // Debug.Log($"Trying to hit: {_currentlyHoveredInteractable}");
                _currentlyHoveredInteractable.OnHitWithTool();
            }
        }

        private void TryToRepairInteractable()
        {
            if (BuildingManager.Instance.CurrentBuildType != BuildOption.RepairMode || BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen || Player.Instance.PauseMenuUI.IsPauseMenuOpen) return;

            // Repair logic here
            if (InventoryManager.Instance.HasSelectedItem)
            {
                if (InventoryManager.Instance.SelectedInventoryItem.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, out hit, _interactSearchDistance))
                    {
                        if (hit.collider.transform.root.TryGetComponent(out BuildPieceDurability bpd))
                        {
                            if (InventoryManager.Instance.InventoryHasItems(BuildingManager.Instance.ItemsNeededForRepairing))
                            {
                                if (bpd.CurrentHitPoints >= bpd.MaxHitPoints)
                                {
                                    return;
                                }

                                bpd.AddHp(toolItem.IntValue);
                                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.BuildingRepairedSFX, bpd.transform.position);
                                InventoryManager.Instance.RemoveItems(BuildingManager.Instance.ItemsNeededForRepairing);
                            }
                        }
                    }
                }
            }
        }

        private bool SearchForInteractable()
        {
            RaycastHit[] hits = Physics.RaycastAll(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, _interactSearchDistance, _interactLayer);

            if (hits.Length == 0)
            {
                _currentlyHoveredInteractable = null;
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == _playerLayer)
                    continue;
                    
                if(hit.collider.gameObject.transform.TryGetComponent(out Resource resource))
                {
                    _currentlyHoveredInteractable = resource;
                    return true;
                }
                else
                {
                    _currentlyHoveredInteractable = hit.collider.GetComponent<IInteractable>();
                    if (_currentlyHoveredInteractable != null) return true;
                }
            }

            _currentlyHoveredInteractable = null;
            return false;
        }
        
        private void SearchForBuildPiece()
        {
            RaycastHit[] hits = Physics.RaycastAll(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, _interactSearchDistance, _buildLayer);

            if (hits.Length == 0)
            {
                CurrentlyHoveredBuildPiece = null;
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == _playerLayer)
                    continue;

                BuildPiece piece = hit.collider.GetComponentInParent<BuildPiece>();
                if (piece != null)
                {
                    CurrentlyHoveredBuildPiece = piece;
                    return;
                }
            }

            CurrentlyHoveredBuildPiece = null;
        }
    }
}
