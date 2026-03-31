using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum HungerState
    {
        Fine,
        Hungry,
        Starving
    }

    public class HungerManager : MonoBehaviour
    {
        public event Action OnHungerPangExecuted;

        public static HungerManager Instance { get; private set; }

        [SerializeField] private int _maxHunger = 100;
        [SerializeField] private float _hungerDrainPerSecond = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _lowHungerThresholdPercent = 0.3f;
        [SerializeField] private float _stomachGrowlIntervalSeconds = 10f;
        [SerializeField] private int _respawnHungerAmount = 65;

        private PlayerStat _hungerStat;
        public int CurrentHunger => _hungerStat.Current;

        public HungerState CurrentHungerState { get; private set; }
        private HungerState _previousHungerState;

        public event Action<int, int> OnHungerChanged;
        private Timer _eatTimer;
        public Timer EatTimer => _eatTimer;

        private ConsumableItemSO _currentConsumable;
        private bool _isEating;
        public bool IsEating => _isEating;

        private void Awake()
        {
            Instance = this;
            _previousHungerState = CurrentHungerState;

            _hungerStat = new PlayerStat(_maxHunger, _hungerDrainPerSecond, 0f);
            _hungerStat.OnValueChanged += HandleHungerValueChanged;
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
            CancelInvoke(nameof(ExecuteHungerPang));
            if (_hungerStat != null)
            {
                _hungerStat.OnValueChanged -= HandleHungerValueChanged;
            }

            Player.Instance.OnPlayerRespawn -= OnRespawn;
            GameInput.Instance.OnSecondaryInteract -= TryToEat;
        }

        private void Update()
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead || Player.Instance.FirstPersonLook.IsSequenceOngoing)
            {
                return;
            }

            _hungerStat.UpdateStat(Time.deltaTime, true);

            if (_isEating && _eatTimer != null)
            {
                if (!GameInput.Instance.IsHoldingDownSecondaryInteract)
                {
                    CancelEating();
                    return;
                }

                _eatTimer.Tick(Time.deltaTime);
            }
        }

        private void ExecuteHungerPang()
        {
            if (CurrentHungerState == HungerState.Hungry || CurrentHungerState == HungerState.Starving)
            {
                OnHungerPangExecuted?.Invoke();
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.StomachGrowlSFX, Player.Instance.transform.position);
            }
        }

        private void HandleHungerValueChanged(int current, int max)
        {
            OnHungerChanged?.Invoke(current, max);

            float percent = max > 0 ? (float)current / max : 0f;
            HungerState newState = current <= 0
                ? HungerState.Starving
                : percent <= _lowHungerThresholdPercent
                    ? HungerState.Hungry
                    : HungerState.Fine;

            if (newState != CurrentHungerState)
            {
                SyncHungerWarningLoop(newState);
                _previousHungerState = CurrentHungerState;
                CurrentHungerState = newState;
            }
        }

        private void SyncHungerWarningLoop(HungerState state)
        {
            CancelInvoke(nameof(ExecuteHungerPang));

            if (state == HungerState.Hungry || state == HungerState.Starving)
            {
                InvokeRepeating(nameof(ExecuteHungerPang), 0.1f, _stomachGrowlIntervalSeconds);
            }
        }

        private void TryToEat(object sender, InputAction.CallbackContext e)
        {
            if (!e.started || !InventoryManager.Instance.HasSelectedItem)
            {
                return;
            }

            if (Grindstone.HoveredSharpeningStoneWantsSecondaryInteract())
            {
                return;
            }

            if (InventoryManager.Instance.SelectedInventoryItem.Item is not ConsumableItemSO consumableItem)
            {
                return;
            }

            if (InteractionManager.Instance.CurrentlyHoveredInteractable is CookingStation || consumableItem.HungerAmount <= 0 || CurrentHunger >= _maxHunger)
            {
                return;
            }

            StartEating(consumableItem);
        }

        private void OnRespawn()
        {
            AddHunger(_maxHunger);
            _hungerStat.SetCurrentStat(_respawnHungerAmount);
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

            InventoryManager.Instance.RemoveItem(_currentConsumable, 1);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.EatingSFX, Player.Instance.transform.position);
            AddHunger(_currentConsumable.HungerAmount);

            bool stillHoldingEat = GameInput.Instance.IsHoldingDownSecondaryInteract;
            bool stillHasFoodSelected = InventoryManager.Instance.HasSelectedItem && InventoryManager.Instance.SelectedInventoryItem.Item == _currentConsumable;

            if (stillHoldingEat && stillHasFoodSelected)
            {
                _eatTimer = new Timer(_currentConsumable.ConsumeDuration);
                _eatTimer.OnTimerEnd += OnEatTimerFinished;
                return;
            }

            _eatTimer = null;
            _currentConsumable = null;
            _isEating = false;
        }
    }
}
