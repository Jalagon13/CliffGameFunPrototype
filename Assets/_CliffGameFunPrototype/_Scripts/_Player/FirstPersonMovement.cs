using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonMovement : MonoBehaviour, IPlayerState
    {
        public float speed = 5f;

        [SerializeField] private GroundCheck _groundCheck;

        [Header("Running")]
        public bool canRun = true;
        public bool IsRunning { get; private set; }
        public float runSpeed = 9f;

        [Header("Physics Settings")]
        public float MovementForceMultiplier = 20f; // Reduced for Acceleration mode
        public float MaxHorizontalSpeed = 10f;
        public float AccelerationRate = 40f;
        public float AirMultiplier = 0.4f;

        private Player _context;
        private Rigidbody _rigidbody;
        private Vector2 _desiredDirection;

        private void Awake()
        {
            _context = GetComponent<Player>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
            _rigidbody.freezeRotation = true;
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
            // Determine target speed
            float targetSpeed = IsRunning ? runSpeed : speed;

            // Input direction in world space
            Vector3 desiredMove = transform.forward * _desiredDirection.y + transform.right * _desiredDirection.x;
            desiredMove *= targetSpeed;

            // Only horizontal velocity
            Vector3 horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

            // Apply AirMultiplier if not grounded
            float multiplier = _groundCheck.IsGrounded ? 1f : AirMultiplier;
            desiredMove *= multiplier;

            // Smoothly move towards desired velocity
            Vector3 smoothedVelocity = Vector3.MoveTowards(horizontalVelocity, desiredMove, AccelerationRate * Time.fixedDeltaTime);

            // Apply the velocity change as force
            Vector3 velocityChange = smoothedVelocity - horizontalVelocity;
            _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
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
