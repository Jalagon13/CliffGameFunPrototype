using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance;

        public event Action OnCraftingUIOpened;
        public event Action OnCraftingUIClosed;

        private bool _craftingMenuUIOpened;
        public bool CraftingMenuUIOpened => _craftingMenuUIOpened;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            GameInput.Instance.OnToggleCraftingMenu += OnCraftingToggle;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnToggleCraftingMenu -= OnCraftingToggle;
        }

        private void OnCraftingToggle(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead || Player.Instance.CurrentMoveStateType == PlayerMoveState.Climbing) return;

            _craftingMenuUIOpened = !_craftingMenuUIOpened;
            if (_craftingMenuUIOpened)
                OnCraftingUIOpened?.Invoke();
            else
                OnCraftingUIClosed?.Invoke();
        }
    }
}
