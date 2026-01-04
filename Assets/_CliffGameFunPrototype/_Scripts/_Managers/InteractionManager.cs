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
        private LayerMask _interactLayer;

        private IInteractable _currentlyHoveredInteractable;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            Player.Instance.ToolHolder.OnToolSwingDown += TryToHitInteractable;
            GameInput.Instance.OnTertiaryInteract += RepairFloor;
        }
        
        private void OnDestroy()
        {
            Player.Instance.ToolHolder.OnToolSwingDown -= TryToHitInteractable;
            GameInput.Instance.OnTertiaryInteract -= RepairFloor;
        }

        private void Update()
        {
            if (GameInput.Instance == null) return;

            SearchForResource();
        }

        private void TryToHitInteractable()
        {
            if(_currentlyHoveredInteractable != null)
            {
                _currentlyHoveredInteractable.OnHitWithTool();
            }
        }

        private void RepairFloor(object sender, InputAction.CallbackContext e)
        {
            if(!e.started) return;

            // Repair logic here
            if (InventoryManager.Instance.HasSelectedItem)
            {
                if(InventoryManager.Instance.SelectedInventoryItem.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, out hit, _interactSearchDistance))
                    {
                        if (hit.collider.TryGetComponent(out Floor floor))
                        {
                            Debug.Log($"Hit repairable object: {hit.collider.name}");
                            floor.AddFloorHp(toolItem.IntValue);
                            // Repair logic here
                        }
                    }
                }
            }
        }

        private bool SearchForResource()
        {
            RaycastHit hit;
            if (Physics.Raycast(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, out hit, _interactSearchDistance, _interactLayer))
            {
                // Try to get the IInteractable component from the hit collider
                _currentlyHoveredInteractable = hit.collider.GetComponent<IInteractable>();
                return _currentlyHoveredInteractable != null;
            }

            _currentlyHoveredInteractable = null;
            return false;
        }
    }
}
