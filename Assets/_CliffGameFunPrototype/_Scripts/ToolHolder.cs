using System;
using UnityEngine;
using DG.Tweening;

namespace CliffGame
{
    public class ToolHolder : MonoBehaviour
    {
        [SerializeField] private float _swingDownTime = 0.2f;
        [SerializeField] private float _swingUpTime = 0.1f;
        [SerializeField] private float _returnTime = 0.15f;
        [SerializeField] private float _swingDownAngle = -50f;
        [SerializeField] private float _swingUpAngle = 40f;

        [Header("Spear Stab Animation")]
        [SerializeField] private float _spearStabPullbackDistanceZ = 0.15f;
        [SerializeField] private float _spearStabThrustDistanceZ = 0.35f;
        [SerializeField] private float _spearStabPullbackTime = 0.08f;
        [SerializeField] private float _spearStabThrustTime = 0.09f;
        [SerializeField] private float _spearStabRecoverTime = 0.12f;

        [SerializeField] private float _chargePullbackDistanceZ = 0.25f;
        [SerializeField] private float _chargePullbackLerpSpeed = 12f;
        // [SerializeField] private float _swingCooldownSeconds = 0.375f;

        private ToolItemSO _currentHeldTool;
        public ToolItemSO CurrentHeldTool => _currentHeldTool;
        
        private bool _isSwinging;
        public bool IsSwinging => _isSwinging;
        private bool _modelsHiddenForTetherSequence;
        private Vector3 _defaultLocalPosition;
        private Quaternion _defaultLocalRotation;
        
        private Timer _swingCooldownTimer;
        public event Action OnToolSwingDown;
    
        private void Start()
        {
            _defaultLocalPosition = transform.localPosition;
            _defaultLocalRotation = transform.localRotation;
            InventoryManager.Instance.OnSelectedSlotChanged += OnSelectedSlotChanged;
            _swingCooldownTimer = new Timer(0);
            _swingCooldownTimer.RemainingSeconds = 0f;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= OnSelectedSlotChanged;
        }
        
        private void Update()
        {
            _swingCooldownTimer?.Tick(Time.deltaTime);

            UpdateChargePullback();

            bool tetherSequenceExecuting = IsSpearTetherSequenceExecuting();
            SyncHeldToolModelVisibility(tetherSequenceExecuting);
            
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead ||
                CraftingManager.Instance.IsCraftingUIOpen || BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen ||
                Player.Instance.ToolHolder.IsSwinging ||
                tetherSequenceExecuting) return;
            
            if(GameInput.Instance.IsHoldingDownPrimaryInteract)
            {
                TryPlaySwingAnimation();
            }
        }

        private void OnSelectedSlotChanged(int arg1, InventoryItem item)
        {
            if (item.Item is ToolItemSO toolItem)
            {
                EquipTool(toolItem);
            }
            else
            {
                UnequipTool();
            }
        }

        private void EquipTool(ToolItemSO toolItem)
        {
            // If the same tool is already equipped, do nothing
            if (_currentHeldTool == toolItem && ToolPrefabExists())
            {
                RefreshSwingCooldownTimer(false);
                return;
            }

            UnequipTool();

            _currentHeldTool = toolItem;
            RefreshSwingCooldownTimer(false);

            // Instantiate as a child so the prefab's local offset is preserved
            Instantiate(toolItem.HeldToolPrefab, transform);
        }

        private void UnequipTool()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            _currentHeldTool = null;
        }
        
        private bool ToolPrefabExists()
        {
            return transform.childCount > 0;
        }

        private bool IsSpearTetherSequenceExecuting()
        {
            return SpearTetherManager.Instance != null &&
                   SpearTetherManager.Instance.SpearTetherHolder != null &&
                   SpearTetherManager.Instance.SpearTetherHolder.SequenceExecuting;
        }

        private void SyncHeldToolModelVisibility(bool hideModels)
        {
            if (_modelsHiddenForTetherSequence == hideModels) return;

            _modelsHiddenForTetherSequence = hideModels;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(!hideModels);
                }
            }
        }

        private void UpdateChargePullback()
        {
            if (_isSwinging)
            {
                return;
            }

            bool isCharging = SpearTetherManager.Instance != null && SpearTetherManager.Instance.IsCharging;
            float targetZ = _defaultLocalPosition.z + (isCharging ? -_chargePullbackDistanceZ : 0f);

            Vector3 current = transform.localPosition;
            Vector3 target = new Vector3(_defaultLocalPosition.x, _defaultLocalPosition.y, targetZ);
            transform.localPosition = Vector3.Lerp(current, target, _chargePullbackLerpSpeed * Time.deltaTime);
        }

        private void TryPlaySwingAnimation()
        {
            // Only swing if a tool is equipped, not already swinging, and cooldown is over
            if (_isSwinging || !ToolPrefabExists() || _swingCooldownTimer.RemainingSeconds > 0f || Player.Instance.PauseMenuUI.IsPauseMenuOpen || CraftingManager.Instance.IsCraftingUIOpen)
                return;

            if (_currentHeldTool != null && _currentHeldTool.ToolType == ToolType.Spear)
            {
                TryPlaySpearStabAnimation();
                return;
            }

            _isSwinging = true;

            // Cache starting rotation on all axes
            Vector3 startEuler = transform.localEulerAngles;

            float startX = startEuler.x;
            if (startX > 180f)
                startX -= 360f;

            float startY = startEuler.y;
            float startZ = startEuler.z;

            Sequence swingSequence = DOTween.Sequence();

            // Swing down
            swingSequence.Append(
                transform.DOLocalRotate(
                    new Vector3(_swingDownAngle, startY, startZ),
                    _swingDownTime
                ).SetEase(Ease.OutQuad)
            );

            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolSwingSFX, transform.position);

            // Swing up
            swingSequence.Append(
                transform.DOLocalRotate(
                    new Vector3(_swingUpAngle, startY, startZ),
                    _swingUpTime
                ).SetEase(Ease.OutQuad)
            );

            swingSequence.AppendCallback(() =>
            {
                OnToolSwingDown?.Invoke();
            });
            
            // Return to original rotation
            swingSequence.Append(
                transform.DOLocalRotate(
                    new Vector3(startX, startY, startZ),
                    _returnTime
                ).SetEase(Ease.OutQuad)
            );

            swingSequence.OnComplete(() =>
            {
                _isSwinging = false;
                RefreshSwingCooldownTimer(true);
            });
        }

        private void TryPlaySpearStabAnimation()
        {
            _isSwinging = true;

            transform.localRotation = _defaultLocalRotation;

            float baseZ = _defaultLocalPosition.z;
            float pullbackZ = baseZ - _spearStabPullbackDistanceZ;
            float thrustZ = baseZ + _spearStabThrustDistanceZ;

            Sequence stabSequence = DOTween.Sequence();

            // Pull hand/spear back before the stab.
            stabSequence.Append(
                transform.DOLocalMoveZ(pullbackZ, _spearStabPullbackTime).SetEase(Ease.OutQuad)
            );

            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolSwingSFX, transform.position);

            // Thrust forward to deliver the hit.
            stabSequence.Append(
                transform.DOLocalMoveZ(thrustZ, _spearStabThrustTime).SetEase(Ease.OutQuad)
            );

            stabSequence.AppendCallback(() =>
            {
                OnToolSwingDown?.Invoke();
            });

            // Recover to resting hand position.
            stabSequence.Append(
                transform.DOLocalMove(_defaultLocalPosition, _spearStabRecoverTime).SetEase(Ease.OutQuad)
            );

            stabSequence.OnComplete(() =>
            {
                transform.localPosition = _defaultLocalPosition;
                _isSwinging = false;
                RefreshSwingCooldownTimer(true);
            });
        }

        private void RefreshSwingCooldownTimer(bool startOnCooldown)
        {
            float cooldownDuration = GetCurrentSwingCooldownDuration();
            _swingCooldownTimer = new Timer(cooldownDuration);

            if (!startOnCooldown)
            {
                _swingCooldownTimer.RemainingSeconds = 0f;
            }
        }

        private float GetCurrentSwingCooldownDuration()
        {
            if (_currentHeldTool == null)
            {
                return 0f;
            }

            float cooldownDuration = _currentHeldTool.SwingCooldownInSeconds;
            if (InventoryManager.Instance.TryGetSelectedToolInventoryItem(out InventoryItem selectedToolItem, out _))
            {
                cooldownDuration *= selectedToolItem.GetResourceSwingCooldownMultiplier();
            }

            return cooldownDuration;
        }

    }
}
