using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class FirstPersonClimbMovement : MonoBehaviour, IPlayerState
    {
        private Vector2 _desiredDirection;
        private Player _context;
        
        private void Awake()
        {
            _context = GetComponent<Player>();
        }

        private void Start()
        {
            GameInput.Instance.OnMove += GameInput_OnMove;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnMove -= GameInput_OnMove;
        }

        public void EnterState()
        {

        }

        public void StateFixedUpdate()
        {
            
        }

        public void ExitState()
        {

        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (!ReferenceEquals(_context.CurrentMoveState, this))
                return;

            _desiredDirection = e.ReadValue<Vector2>();
        }
    }
}
