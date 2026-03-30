using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AdvancedTooltips.Core;

namespace CliffGame
{
    public class SlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image _itemImage;

        [SerializeField]
        private TextMeshProUGUI _itemQuantityText;

        [SerializeField]
        private GameObject _highlightedVisuals;

        [Header("Durability UI")]
        [SerializeField] private bool _autoCreateDurabilityBar = true;
        [SerializeField] private Vector2 _durabilityBarSize = new Vector2(58f, 6f);
        [SerializeField] private Vector2 _durabilityBarAnchoredPosition = new Vector2(0f, 6f);
        [SerializeField] private Color _durabilityCurrentColor = new Color(0.3f, 0.95f, 0.3f, 1f);
        [SerializeField] private Color _durabilityMissingColor = new Color(0.85f, 0.2f, 0.2f, 1f);

        private InventoryItem _item;
        private int _inventoryIndex;
        private InventoryModel _inventoryAssociatedWith;
        private bool _hovered;
        private Image _durabilityBarBackgroundImage;
        private RectTransform _durabilityBarFillRect;
        private Image _durabilityBarFillImage;
        private float _durabilityBarFullWidth;

        private void Awake()
        {
            SetHighlighted(false);
            EnsureDurabilityBarExists();
        }

        private void OnDisable()
        {
            if (_hovered)
            {
                Tooltip.HideUI();
            }
        }
        
        private void OnDestroy()
        {
            Tooltip.HideUI();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryManager.Instance.SlotInteractionHandler.InventorySlotLeftClicked(_inventoryIndex, _inventoryAssociatedWith);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryManager.Instance.SlotInteractionHandler.InventorySlotRightClicked(_inventoryIndex, _inventoryAssociatedWith);
            }
        }

        public void InitializeInvSlotUI(int inventoryIndex, InventoryModel inventoryAssociatedWith)
        {
            _inventoryAssociatedWith = inventoryAssociatedWith;
            _inventoryIndex = inventoryIndex;
        }

        public void UpdateDisplayUI(InventoryItem item)
        {
            _item = item;
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

        public void SetHighlighted(bool isHighlighted)
        {
            if(_highlightedVisuals != null)
                _highlightedVisuals.SetActive(isHighlighted);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (_item != null && _item.HasItem && !InventoryManager.Instance.MouseHasItem)
            {
                _hovered = true;

                Tooltip.ShowNew();
                
                InventoryItem item = _inventoryAssociatedWith.InventoryItems[_inventoryIndex];
                string itemText = item.GetTooltipText();

                Tooltip.JustText(itemText, Color.white, fontSize: 12f);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideUI();
        }

        private void EnsureDurabilityBarExists()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (!_autoCreateDurabilityBar || _durabilityBarBackgroundImage != null || rectTransform == null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject("DurabilityBarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(transform, false);
            _durabilityBarBackgroundImage = backgroundObject.GetComponent<Image>();
            _durabilityBarBackgroundImage.color = _durabilityMissingColor;
            _durabilityBarBackgroundImage.raycastTarget = false;

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = _durabilityBarAnchoredPosition;
            backgroundRect.sizeDelta = _durabilityBarSize;

            GameObject fillObject = new GameObject("DurabilityBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(backgroundObject.transform, false);
            _durabilityBarFillImage = fillObject.GetComponent<Image>();
            _durabilityBarFillImage.color = _durabilityCurrentColor;
            _durabilityBarFillImage.raycastTarget = false;

            _durabilityBarFillRect = fillObject.GetComponent<RectTransform>();
            _durabilityBarFillRect.anchorMin = new Vector2(0f, 0f);
            _durabilityBarFillRect.anchorMax = new Vector2(0f, 1f);
            _durabilityBarFillRect.pivot = new Vector2(0f, 0.5f);
            _durabilityBarFillRect.anchoredPosition = Vector2.zero;
            _durabilityBarFillRect.sizeDelta = _durabilityBarSize;

            _durabilityBarFullWidth = _durabilityBarSize.x;

            if (rectTransform.rect.width > 0f)
            {
                _durabilityBarFullWidth = Mathf.Min(_durabilityBarFullWidth, rectTransform.rect.width - 16f);
                backgroundRect.sizeDelta = new Vector2(_durabilityBarFullWidth, _durabilityBarSize.y);
                _durabilityBarFillRect.sizeDelta = new Vector2(_durabilityBarFullWidth, _durabilityBarSize.y);
            }

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

            float normalizedDurability = item.DurabilityNormalized;
            _durabilityBarFillRect.sizeDelta = new Vector2(_durabilityBarFullWidth * normalizedDurability, _durabilityBarSize.y);
        }
    }
}
