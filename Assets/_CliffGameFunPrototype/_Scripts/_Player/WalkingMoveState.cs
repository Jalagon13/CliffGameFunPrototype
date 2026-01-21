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
        public GroundCheck GroundCheck => _groundCheck;

        [Header("Running")]
        public bool canRun = true;
        public bool IsRunning { get; private set; }
        public float runSpeed = 9f;

        [Header("Physics Settings")]
        [SerializeField] 
        private float _accelLerpSpeed = 14f;
        
        [SerializeField] 
        private float _decelLerpSpeed = 10f;
        
        public float AccelerationRate = 40f;
        public float AirMultiplier = 0.4f;

        [Header("Falling Settings")]
        [SerializeField, Tooltip("How far you can fall for free (no damage)")] 
        private float _fallDamageThreshold = 5f;
        [SerializeField, Tooltip("How hard each extra meter of falling hurts")] 
        private float _fallDamageMultiplier = 2f;
        private float _fallStartY;
        
        private bool _isFallingFlag;
        public bool IsFalling => _isFallingFlag;

        private Player _context;
        private Rigidbody _rigidbody;
        private Vector3 _smoothedDesiredVelocity;

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
            // ---- BASIC ACCELERATION-BASED WALKING ----
            float targetSpeed = IsRunning ? runSpeed : speed;

            // Input direction in world space
            Vector3 inputDir =
                transform.forward * DesiredMoveDirection.y +
                transform.right * DesiredMoveDirection.x;

            // Prevent faster diagonal movement
            inputDir = Vector3.ClampMagnitude(inputDir, 1f);

            bool hasInput = inputDir.sqrMagnitude > 0.0001f;

            // Raw desired velocity from input
            Vector3 rawDesiredVelocity = inputDir * targetSpeed;

            // Reduce control in air
            float controlMultiplier = _groundCheck.IsGrounded ? 1f : AirMultiplier;
            rawDesiredVelocity *= controlMultiplier;

            // Smooth desired velocity (Mario-style micro inertia)
            float lerpSpeed = hasInput ? _accelLerpSpeed : _decelLerpSpeed;

            _smoothedDesiredVelocity = Vector3.Lerp(
                _smoothedDesiredVelocity,
                rawDesiredVelocity,
                lerpSpeed * Time.fixedDeltaTime
            );

            // Current horizontal velocity (ignore Y)
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 currentHorizontalVelocity =
                new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            // Accelerate toward smoothed desired velocity
            Vector3 velocityDelta = _smoothedDesiredVelocity - currentHorizontalVelocity;
            Vector3 acceleration = velocityDelta * AccelerationRate;

            _rigidbody.AddForce(acceleration, ForceMode.Acceleration);

            // ---- WIND FORCE (UNCHANGED) ----
            if (WindManager.Instance != null && WindManager.Instance.WindCanPushPlayer)
            {
                float windSeverity =
                    WindManager.Instance.WindSeverity > WindManager.Instance.WindPushesPlayerThreshold
                        ? WindManager.Instance.WindSeverity
                        : 0f;

                float windForceOnPlayer =
                    WindManager.Instance.MaxWindForceAtFullSeverity * windSeverity;

                if (EquipmentSlotUI.PREVENT_WIND_WITH_BOOTS)
                {
                    windForceOnPlayer -= WindManager.Instance.MaxWindForceAtFullSeverity * 0.6667f;
                    windForceOnPlayer = Mathf.Max(0f, windForceOnPlayer);
                }

                Vector3 windForce = Vector3.right * windForceOnPlayer;
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
            if (CraftingManager.Instance.IsCraftingUIOpen) return;

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
