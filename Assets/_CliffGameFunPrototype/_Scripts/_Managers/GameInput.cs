using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnMove;

        private PlayerInput _playerInput;

        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();

            _playerInput.Player.Move.started += PlayerInput_OnMove;
            _playerInput.Player.Move.performed += PlayerInput_OnMove;
            _playerInput.Player.Move.canceled += PlayerInput_OnMove;
        }

        public void OnDestroy()
        {
            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnMove(InputAction.CallbackContext context)
        {
            Debug.Log($"Move Input: {context.ReadValue<Vector2>()}");
        
            OnMove?.Invoke(this, context);
        }
    }
}
