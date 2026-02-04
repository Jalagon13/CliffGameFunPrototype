using System;
using System.Collections.Generic;
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
        public Vector2 DesiredMoveDirection { get; private set; }
        
        private Vector3 _moveDirection;
        private bool _isJumping;

        private void Awake()
        {
            _context = GetComponent<Player>();
        }

        private void Start()
        {
            GameInput.Instance.OnMove += GameInput_OnMove;
            CraftingManager.Instance.OnCraftingUIOpened += CraftingManager_OnCraftingUIOpened;
            Player.Instance.OnStateChanged += OnStateChange;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnMove -= GameInput_OnMove;
            CraftingManager.Instance.OnCraftingUIOpened -= CraftingManager_OnCraftingUIOpened;
            Player.Instance.OnStateChanged -= OnStateChange;
        }

        public void StateFixedUpdate()
        {
            MovePlayer();
            Jump();

            HandleWind();
            HandleFallTracking();
        }

        private void Jump()
        {
            if(GameInput.Instance.IsHoldingDownJump && _characterController.isGrounded && !_isJumping)
            {
                _verticalVelocity = _jumpForce;
                _isJumping = true;
            }
            
            if(_isJumping && _characterController.isGrounded)
            {
                _isJumping = false;
            }
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            if (CraftingManager.Instance.IsCraftingUIOpen) return;

            DesiredMoveDirection = e.ReadValue<Vector2>();
        }

        private void OnStateChange(PlayerMoveState state1, PlayerMoveState state2)
        {
            if(state2 == PlayerMoveState.Dead)
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

        private void MovePlayer()
        {
            Vector3 moveVector = transform.forward * DesiredMoveDirection.y + transform.right * DesiredMoveDirection.x;
            moveVector = moveVector * _walkSpeed * Time.deltaTime;
            _characterController.Move(moveVector);
            
            if(!_characterController.isGrounded)
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
            // bool isGrounded = _groundCheck.IsGrounded;
            // bool isFallingNow = !isGrounded && _rigidbody.linearVelocity.y < -0.01f;

            // // ---- FALL START ----
            // if (isFallingNow && !_isFallingFlag)
            // {
            //     _isFallingFlag = true;
            //     _fallStartY = transform.position.y;
            // }

            // // ---- FALL END (LANDING) ----
            // if (!isFallingNow && _isFallingFlag && isGrounded)
            // {
            //     float fallEndY = transform.position.y;
            //     float distanceFallen = _fallStartY - fallEndY;

            //     _isFallingFlag = false;
                
            //     if(distanceFallen > _fallDamageThreshold)
            //     {
            //         float damage = (distanceFallen - _fallDamageThreshold) * _fallDamageMultiplier;
            //         int finalDamage = Mathf.RoundToInt(damage);

            //         HealthManager.Instance.DamageHealth(finalDamage);
            //         // Debug.Log($"Fall dmg: {finalDamage}, dist fell: {distanceFallen}");
            //     }
            // }
        }
    }
}
