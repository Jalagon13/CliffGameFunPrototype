using System;
using System.Collections;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum CardinalDireciton
    {
        North,
        South,
        East,
        West
    }

    public enum PlayerMoveState
    {
        Walking,
        Dead
    }

    [RequireComponent(typeof(WalkingMoveState))]
    [RequireComponent(typeof(DeadState))]
    public class Player : MonoBehaviour
    {
        public static Player Instance;
        
        public Action<PlayerMoveState, PlayerMoveState> OnStateChanged;
        public event Action OnPlayerRespawn;

        private Dictionary<PlayerMoveState, IPlayerState> _states;
        
        private IPlayerState _currentState;
        public PlayerMoveState CurrentMoveStateType { get; private set; }

        private Camera _playerCamera;
        public Camera PlayerCamera => _playerCamera;

        [SerializeField]
        private Transform _respawnTransform;

        [SerializeField] 
        private float _climbRayDistance = 2f;
        public float ClimbRayDistance => _climbRayDistance;
        
        [SerializeField] 
        private LayerMask _climbableLayer;
        
        private WalkingMoveState _walkingMoveState;
        public WalkingMoveState WalkingMoveState => _walkingMoveState;
        
        private DeadState _deadState;
        
        [SerializeField] 
        private ToolHolder _toolHolder;
        public ToolHolder ToolHolder => _toolHolder;
        
        [field: SerializeField]
        public PauseMenuUI PauseMenuUI { get; private set; }
        
        public CardinalDireciton PlayerFacingDirection { get; private set; }
        
        private void Awake()
        {
            Instance = this;

            _walkingMoveState = GetComponent<WalkingMoveState>();
            _deadState = GetComponent<DeadState>();
            _playerCamera = Camera.main;

            _states = new Dictionary<PlayerMoveState, IPlayerState>
            {
                { PlayerMoveState.Walking, _walkingMoveState },
                { PlayerMoveState.Dead, _deadState }
            };

            TransitionState(PlayerMoveState.Walking);
        }
        
        private void Start()
        {
            // GameInput.Instance.OnPrimaryInteract += GameInput_OnPrimaryInteract;
            HealthManager.Instance.OnPlayerDeath += HealthManager_OnPlayerDeath;
            
        }
        
        private void OnDestroy()
        {
            // GameInput.Instance.OnPrimaryInteract -= GameInput_OnPrimaryInteract;
            HealthManager.Instance.OnPlayerDeath -= HealthManager_OnPlayerDeath;
            
        }
        
        private void Update()
        {
            _currentState.StateFixedUpdate();

            Vector3 camForward = _playerCamera.transform.forward;
            camForward.y = 0f;                 // ignore vertical tilt
            camForward.Normalize();

            float northDot = Vector3.Dot(camForward, Vector3.forward); // +Z
            float southDot = Vector3.Dot(camForward, Vector3.back);    // -Z
            float eastDot = Vector3.Dot(camForward, Vector3.right);   // +X
            float westDot = Vector3.Dot(camForward, Vector3.left);    // -X

            float maxDot = Mathf.Max(northDot, southDot, eastDot, westDot);

            if (maxDot == northDot)
                PlayerFacingDirection = CardinalDireciton.North;
            else if (maxDot == southDot)
                PlayerFacingDirection = CardinalDireciton.South;
            else if (maxDot == eastDot)
                PlayerFacingDirection = CardinalDireciton.East;
            else
                PlayerFacingDirection = CardinalDireciton.West;
            // Debug.Log($"Player facing direction: {PlayerFacingDirection}");
        }

        private void FixedUpdate()
        {
            
        }

        private void HealthManager_OnPlayerDeath()
        {
            TransitionState(PlayerMoveState.Dead);
        }

        public void RespawnButtonPressed()
        {
            TransitionState(PlayerMoveState.Walking);
        
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

        public void TransitionState(PlayerMoveState playerMoveState)
        {
            PlayerMoveState previousState = CurrentMoveStateType;

            _currentState?.ExitState();
            _currentState = _states[playerMoveState];
            _currentState?.EnterState();

            CurrentMoveStateType = playerMoveState;
            
            OnStateChanged?.Invoke(previousState, playerMoveState);
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
