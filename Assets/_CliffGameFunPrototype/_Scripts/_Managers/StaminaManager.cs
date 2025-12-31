using System;
using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class StaminaManager : MonoBehaviour
    {
        public static StaminaManager Instance;

        public event Action<int, int> OnStaminaChanged;

        [SerializeField]
        private int _maxStamina = 100;

        [SerializeField]
        private float _climbMovingStaminaDecreasePerSecond = 4f, _climbIdleStaminaDecreasePerSecond = 1f, _staminaRegenPerSecond = 10f;

        private PlayerStat _staminaStat;
        public int CurrentStamina => _staminaStat.Current;

        private bool _isClimbing;
        
        [HideInInspector]
        public bool IsExhausted; // Cannot regenerate stamina 

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            _staminaStat = new PlayerStat(_maxStamina, _climbMovingStaminaDecreasePerSecond, _staminaRegenPerSecond);
            _staminaStat.OnValueChanged += (current, max) =>
            {
                OnStaminaChanged?.Invoke(current, max);
            };

            Player.Instance.OnMoveStateChanged += HandlePlayerStateChanged;
            Player.Instance.OnPlayerRespawn += OnRespawn;
            Player.Instance.WalkingMoveState.GroundCheck.Grounded += OnGrounded;

            yield return null;

            OnStaminaChanged?.Invoke(CurrentStamina, _staminaStat.Max);
        }

        private void OnDestroy()
        {
            Player.Instance.OnMoveStateChanged -= HandlePlayerStateChanged;
            Player.Instance.OnPlayerRespawn -= OnRespawn;
            Player.Instance.WalkingMoveState.GroundCheck.Grounded -= OnGrounded;
        }

        private void Update()
        {
            if (_staminaStat == null) return;

            if (_isClimbing)
            {
                if (Player.Instance.ClimbMoveState.IsMovingWhileClimbing)
                {
                    _staminaStat.SetDrainPerSecond(_climbMovingStaminaDecreasePerSecond);
                    _staminaStat.UpdateStat(Time.deltaTime, true);
                }
                else
                {
                    _staminaStat.SetDrainPerSecond(_climbIdleStaminaDecreasePerSecond);
                    _staminaStat.UpdateStat(Time.deltaTime, true);
                }

            }
            else if(!IsExhausted)
            {
                _staminaStat.UpdateStat(Time.deltaTime, false);
            }
        }

        private void OnGrounded()
        {
            if (IsExhausted)
            {
                IsExhausted = false;
            }
        }

        private void OnRespawn()
        {
            RestoreStamina(_maxStamina);
        }

        private void HandlePlayerStateChanged(PlayerMoveState previousState, PlayerMoveState currentState)
        {
            if (currentState == PlayerMoveState.Climbing)
            {
                _isClimbing = true;
            }
            else if (currentState == PlayerMoveState.Walking)
            {
                _isClimbing = false;
            }
        }

        public void RestoreStamina(int amount)
        {
            _staminaStat.ChangeCurrent(amount);
        }
    }
}
