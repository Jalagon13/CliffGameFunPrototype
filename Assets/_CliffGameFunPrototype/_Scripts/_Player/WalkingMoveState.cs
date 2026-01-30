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
        public float AccelerationRate = 5f;
        [SerializeField] private float _groundDrag = 6f;
        [SerializeField] private float _maxSlopeAngle = 50f;

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

        [HideInInspector]
        public Vector2 DesiredMoveDirection;
        
        private Vector3 _captureExitVelocity;
        private RaycastHit _slopeHit;

        public Vector3 CaptureExitVelocity => _captureExitVelocity;

        private Vector3 _moveDirection;

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

        private void Update()
        {
            SpeedControl();

            if (_groundCheck.IsGrounded)
            {
                _rigidbody.linearDamping = _groundDrag;
            }
            else
            {
                _rigidbody.linearDamping = 0f;
            }
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (CraftingManager.Instance.IsCraftingUIOpen) return;

            DesiredMoveDirection = e.ReadValue<Vector2>();
        }

        public void EnterState()
        {
            // Debug.Log($"Entered Walk State");
        }

        public void ExitState()
        {
            _captureExitVelocity = _rigidbody.linearVelocity;
            // Debug.Log($"Exited Walk State with velocity: {_captureExitVelocity}");
        }

        public void StateFixedUpdate()
        {
            PlayerMovement();

            HandleWind();
            
            HandleFallTracking();
        }
        
        private void PlayerMovement()
        {
            float targetSpeed = IsRunning ? runSpeed : speed;

            // Input direction in world space
            _moveDirection = transform.forward * DesiredMoveDirection.y + transform.right * DesiredMoveDirection.x;

            if (OnSlope())
            {
                _rigidbody.AddForce(GetSlopeMoveDirection() * targetSpeed * AccelerationRate, ForceMode.Force);

                if (_rigidbody.linearVelocity.y < 0 && DesiredMoveDirection.sqrMagnitude != 0)
                {
                    _rigidbody.AddForce(Vector3.down * 80f, ForceMode.Force);
                }
            }

            if (_groundCheck.IsGrounded)
            {
                _rigidbody.AddForce(_moveDirection.normalized * (targetSpeed * AccelerationRate), ForceMode.Force);
            }

            _rigidbody.useGravity = !OnSlope();
        }
        
        private void SpeedControl()
        {
            if(OnSlope())
            {
                if(_rigidbody.linearVelocity.magnitude > speed)
                {
                    _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * speed;
                }
            }
            else
            {
                Vector3 flatVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

                // limit velocity if needed
                if (flatVel.magnitude > speed)
                {
                    Vector3 clampedVel = flatVel.normalized * speed;
                    _rigidbody.linearVelocity = new Vector3(clampedVel.x, _rigidbody.linearVelocity.y, clampedVel.z);
                }
            }
        }
        
        private bool OnSlope()
        {
            if(Physics.Raycast(transform.position, Vector3.down, out _slopeHit, 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                return angle < _maxSlopeAngle && angle != 0;
            }
            
            return false;
        }
        
        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
        }

        private void HandleWind()
        {
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
