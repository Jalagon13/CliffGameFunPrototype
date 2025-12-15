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

        private InventoryItem _item;
        private int _inventoryIndex;
        private InventoryModel _inventoryAssociatedWith;
        private bool _hovered;

        private void Awake()
        {
            SetHighlighted(false);
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

        public void OnPointerClick(PointerEventData eventData)
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
        }

        public void SetHighlighted(bool isHighlighted)
        {
            _highlightedVisuals.SetActive(isHighlighted);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_item != null && _item.HasItem && !InventoryManager.Instance.MouseHasItem)
            {
                _hovered = true;

                Tooltip.ShowNew();

                int quantity = _inventoryAssociatedWith.InventoryItems[_inventoryIndex].Quantity;
                string quantityString = quantity > 1 ? $"({quantity})" : string.Empty;
                string itemText = $"{_item.Item.InGameName} {quantityString}<br>{_item.Item.GetDescription()}";

                Tooltip.JustText(itemText, Color.white, fontSize: 12f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideUI();
        }
    }
}
