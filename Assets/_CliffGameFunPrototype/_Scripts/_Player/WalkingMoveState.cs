using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class WalkingMoveState : MonoBehaviour, IPlayerState
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

        [Header("Falling Settings")]
        [SerializeField, Tooltip("How far you can fall for free (no damage)")] 
        private float _fallDamageThreshold = 5f;
        [SerializeField, Tooltip("How hard each extra meter of falling hurts")] 
        private float _fallDamageMultiplier = 2f;
        private float _fallStartY;
        
        private bool _isFallingFlag;

        private Player _context;
        private Rigidbody _rigidbody;
        
        [HideInInspector]
        public Vector2 DesiredMoveDirection;
        
        private Vector3 _captureExitVelocity;
        public Vector3 CaptureExitVelocity => _captureExitVelocity;

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
            // Debug.Log($"Entered Walk State");
        }

        public void StateFixedUpdate()
        {
            // Determine target speed
            float targetSpeed = IsRunning ? runSpeed : speed;

            // Input direction in world space
            Vector3 desiredMove = transform.forward * DesiredMoveDirection.y + transform.right * DesiredMoveDirection.x;
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

            // Apply wind force in positive X direction. ONLY IN X DIRECTION RN FIX LATER
            if (WindManager.Instance != null)
            {
                float windSeverity = WindManager.Instance.WindSeverity > WindManager.Instance.WindPushesPlayerThreshold ? WindManager.Instance.WindSeverity : 0f;
                Vector3 windForce = Vector3.right * (WindManager.Instance.MaxWindForceAtFullSeverity * windSeverity);
                _rigidbody.AddForce(windForce, ForceMode.Acceleration);
            }

            HandleFallTracking();
        }

        public void ExitState()
        {
            _captureExitVelocity = _rigidbody.linearVelocity;
            // Debug.Log($"Exited Walk State with velocity: {_captureExitVelocity}");
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (CraftingManager.Instance.CraftingMenuUIOpened) return;

            DesiredMoveDirection = e.ReadValue<Vector2>();
        }

        private void HandleFallTracking()
        {
            bool isGrounded = _groundCheck.IsGrounded;
            bool isFallingNow = !isGrounded && _rigidbody.linearVelocity.y < -0.01f;

            // ---- FALL START ----
            if (isFallingNow && !_isFallingFlag)
            {
                _isFallingFlag = true;
                _fallStartY = transform.position.y;
            }

            // ---- FALL END (LANDING) ----
            if (!isFallingNow && _isFallingFlag && isGrounded)
            {
                float fallEndY = transform.position.y;
                float distanceFallen = _fallStartY - fallEndY;

                _isFallingFlag = false;
                
                if(distanceFallen > _fallDamageThreshold)
                {
                    float damage = (distanceFallen - _fallDamageThreshold) * _fallDamageMultiplier;
                    int finalDamage = Mathf.RoundToInt(damage);

                    HealthManager.Instance.DamageHealth(finalDamage);
                    // Debug.Log($"Fall dmg: {finalDamage}, dist fell: {distanceFallen}");
                }
            }
        }
    }
}
