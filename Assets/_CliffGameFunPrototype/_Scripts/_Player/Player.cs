using System;
using System.Collections;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum PlayerMoveState
    {
        Walking,
        Climbing,
        Dead
    }

    [RequireComponent(typeof(ClimbMoveState))]
    [RequireComponent(typeof(WalkingMoveState))]
    [RequireComponent(typeof(DeadState))]
    public class Player : MonoBehaviour
    {
        public static Player Instance;
        
        public Action<PlayerMoveState, PlayerMoveState> OnMoveStateChanged;
        public event Action OnPlayerRespawn;

        private Dictionary<PlayerMoveState, IPlayerState> _states;
        
        private IPlayerState _currentState;
        public PlayerMoveState CurrentMoveStateType { get; private set; }

        private Camera _playerCamera;

        [SerializeField]
        private Transform _respawnTransform;

        [SerializeField] 
        private float _climbRayDistance = 2f;
        public float ClimbRayDistance => _climbRayDistance;
        
        [SerializeField] 
        private LayerMask _climbableLayer;
        
        private WalkingMoveState _walkingMoveState;
        public WalkingMoveState WalkingMoveState => _walkingMoveState;
        
        private ClimbMoveState _climbMoveState;
        public ClimbMoveState ClimbMoveState => _climbMoveState;
        
        private DeadState _deadState;
        
        public Rigidbody RigidBody { get; private set; }
        
        private void Awake()
        {
            Instance = this;

            RigidBody = GetComponent<Rigidbody>();
            _walkingMoveState = GetComponent<WalkingMoveState>();
            _climbMoveState = GetComponent<ClimbMoveState>();
            _deadState = GetComponent<DeadState>();
            _playerCamera = Camera.main;

            _states = new Dictionary<PlayerMoveState, IPlayerState>
            {
                { PlayerMoveState.Walking, _walkingMoveState },
                { PlayerMoveState.Climbing, _climbMoveState },
                { PlayerMoveState.Dead, _deadState }
            };

            TransitionState(PlayerMoveState.Walking);
        }
        
        private void Start()
        {
            GameInput.Instance.OnToggleClimb += GameInput_OnToggleClimb;
            HealthManager.Instance.OnPlayerDeath += HealthManager_OnPlayerDeath;
            CraftingManager.Instance.OnCraftingUIOpened += CraftingManager_OnCraftingUIOpened;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnToggleClimb -= GameInput_OnToggleClimb;
            HealthManager.Instance.OnPlayerDeath -= HealthManager_OnPlayerDeath;
            CraftingManager.Instance.OnCraftingUIOpened -= CraftingManager_OnCraftingUIOpened;
        }

        private void FixedUpdate()
        {
            _currentState.StateFixedUpdate();
        }

        private void HealthManager_OnPlayerDeath()
        {
            TransitionState(PlayerMoveState.Dead);
        }

        private void CraftingManager_OnCraftingUIOpened()
        {
            WalkingMoveState.DesiredMoveDirection = Vector2.zero;
            ClimbMoveState.DesiredMoveDirection = Vector2.zero;
        }

        public void RespawnButtonPressed()
        {
            TransitionState(PlayerMoveState.Walking);
        
            RigidBody.constraints = RigidbodyConstraints.None;
            RigidBody.freezeRotation = true;
            RigidBody.linearVelocity = Vector3.zero;
            RigidBody.angularVelocity = Vector3.zero;

            StartCoroutine(RespawnAtCorrectPosition());

            OnPlayerRespawn?.Invoke();
        }

        private IEnumerator RespawnAtCorrectPosition()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                transform.SetPositionAndRotation(_respawnTransform.position, Quaternion.identity);
            }
        }

        private void GameInput_OnToggleClimb(object sender, InputAction.CallbackContext e)
        {
            if(CurrentMoveStateType == PlayerMoveState.Walking)
            {
                if(e.started)
                {
                    RaycastHit hit;
                    if(Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _climbRayDistance, _climbableLayer))
                    {
                        TransitionState(PlayerMoveState.Climbing);
                    }
                }
            }
            else if(CurrentMoveStateType == PlayerMoveState.Climbing)
            {
                if(e.canceled && !_climbMoveState.IsLerpingToLedge)
                {
                    TransitionState(PlayerMoveState.Walking);
                }
            }
        }

        public void TransitionState(PlayerMoveState playerMoveState)
        {
            PlayerMoveState previousState = CurrentMoveStateType;

            _currentState?.ExitState();
            _currentState = _states[playerMoveState];
            _currentState?.EnterState();

            CurrentMoveStateType = playerMoveState;
            
            OnMoveStateChanged?.Invoke(previousState, playerMoveState);
        }

        public IPlayerState GetState(PlayerMoveState key)
        {
            if (_states.TryGetValue(key, out var state))
            {
                return state;
            }

            Debug.LogWarning($"State {key} not found in state machine.");
            return null;
        }
    }
}
