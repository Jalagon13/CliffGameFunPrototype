using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CliffGame
{
    public class MouseItemUI : MonoBehaviour
    {
        [SerializeField] private Image _itemImage;
        [SerializeField] private TextMeshProUGUI _itemQuantityText;
        [Header("Durability UI")]
        [SerializeField] private bool _autoCreateDurabilityBar = true;
        [SerializeField] private Vector2 _durabilityBarSize = new Vector2(58f, 6f);
        [SerializeField] private Vector2 _durabilityBarAnchoredPosition = new Vector2(0f, -34f);
        [SerializeField] private Color _durabilityCurrentColor = new Color(0.3f, 0.95f, 0.3f, 1f);
        [SerializeField] private Color _durabilityMissingColor = new Color(0.85f, 0.2f, 0.2f, 1f);

        private Image _durabilityBarBackgroundImage;
        private RectTransform _durabilityBarFillRect;
        private float _durabilityBarFullWidth;

        private void Awake()
        {
            _itemImage.color = new Vector4(1, 1, 1, 0);
            _itemImage.sprite = null;
            _itemQuantityText.text = string.Empty;
            EnsureDurabilityBarExists();
        }

        private void Start()
        {
            InventoryManager.Instance.OnMouseItemUpdated += InventoryManager_OnMouseItemUpdated;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
        }

        private void InventoryManager_OnMouseItemUpdated(InventoryItem item)
        {
            UpdateView(item);
        }

        private void Update()
        {
            if (Camera.main == null) return;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            RectTransform rectTransform = (RectTransform)transform;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            rectTransform.position = mouseScreenPosition;
        }

        public void UpdateView(InventoryItem item)
        {
            if (item.Item != null)
            {
                _itemImage.color = new Vector4(1, 1, 1, 1);
                _itemImage.sprite = item.Item.UiDisplay;

                _itemQuantityText.text = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;
            }
            else
            {
                _itemImage.color = new Vector4(1, 1, 1, 0);
                _itemImage.sprite = null;

                _itemQuantityText.text = string.Empty;
            }

            UpdateDurabilityUI(item);
        }

        private void EnsureDurabilityBarExists()
        {
            if (!_autoCreateDurabilityBar || _durabilityBarBackgroundImage != null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject("MouseDurabilityBarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(transform, false);
            _durabilityBarBackgroundImage = backgroundObject.GetComponent<Image>();
            _durabilityBarBackgroundImage.color = _durabilityMissingColor;
            _durabilityBarBackgroundImage.raycastTarget = false;

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = _durabilityBarAnchoredPosition;
            backgroundRect.sizeDelta = _durabilityBarSize;

            GameObject fillObject = new GameObject("MouseDurabilityBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(backgroundObject.transform, false);
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = _durabilityCurrentColor;
            fillImage.raycastTarget = false;

            _durabilityBarFillRect = fillObject.GetComponent<RectTransform>();
            _durabilityBarFillRect.anchorMin = new Vector2(0f, 0f);
            _durabilityBarFillRect.anchorMax = new Vector2(0f, 0f);
            _durabilityBarFillRect.pivot = new Vector2(0f, 0f);
            _durabilityBarFillRect.anchoredPosition = Vector2.zero;
            _durabilityBarFillRect.sizeDelta = _durabilityBarSize;

            _durabilityBarFullWidth = _durabilityBarSize.x;
            backgroundObject.SetActive(false);
        }

        private void UpdateDurabilityUI(InventoryItem item)
        {
            if (_durabilityBarBackgroundImage == null || _durabilityBarFillRect == null)
            {
                return;
            }

            bool showDurabilityBar = item != null &&
                                     item.HasItem &&
                                     item.UsesDurability &&
                                     item.CurrentDurability < item.MaxDurability;

            _durabilityBarBackgroundImage.gameObject.SetActive(showDurabilityBar);
            if (!showDurabilityBar)
            {
                return;
            }

            _durabilityBarFillRect.sizeDelta = new Vector2(_durabilityBarFullWidth * item.DurabilityNormalized, _durabilityBarSize.y);
        }
    }
}
