using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class HookshotManager : MonoBehaviour
    {
        public static HookshotManager Instance;
        
        public event Action<float> OnHookshotRelease;
        public event Action OnHookshotEquipped;
        public event Action OnHookshotUnequipped;
        
        [SerializeField] private HookshotHolder _hookshotHolder;
        public HookshotHolder HookshotHolder => _hookshotHolder;
       
        
        private HookshotItemSO _currentHookshot;
        public HookshotItemSO CurrentHookshot => _currentHookshot;
        
        private Timer _chargeTimer;
        public Timer HookshotChargeTimer => _chargeTimer;
        private bool _isCharging;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHookshot;
            GameInput.Instance.OnPrimaryInteract += TryToCharge;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForHookshot;
            GameInput.Instance.OnPrimaryInteract -= TryToCharge;
        }

        private void Update()
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            if (_isCharging && _chargeTimer != null)
            {
                if (!GameInput.Instance.IsHoldingDownPrimaryInteract)
                {
                    float chargePercent = _chargeTimer.PercentRemaining;
                    CancelCharging();
                    ReleaseHookshot(chargePercent);
                    return;
                }

                _chargeTimer.Tick(Time.deltaTime);
            }
        }

        private void TryToCharge(object sender, InputAction.CallbackContext e)
        {
            if (!e.started || _currentHookshot == null) return;

            if (CraftingManager.Instance.IsCraftingUIOpen || Player.Instance.PauseMenuUI.IsPauseMenuOpen || _hookshotHolder.HookSequenceExecuting) return;

            StartCharging();
        }

        private void StartCharging()
        {
            if (_isCharging) return;

            _chargeTimer = new Timer(_currentHookshot.ChargeDuration);
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
            
            ReleaseHookshot(1f);
        }

        private void ReleaseHookshot(float chargePercent)
        {
            OnHookshotRelease?.Invoke(chargePercent);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolSwingSFX, Player.Instance.transform.position);
        }

        private void CheckForHookshot(int arg1, InventoryItem item)
        {
            CancelCharging();

            if(item.Item is HookshotItemSO hookshot)
            {
                OnHookshotEquipped?.Invoke();
                _currentHookshot = hookshot;
            }
            else
            {
                OnHookshotUnequipped?.Invoke();
                _currentHookshot = null;
            }
        }
    }
}
