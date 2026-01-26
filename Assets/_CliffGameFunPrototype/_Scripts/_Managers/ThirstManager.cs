using System;
using UnityEngine;
using CliffGame;
using System.Collections;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum ThirstState
    {
        Full,   // When the player has a thirst higher than 0
        Thirsty  // When thirst is 0
    }

    public class ThirstManager : MonoBehaviour
    {
        public static ThirstManager Instance { get; private set; }

        [SerializeField]
        private int _maxThirst = 100;
        [SerializeField]
        private float _thirstDrainPerSecond = 0.1f;

        private PlayerStat _thirstStat;
        public int CurrentThirst => _thirstStat.Current;

        public ThirstState CurrentThirstState { get; private set; }

        public event Action<int, int> OnThirstChanged; // current, max
        private Timer _drinkTimer;
        public Timer DrinkTimer => _drinkTimer;

        private ConsumableItemSO _currentConsumable;
        private bool _isDrinking;
        public bool IsDrinking => _isDrinking;

        private void Awake()
        {
            Instance = this;

            _thirstStat = new PlayerStat(_maxThirst, _thirstDrainPerSecond, 0f);
            _thirstStat.OnValueChanged += (current, max) =>
            {
                OnThirstChanged?.Invoke(current, max);
                if (current <= 0)
                    CurrentThirstState = ThirstState.Thirsty;
                else
                    CurrentThirstState = ThirstState.Full;
            };
        }

        private IEnumerator Start()
        {
            Player.Instance.OnPlayerRespawn += OnRespawn;
            GameInput.Instance.OnSecondaryInteract += TryToDrink;

            yield return null;
            OnThirstChanged?.Invoke(CurrentThirst, _thirstStat.Max);
        }

        private void OnDestroy()
        {
            Player.Instance.OnPlayerRespawn -= OnRespawn;
            GameInput.Instance.OnSecondaryInteract -= TryToDrink;
        }

        private void Update()
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            _thirstStat.UpdateStat(Time.deltaTime, true);

            if (_isDrinking && _drinkTimer != null)
            {
                // Cancel drinking if button released
                if (!GameInput.Instance.IsHoldingDownSecondaryInteract)
                {
                    CancelDrinking();
                    return;
                }

                _drinkTimer.Tick(Time.deltaTime);
            }
        }

        private void TryToDrink(object sender, InputAction.CallbackContext e)
        {
            if (!e.started) return;

            if (!InventoryManager.Instance.HasSelectedItem) return;

            if (InventoryManager.Instance.SelectedInventoryItem.Item is not ConsumableItemSO consumableItem)
                return;

            if (InteractionManager.Instance.CurrentlyHoveredInteractable != null && InteractionManager.Instance.CurrentlyHoveredInteractable is CookingStation)
            {
                return;
            }

            if (consumableItem.ConsumableType != ConsumableType.Drink)
                return;

            StartDrinking(consumableItem);
        }

        private void OnRespawn()
        {
            AddThirst(_maxThirst);
            _thirstStat.SetCurrentStat(50);
        }

        public void AddThirst(int amount)
        {
            _thirstStat.ChangeCurrent(amount);
        }

        private void StartDrinking(ConsumableItemSO consumable)
        {
            if (_isDrinking) return;

            _currentConsumable = consumable;
            _drinkTimer = new Timer(consumable.ConsumeDuration);
            _drinkTimer.OnTimerEnd += OnDrinkTimerFinished;
            _isDrinking = true;
        }

        private void CancelDrinking()
        {
            if (!_isDrinking) return;

            _drinkTimer.OnTimerEnd -= OnDrinkTimerFinished;
            _drinkTimer = null;
            _currentConsumable = null;
            _isDrinking = false;
        }

        private void OnDrinkTimerFinished(object sender, EventArgs e)
        {
            _drinkTimer.OnTimerEnd -= OnDrinkTimerFinished;

            // Consume the drink
            InventoryManager.Instance.RemoveItem(_currentConsumable, 1);
            AudioManager.Instance.PlayOneShot(
                FMODEvents.Instance.EatingSFX,
                Player.Instance.transform.position
            );
            AddThirst(_currentConsumable.HealAmount);

            // Check if player is still holding drink input
            bool stillHoldingDrink = GameInput.Instance.IsHoldingDownSecondaryInteract;
            // Check if the same consumable is still selected and available
            bool stillHasFoodSelected =
                InventoryManager.Instance.HasSelectedItem &&
                InventoryManager.Instance.SelectedInventoryItem.Item == _currentConsumable;

            if (stillHoldingDrink && stillHasFoodSelected)
            {
                // Restart drinking immediately with a fresh timer
                _drinkTimer = new Timer(_currentConsumable.ConsumeDuration);
                _drinkTimer.OnTimerEnd += OnDrinkTimerFinished;
                return;
            }

            // Otherwise, fully exit drinking state
            _drinkTimer = null;
            _currentConsumable = null;
            _isDrinking = false;
        }
    }
}
