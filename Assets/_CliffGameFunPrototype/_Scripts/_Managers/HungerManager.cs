using System;
using UnityEngine;
using CliffGame;
using System.Collections;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum HungerState
    {
        Full,   // When the player has a hunger higher than 0
        Hungry  // When hunger is 0
    }

    public class HungerManager : MonoBehaviour
    {
        public static HungerManager Instance { get; private set; }

        [SerializeField]
        private int _maxHunger = 100;
        [SerializeField]
        private float _hungerDrainPerSecond = 0.1f;

        private PlayerStat _hungerStat;
        public int CurrentHunger => _hungerStat.Current;

        public HungerState CurrentHungerState { get; private set; }

        public event Action<int, int> OnHungerChanged; // current, max
        private Timer _eatTimer;
        public Timer EatTimer => _eatTimer;
        
        private ConsumableItemSO _currentConsumable;
        private bool _isEating;
        public bool IsEating => _isEating;

        private void Awake()
        {
            Instance = this;

            _hungerStat = new PlayerStat(_maxHunger, _hungerDrainPerSecond, 0f);
            _hungerStat.OnValueChanged += (current, max) =>
            {
                OnHungerChanged?.Invoke(current, max);
                if (current <= 0)
                    CurrentHungerState = HungerState.Hungry;
                else
                    CurrentHungerState = HungerState.Full;
            };
        }

        private IEnumerator Start()
        {
            Player.Instance.OnPlayerRespawn += OnRespawn;
            GameInput.Instance.OnSecondaryInteract += TryToEat;

            yield return null;
            OnHungerChanged?.Invoke(CurrentHunger, _hungerStat.Max);
        }

        private void OnDestroy()
        {
            Player.Instance.OnPlayerRespawn -= OnRespawn;
            GameInput.Instance.OnSecondaryInteract -= TryToEat;
        }

        private void Update()
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            _hungerStat.UpdateStat(Time.deltaTime, true);

            if (_isEating && _eatTimer != null)
            {
                // Cancel eating if button released
                if (!GameInput.Instance.IsHoldingDownSecondaryInteract)
                {
                    CancelEating();
                    return;
                }

                _eatTimer.Tick(Time.deltaTime);
            }
        }

        private void TryToEat(object sender, InputAction.CallbackContext e)
        {
            if (!e.started) return;

            if (!InventoryManager.Instance.HasSelectedItem) return;

            if (InventoryManager.Instance.SelectedInventoryItem.Item is not ConsumableItemSO consumableItem)
                return;

            if (InteractionManager.Instance.CurrentlyHoveredInteractable != null && InteractionManager.Instance.CurrentlyHoveredInteractable is CookingStation)
            {
                return;
            }
            
            if(consumableItem.ConsumableType != ConsumableType.Food)
                return;

            StartEating(consumableItem);
        }

        private void OnRespawn()
        {
            AddHunger(_maxHunger);
            _hungerStat.SetCurrentStat(50);
        }

        public void AddHunger(int amount)
        {
            _hungerStat.ChangeCurrent(amount);
        }

        private void StartEating(ConsumableItemSO consumable)
        {
            if (_isEating) return;

            _currentConsumable = consumable;
            _eatTimer = new Timer(consumable.ConsumeDuration);
            _eatTimer.OnTimerEnd += OnEatTimerFinished;

            _isEating = true;
        }

        private void CancelEating()
        {
            if (!_isEating) return;

            _eatTimer.OnTimerEnd -= OnEatTimerFinished;
            _eatTimer = null;
            _currentConsumable = null;
            _isEating = false;
        }

        private void OnEatTimerFinished(object sender, EventArgs e)
        {
            _eatTimer.OnTimerEnd -= OnEatTimerFinished;

            // Consume the food
            InventoryManager.Instance.RemoveItem(_currentConsumable, 1);
            AudioManager.Instance.PlayOneShot(
                FMODEvents.Instance.EatingSFX,
                Player.Instance.transform.position
            );
            AddHunger(_currentConsumable.HealAmount);

            // Check if player is still holding eat input
            bool stillHoldingEat = GameInput.Instance.IsHoldingDownSecondaryInteract;

            // Check if the same consumable is still selected and available
            bool stillHasFoodSelected =
                InventoryManager.Instance.HasSelectedItem &&
                InventoryManager.Instance.SelectedInventoryItem.Item == _currentConsumable; 

            if (stillHoldingEat && stillHasFoodSelected)
            {
                // Restart eating immediately with a fresh timer
                _eatTimer = new Timer(_currentConsumable.ConsumeDuration);
                _eatTimer.OnTimerEnd += OnEatTimerFinished;
                return;
            }

            // Otherwise, fully exit eating state
            _eatTimer = null;
            _currentConsumable = null;
            _isEating = false;
        }
    }
}
