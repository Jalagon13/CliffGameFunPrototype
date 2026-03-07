using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class WalkingMoveState : MonoBehaviour, IPlayerState
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 6f;
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _terminalVelocity = -50f;
        [SerializeField] private float _inAirMoveMultiplier = 0.5f;
        [SerializeField] private float _jumpCooldown = 0.2f;
        [SerializeField] private float _jumpWindStrengthMulti = 1.5f;
        [SerializeField] private float _windNegationThreshold = 0.25f;
        [SerializeField] private float _windAccumulationThreshold = 0.002f;
        [SerializeField] private float _stickToGroundForce = -5f;
        [SerializeField] private float _coyoteTime = 0.15f;
        [SerializeField] private float _minAirTimeForLandingSFX = 0.2f;
        private float _verticalVelocity;
        private CharacterController _cc;
    
        [Header("Falling Settings")]
        [SerializeField, Tooltip("How far you can fall for free (no damage)")] 
        private float _fallDamageThreshold = 5f;
        [SerializeField, Tooltip("How hard each extra meter of falling hurts")] 
        private float _fallDamageMultiplier = 2f;
        [SerializeField, Tooltip("Distance fallen that causes instant death")]
        private float _lethalFallDistance = 90f;
        private float _fallStartY;
        
        private bool _isFallingFlag;
        public bool IsFalling => _isFallingFlag;


        [HideInInspector]
        public Vector3 DesiredMoveDirection { get; private set; }
        
        private bool _isGrounded, _isSliding;
        private Timer _jumpCooldownTimer;
        private EventInstance _stepsInstance;
        private ControllerColliderHit _ccHit;
        private Vector3 _accumulatedWind;
        private float _airTime;

        private void Awake()
        {
            _jumpCooldownTimer = new Timer(_jumpCooldown);
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            GameInput.Instance.OnMove += GameInput_OnMove;
            CraftingManager.Instance.OnCraftingUIOpened += CraftingManager_OnCraftingUIOpened;
            Player.Instance.OnStateChanged += OnStateChange;

            _stepsInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.StepsSFX);
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnMove -= GameInput_OnMove;
            CraftingManager.Instance.OnCraftingUIOpened -= CraftingManager_OnCraftingUIOpened;
            Player.Instance.OnStateChanged -= OnStateChange;

            _stepsInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _stepsInstance.release();
        }

        public void StateUpdate()
        {
            WalkStateHandler();
            HandleWind(); // Temp delete in case I need it later
            MovePlayer();
            Jump();
            HandleFallTracking();
            HandleFootsteps();
            // Debug.Log($"Issliding: {_isSliding}, IsGrounded: {_isGrounded}");
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _ccHit = hit;
        }

        private void WalkStateHandler()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = _cc.isGrounded;
            _isSliding = _isGrounded ? Vector3.Angle(Vector3.up, _ccHit.normal) >= _cc.slopeLimit && _ccHit.collider.gameObject.layer == 6 : false;

            if (!_isGrounded)
            {
                _airTime += Time.deltaTime;
            }

            if (!wasGrounded && _isGrounded)
            {
                if (_airTime > _minAirTimeForLandingSFX)
                {
                    _jumpCooldownTimer.Reset();
                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.LandingSFX, transform.position);
                }
                _airTime = 0f;
            }
            else if (_isGrounded)
            {
                _airTime = 0f;
            }
        }

        private void Jump()
        {
            _jumpCooldownTimer.Tick(Time.deltaTime);

            if ((_isGrounded || _airTime < _coyoteTime) && !_isSliding)
            {
                if (GameInput.Instance.IsHoldingDownJump && _jumpCooldownTimer.RemainingSeconds <= 0f)
                {
                    _verticalVelocity = _jumpForce;
                    _airTime = _coyoteTime * 2f; // Prevent double jumping

                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.JumpSFX, transform.position);
                }
            }
        }

        private void MovePlayer()
        {
            Vector3 finalMove = Vector3.zero;

            if (_isSliding)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, _ccHit.normal).normalized;

                float slideSpeed = Mathf.Abs(_verticalVelocity);

                finalMove += downhill * slideSpeed * Time.deltaTime;
            }
            else
            {
                Vector3 horizontalMove = transform.forward * DesiredMoveDirection.y + transform.right * DesiredMoveDirection.x;
                float speed = GameInput.Instance.IsHoldingDownSprint ? _sprintSpeed : _walkSpeed;
                horizontalMove *= speed * (_isGrounded ? 1 : _inAirMoveMultiplier) * Time.deltaTime;
                
                finalMove += horizontalMove;
            }

            if (_accumulatedWind.sqrMagnitude > _windAccumulationThreshold * _windAccumulationThreshold)
            {
                finalMove += _accumulatedWind;
                _accumulatedWind = Vector3.zero;
            }
            
            _verticalVelocity += _gravity * Time.deltaTime;
            
            if(_isGrounded)
            {
                if(!_isSliding && _verticalVelocity < 0f)
                {
                    _verticalVelocity = _stickToGroundForce;
                }
            }
            else
            {
                if(_verticalVelocity < _terminalVelocity)
                {
                    _verticalVelocity = _terminalVelocity;
                }
            }
            
            finalMove += Vector3.up * _verticalVelocity * Time.deltaTime;
            
            _cc.Move(finalMove);

            if ((_cc.collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }
        }

        private void HandleWind()
        {
            if (WindManager.Instance == null || !WindManager.Instance.WindCanPushPlayer || Player.Instance.FirstPersonLook.IsSequenceOngoing)
            {
                _accumulatedWind = Vector3.zero;
                return;
            }

            float windSeverity = WindManager.Instance.WindSeverity > WindManager.Instance.WindPushesPlayerThreshold ? WindManager.Instance.WindSeverity : 0f;

            if (windSeverity <= 0f)
            {
                _accumulatedWind = Vector3.zero;
                return;
            }

            float windSpeed = WindManager.Instance.MaxWindForceAtFullSeverity * windSeverity;

            if (EquipmentSlotUI.PREVENT_WIND_WITH_BOOTS)
            {
                windSpeed *= 0.35f;
                if (windSpeed < _windNegationThreshold)
                {
                    windSpeed = 0f;
                }
            }

            Vector3 windDirection = Vector3.right; // test direction for now

            // Optional: reduce wind effect while grounded
            float groundedMultiplier = _isGrounded ? 1f : _jumpWindStrengthMulti;
            _accumulatedWind += windDirection * windSpeed * groundedMultiplier * Time.deltaTime;
        }

        private void HandleFallTracking()
        {
            bool isGrounded = _isGrounded;

            // Falling = not grounded AND moving downward
            bool isFallingNow = !isGrounded && _verticalVelocity < -0.1f;

            // ---- FALL START ----
            if (isFallingNow && !_isFallingFlag)
            {
                _isFallingFlag = true;
                _fallStartY = transform.position.y;
            }

            if (_isFallingFlag)
            {
                if (_fallStartY - transform.position.y >= _lethalFallDistance)
                {
                    HealthManager.Instance.DamageHealth(1000);
                    _isFallingFlag = false;
                    return;
                }
            }

            // ---- FALL END (LANDING) ----
            if (_isFallingFlag && isGrounded && !_isSliding)
            {
                float fallEndY = transform.position.y;
                float distanceFallen = _fallStartY - fallEndY;

                _isFallingFlag = false;

                if (distanceFallen > _fallDamageThreshold)
                {
                    float damage =
                        (distanceFallen - _fallDamageThreshold) * _fallDamageMultiplier;

                    int finalDamage = Mathf.RoundToInt(damage);
                    HealthManager.Instance.DamageHealth(finalDamage);

                    // Optional debug
                    Debug.Log($"Fall damage: {finalDamage}, Distance: {distanceFallen}");
                }
            }
        }

        private void HandleFootsteps()
        {
            bool isMoving = DesiredMoveDirection.sqrMagnitude > 0f;

            if (isMoving && _isGrounded && !_isSliding)
            {
                _stepsInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
                if (playbackState == PLAYBACK_STATE.STOPPED)
                {
                    _stepsInstance.start();
                }
                _stepsInstance.setPaused(false);
            }
            else
            {
                _stepsInstance.setPaused(true);
            }
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (CraftingManager.Instance.IsCraftingUIOpen || Player.Instance.FirstPersonLook.IsSequenceOngoing) return;

            DesiredMoveDirection = e.ReadValue<Vector2>();
        }

        private void OnStateChange(PlayerMoveState state1, PlayerMoveState state2)
        {
            if (state2 == PlayerMoveState.Dead)
            {
                DesiredMoveDirection = Vector2.zero;
            }
        }

        private void CraftingManager_OnCraftingUIOpened(bool useCraftingTableRecipes)
        {
            DesiredMoveDirection = Vector2.zero;
            _stepsInstance.setPaused(true);
        }

        public void EnterState()
        {
            // Debug.Log($"Entered Walk State");
            DesiredMoveDirection = Vector2.zero;
        }

        public void ExitState()
        {
            // Debug.Log($"Exited Walk State with velocity: {_captureExitVelocity}");
            DesiredMoveDirection = Vector2.zero;
        }
    }
}
