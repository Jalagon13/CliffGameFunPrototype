using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace CliffGame
{
    public class PickupPanelUI : MonoBehaviour
    {
        private static event Action<PickupPanelUI> OnPickupPanelCreated;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _iconImage;
        [Header("Animation Settings")]
        [SerializeField] private float _disapearDelay = 3f;
        [SerializeField] private float _lerpDuration = 0.25f;
        [SerializeField] private float _fadeOutFraction = 0.25f;
        [Header("Custom Message Layout")]
        [SerializeField] private Vector2 _customNameAnchoredPosition = new Vector2(22f, 0f);

        private RectTransform _rectTransform;
        private RectTransform _nameRectTransform;
        private Tween _moveTween;
        private Tween _fadeTween;
        private float _currentTargetY;
        private CanvasGroup _canvasGroup;
        private Vector2 _defaultNameAnchoredPosition;

        private void InitializeVariables()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _nameRectTransform = _nameText.rectTransform;
            _currentTargetY = _rectTransform.anchoredPosition.y;
            _defaultNameAnchoredPosition = _nameRectTransform.anchoredPosition;
            OnPickupPanelCreated += HandlePickupPanelCreated;
        }
        
        private void OnDestroy()
        {
            _moveTween?.Kill();
            _fadeTween?.Kill();
            OnPickupPanelCreated -= HandlePickupPanelCreated;
        }

        public void Setup(InventoryItem item)
        {
            InitializeVariables();
            ApplyStandardLayout();

            _iconImage.gameObject.SetActive(true);
            _iconImage.sprite = item.Item.UiDisplay;
            _nameText.text = item.Item.InGameName;
            _amountText.text = $"+{item.Quantity}";

            NotifyPanelCreated();
            StartLifetime();
        }

        public void SetupCustom(Sprite icon, string nameText, string amountText = "")
        {
            InitializeVariables();
            ApplyCustomLayout();

            _iconImage.gameObject.SetActive(icon != null);
            _iconImage.sprite = icon;
            _nameText.text = nameText;
            _amountText.text = amountText;

            NotifyPanelCreated();
            StartLifetime();
        }

        private void StartLifetime()
        {
            float fadeDuration = _disapearDelay * _fadeOutFraction;
            float fadeStartTime = _disapearDelay - fadeDuration;

            _canvasGroup.alpha = 1f;

            _fadeTween = _canvasGroup
                .DOFade(0f, fadeDuration)
                .SetDelay(fadeStartTime)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);

            Destroy(gameObject, _disapearDelay);
        }

        private void HandlePickupPanelCreated(PickupPanelUI createdPanel)
        {
            if (createdPanel == this)
            {
                return;
            }

            float height = _rectTransform.rect.height;
            float newTargetY = _currentTargetY + height;

            float remainingDuration = _lerpDuration;

            if (_moveTween != null && _moveTween.IsActive() && _moveTween.IsPlaying())
            {
                remainingDuration = Mathf.Max(0f, _lerpDuration - _moveTween.Elapsed());
                _moveTween.Kill();
            }

            _currentTargetY = newTargetY;

            _moveTween = _rectTransform
                .DOAnchorPosY(_currentTargetY, remainingDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);       
        }

        private void NotifyPanelCreated()
        {
            OnPickupPanelCreated?.Invoke(this);
        }

        private void ApplyStandardLayout()
        {
            _amountText.gameObject.SetActive(true);
            _nameRectTransform.anchoredPosition = _defaultNameAnchoredPosition;
        }

        private void ApplyCustomLayout()
        {
            _amountText.gameObject.SetActive(false);
            _nameRectTransform.anchoredPosition = _customNameAnchoredPosition;
        }
    }
}
