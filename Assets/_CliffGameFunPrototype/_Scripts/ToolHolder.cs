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
        // [SerializeField] private float _swingCooldownSeconds = 0.375f;

        private ToolItemSO _currentHeldTool;
        public ToolItemSO CurrentHeldTool => _currentHeldTool;
        
        private bool _isSwinging;
        public bool IsSwinging => _isSwinging;
        
        private Timer _swingCooldownTimer;
        public event Action OnToolSwingDown;
    
        private void Start()
        {
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
            
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead ||
                CraftingManager.Instance.IsCraftingUIOpen || BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen ||
                Player.Instance.ToolHolder.IsSwinging) return;
            
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
            if (_currentHeldTool == toolItem && ToolPrefabExists()) return;

            UnequipTool();

            _currentHeldTool = toolItem;
            _swingCooldownTimer = new Timer(_currentHeldTool.SwingCooldownInSeconds);
            _swingCooldownTimer.RemainingSeconds = 0;

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

        private void TryPlaySwingAnimation()
        {
            // Only swing if a tool is equipped, not already swinging, and cooldown is over
            if (_isSwinging || !ToolPrefabExists() || _swingCooldownTimer.RemainingSeconds > 0f)
                return;

            

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
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolSwingSFX, transform.position);
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
                _swingCooldownTimer.Reset();
            });
        }

    }
}
