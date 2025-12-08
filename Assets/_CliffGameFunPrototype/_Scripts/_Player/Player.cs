using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public enum PlayerMoveState
    {
        Walking,
        Climbing
    }

    [RequireComponent(typeof(FirstPersonClimbMovement))]
    [RequireComponent(typeof(FirstPersonMovement))]
    public class Player : MonoBehaviour
    {
        private Dictionary<PlayerMoveState, IPlayerState> _states;
        private IPlayerState _currentState;
        public IPlayerState CurrentMoveState => _currentState;
        
        private void Awake()
        {
            _states = new Dictionary<PlayerMoveState, IPlayerState>
            {
                { PlayerMoveState.Walking, GetComponent<FirstPersonMovement>() },
                { PlayerMoveState.Climbing, GetComponent<FirstPersonClimbMovement>() }
            };

            _currentState = _states[PlayerMoveState.Walking];
        }

        private void FixedUpdate()
        {
            _currentState.StateFixedUpdate();
        }

        public void TransitionState(PlayerMoveState playerMoveState)
        {
            _currentState.ExitState();
            _currentState = _states[playerMoveState];
            _currentState.EnterState();
        }
    }
}
