using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum PlayerMoveState
    {
        Walking,
        Climbing
    }

    [RequireComponent(typeof(ClimbMoveState))]
    [RequireComponent(typeof(WalkingMoveState))]
    public class Player : MonoBehaviour
    {
        public static Player Instance;
        
        public Action<PlayerMoveState, PlayerMoveState> OnMoveStateChanged;
    
        private Dictionary<PlayerMoveState, IPlayerState> _states;
        
        private IPlayerState _currentState;
        public PlayerMoveState CurrentMoveStateType { get; private set; }

        private Camera _playerCamera;
        
        [SerializeField] 
        private float _climbRayDistance = 2f;
        public float ClimbRayDistance => _climbRayDistance;
        
        [SerializeField] 
        private LayerMask _climbableLayer;
        
        private WalkingMoveState _walkingMoveState;
        public WalkingMoveState WalkingMoveState => _walkingMoveState;
        
        private ClimbMoveState _climbMoveState;
        public ClimbMoveState ClimbMoveState => _climbMoveState;
        
        private void Awake()
        {
            Instance = this;
            
            _walkingMoveState = GetComponent<WalkingMoveState>();
            _climbMoveState = GetComponent<ClimbMoveState>();
            _playerCamera = Camera.main;

            _states = new Dictionary<PlayerMoveState, IPlayerState>
            {
                { PlayerMoveState.Walking, _walkingMoveState },
                { PlayerMoveState.Climbing, _climbMoveState }
            };

            TransitionState(PlayerMoveState.Walking);
        }
        
        private void Start()
        {
            GameInput.Instance.OnToggleClimb += GameInput_OnToggleClimb;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnToggleClimb -= GameInput_OnToggleClimb;
        }

        private void FixedUpdate()
        {
            _currentState.StateFixedUpdate();
        }

        private void GameInput_OnToggleClimb(object sender, InputAction.CallbackContext e)
        {
            if(CurrentMoveStateType == PlayerMoveState.Walking)
            {
                if(e.started)
                {
                    // If I am walking, and i hit the toggle climb button, shoot ray logic

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
                    // If I am climbing, and i release the toggle climb button, try to switch to walking.
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
