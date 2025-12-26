using System;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnMove;
        public event EventHandler<InputAction.CallbackContext> OnLook;
        public event EventHandler<InputAction.CallbackContext> OnJump;
        public event EventHandler<InputAction.CallbackContext> OnScrollWheel;
        public event EventHandler<InputAction.CallbackContext> OnSelectSlot;
        public event EventHandler<InputAction.CallbackContext> OnToggleCraftingMenu;
        
        public event EventHandler<InputAction.CallbackContext> OnPrimaryInteract;
        public event EventHandler<InputAction.CallbackContext> OnSecondaryInteract;
        public event EventHandler<InputAction.CallbackContext> OnTertiaryInteract;

        private PlayerInput _playerInput;
        
        public bool IsHoldingDownSecondaryInteract { get; private set; }
        public bool IsHoldingDownPrimaryInteract { get; private set; }

        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();

            _playerInput.Player.Move.started += PlayerInput_OnMove;
            _playerInput.Player.Move.performed += PlayerInput_OnMove;
            _playerInput.Player.Move.canceled += PlayerInput_OnMove;
            _playerInput.Player.Look.started += PlayerInput_OnLook;
            _playerInput.Player.Look.performed += PlayerInput_OnLook;
            _playerInput.Player.Look.canceled += PlayerInput_OnLook;
            _playerInput.Player.Jump.started += PlayerInput_OnJump;
            _playerInput.Player.ToggleCraftingMenu.started += PlayerInput_OnToggleCraftingMenu;
            _playerInput.Player.PrimaryInteract.started += PlayerInput_OnPrimaryInteract;
            _playerInput.Player.PrimaryInteract.canceled += PlayerInput_OnPrimaryInteract;
            _playerInput.Player.SecondaryInteract.started += PlayerInput_OnSecondaryInteract;
            _playerInput.Player.SecondaryInteract.canceled += PlayerInput_OnSecondaryInteract;
            _playerInput.Player.TertiaryInteract.started += PlayerInput_OnTertiaryInteract;
            _playerInput.Player.TertiaryInteract.canceled += PlayerInput_OnTertiaryInteract;

            _playerInput.UI.ScrollWheel.performed += PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started += PlayerInput_OnSelectSlot;
        }

        public void OnDestroy()
        {
            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnTertiaryInteract(InputAction.CallbackContext context)
        {
            OnTertiaryInteract?.Invoke(this, context);
        }

        private void PlayerInput_OnPrimaryInteract(InputAction.CallbackContext context)
        {
            if(context.started)
            {
                IsHoldingDownPrimaryInteract = true;
            }
            else if(context.canceled)
            {
                IsHoldingDownPrimaryInteract = false;
            }

            OnPrimaryInteract?.Invoke(this, context);
        }

        private void PlayerInput_OnSecondaryInteract(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                IsHoldingDownSecondaryInteract = true;
            }
            else if (context.canceled)
            {
                IsHoldingDownSecondaryInteract = false;
            }

            OnSecondaryInteract?.Invoke(this, context);
        }

        private void PlayerInput_OnToggleCraftingMenu(InputAction.CallbackContext context)
        {
            OnToggleCraftingMenu?.Invoke(this, context);
        }

        private void PlayerInput_OnJump(InputAction.CallbackContext context)
        {
            OnJump?.Invoke(this, context);
        }

        private void PlayerInput_OnLook(InputAction.CallbackContext context)
        {
            OnLook?.Invoke(this, context);
        }

        private void PlayerInput_OnMove(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(this, context);
        }

        private void PlayerInput_OnScrollWheel(InputAction.CallbackContext context)
        {
            OnScrollWheel?.Invoke(this, context);
        }

        private void PlayerInput_OnSelectSlot(InputAction.CallbackContext context)
        {
            OnSelectSlot?.Invoke(this, context);
        }
    }
}
