using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace CliffGame
{
    public class SpearTetherManager : MonoBehaviour
    {
        public static SpearTetherManager Instance;

        public event Action<float> OnSpearTetherRelease;

        [FormerlySerializedAs("_hookshotHolder")]
        [SerializeField] private SpearTetherHolder _spearTetherHolder;
        public SpearTetherHolder SpearTetherHolder => _spearTetherHolder;

        private ToolItemSO _currentSpearTetherTool;
        public ToolItemSO CurrentSpearTetherTool => _currentSpearTetherTool;

        private Timer _chargeTimer;
        public Timer SpearTetherChargeTimer => _chargeTimer;

        private bool _isCharging;
        public bool IsCharging => _isCharging;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForSpearTether;
            GameInput.Instance.OnSecondaryInteract += TryToCharge;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForSpearTether;
            GameInput.Instance.OnSecondaryInteract -= TryToCharge;
        }

        private void Update()
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            if (_isCharging && _chargeTimer != null)
            {
                if (!GameInput.Instance.IsHoldingDownSecondaryInteract)
                {
                    float chargePercent = _chargeTimer.PercentRemaining;
                    CancelCharging();
                    ReleaseSpearTether(chargePercent);
                    return;
                }

                _chargeTimer.Tick(Time.deltaTime);
            }
        }

        private void TryToCharge(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || _currentSpearTetherTool == null) return;

            if (_spearTetherHolder.SequenceExecuting)
            {
                _spearTetherHolder.RequestEarlyRetract();
                return;
            }

            if (CraftingManager.Instance.IsCraftingUIOpen ||
                Player.Instance.PauseMenuUI.IsPauseMenuOpen ||
                Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead)
            {
                return;
            }

            StartCharging();
        }

        private void StartCharging()
        {
            if (_isCharging) return;

            _chargeTimer = new Timer(_currentSpearTetherTool.TetherChargeDuration);
            _chargeTimer.OnTimerEnd += OnChargeTimerFinished;
            _isCharging = true;
        }

        private void CancelCharging()
        {
            if (!_isCharging) return;

            if (_chargeTimer != null)
            {
                _chargeTimer.OnTimerEnd -= OnChargeTimerFinished;
                _chargeTimer = null;
            }

            _isCharging = false;
        }

        private void OnChargeTimerFinished(object sender, EventArgs e)
        {
            _chargeTimer.OnTimerEnd -= OnChargeTimerFinished;
            _chargeTimer = null;
            _isCharging = false;

            ReleaseSpearTether(1f);
        }

        private void ReleaseSpearTether(float chargePercent)
        {
            OnSpearTetherRelease?.Invoke(chargePercent);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolSwingSFX, Player.Instance.transform.position);
        }

        private void CheckForSpearTether(int selectedIndex, InventoryItem item)
        {
            CancelCharging();

            if (item != null && item.HasItem && item.Item is ToolItemSO toolItem &&
                toolItem.ToolType == ToolType.Spear && toolItem.CanThrowTethered)
            {
                _currentSpearTetherTool = toolItem;
                return;
            }

            _currentSpearTetherTool = null;
        }
    }
}
