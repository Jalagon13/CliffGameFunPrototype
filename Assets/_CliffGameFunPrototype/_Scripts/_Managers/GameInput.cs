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
        public event EventHandler<InputAction.CallbackContext> OnToggleClimb;

        private PlayerInput _playerInput;

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
            _playerInput.Player.ToggleClimb.started += PlayerInput_OnToggleClimb;
            _playerInput.Player.ToggleClimb.canceled += PlayerInput_OnToggleClimb;
        }

        public void OnDestroy()
        {
            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnToggleClimb(InputAction.CallbackContext context)
        {
            OnToggleClimb?.Invoke(this, context);
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
    }
}
