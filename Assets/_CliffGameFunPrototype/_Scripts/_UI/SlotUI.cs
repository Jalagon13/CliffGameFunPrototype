using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CliffGame
{
    public class SlotUI : MonoBehaviour, IPointerClickHandler
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

        private void Awake()
        {
            SetHighlighted(false);
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
    }
}
