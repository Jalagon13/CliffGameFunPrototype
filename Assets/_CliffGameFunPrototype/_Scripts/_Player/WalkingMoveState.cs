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
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _inAirMoveMultiplier = 0.5f;
        [SerializeField] private float _jumpCooldown = 0.5f;
        private float _verticalVelocity;
        
    
        [Header("Falling Settings")]
        [SerializeField, Tooltip("How far you can fall for free (no damage)")] 
        private float _fallDamageThreshold = 5f;
        [SerializeField, Tooltip("How hard each extra meter of falling hurts")] 
        private float _fallDamageMultiplier = 2f;
        private float _fallStartY;
        
        private bool _isFallingFlag;
        public bool IsFalling => _isFallingFlag;

        private Player _context;

        [HideInInspector]
        public Vector3 DesiredMoveDirection { get; private set; }
        
        private bool _isJumping;
        private bool _wasGrounded;
        private Timer _jumpCooldownTimer;
        private EventInstance _stepsInstance;

        private void Awake()
        {
            _jumpCooldownTimer = new Timer(_jumpCooldown);
            _context = GetComponent<Player>();
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
            TransitionCheck();
            MovePlayer();
            Jump();
            
            HandleFootsteps();
            HandleWind();
            HandleFallTracking();
        }

        private void TransitionCheck()
        {
            bool isGrounded = _characterController.isGrounded;

            // Grounded -> AIR
            if (_wasGrounded && !isGrounded)
            {
                OnLeftGround();
            }

            // Air -> GROUNDED
            if (!_wasGrounded && isGrounded)
            {
                OnLanded();
            }

            _wasGrounded = isGrounded;
        }

        private void OnLeftGround()
        {
            // Debug.Log("Left ground");
            
            if(!_isJumping)
            {
                _verticalVelocity = 0f;
            }
        }

        private void OnLanded()
        {
            // Debug.Log("Landed");
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.LandingSFX, transform.position);
        }

        private void Jump()
        {
            _jumpCooldownTimer.Tick(Time.deltaTime);

            if (GameInput.Instance.IsHoldingDownJump && _characterController.isGrounded && !_isJumping && _jumpCooldownTimer.RemainingSeconds <= 0f)
            {
                _verticalVelocity = _jumpForce;
                _isJumping = true;

                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.JumpSFX, transform.position);
            }

            // Only trigger when we were jumping and just touched ground while falling
            if (_isJumping && _characterController.isGrounded && _verticalVelocity <= 0f)
            {
                _isJumping = false;
                _jumpCooldownTimer.Reset();
                _verticalVelocity = -2f;
            }
        }

        private void MovePlayer()
        {
            Vector3 moveVector = transform.forward * DesiredMoveDirection.y + transform.right * DesiredMoveDirection.x;
            moveVector = moveVector * _walkSpeed * (_characterController.isGrounded ? 1 : _inAirMoveMultiplier) * Time.deltaTime;
            _characterController.Move(moveVector);

            if (!_characterController.isGrounded)
            {
                _verticalVelocity = _verticalVelocity + (_gravity * Time.deltaTime);
                _characterController.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
            }
        }

        private void HandleWind()
        {
            // ---- WIND FORCE (UNCHANGED) ----
            // if (WindManager.Instance != null && WindManager.Instance.WindCanPushPlayer)
            // {
            //     float windSeverity =
            //         WindManager.Instance.WindSeverity > WindManager.Instance.WindPushesPlayerThreshold
            //             ? WindManager.Instance.WindSeverity
            //             : 0f;

            //     float windForceOnPlayer =
            //         WindManager.Instance.MaxWindForceAtFullSeverity * windSeverity;

            //     if (EquipmentSlotUI.PREVENT_WIND_WITH_BOOTS)
            //     {
            //         windForceOnPlayer -= WindManager.Instance.MaxWindForceAtFullSeverity * 0.6667f;
            //         windForceOnPlayer = Mathf.Max(0f, windForceOnPlayer);
            //     }

            //     Vector3 windForce = Vector3.right * windForceOnPlayer;
            //     _rigidbody.AddForce(windForce, ForceMode.Acceleration);
            // }
        }

        private void HandleFallTracking()
        {
            bool isGrounded = _characterController.isGrounded;

            // Falling = not grounded AND moving downward
            bool isFallingNow = !isGrounded && _verticalVelocity < -0.1f;

            // ---- FALL START ----
            if (isFallingNow && !_isFallingFlag)
            {
                _isFallingFlag = true;
                _fallStartY = transform.position.y;
            }

            // ---- FALL END (LANDING) ----
            if (_isFallingFlag && isGrounded)
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
            bool isGrounded = _characterController.isGrounded;
            bool isMoving = DesiredMoveDirection.sqrMagnitude > 0.01f;

            if (isGrounded && isMoving)
            {
                StartSteps();
            }
            else
            {
                PauseSteps();
            }
        }

        private void StartSteps()
        {
            PLAYBACK_STATE state;
            _stepsInstance.getPlaybackState(out state);

            if (state == PLAYBACK_STATE.STOPPED)
                _stepsInstance.start();
            else
                _stepsInstance.setPaused(false);
        }

        private void PauseSteps()
        {
            _stepsInstance.setPaused(true);
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (CraftingManager.Instance.IsCraftingUIOpen) return;

            DesiredMoveDirection = e.ReadValue<Vector2>();
        }

        private void OnStateChange(PlayerMoveState state1, PlayerMoveState state2)
        {
            if (state2 == PlayerMoveState.Dead)
            {
                DesiredMoveDirection = Vector2.zero;
            }
        }

        private void CraftingManager_OnCraftingUIOpened()
        {
            DesiredMoveDirection = Vector2.zero;
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
