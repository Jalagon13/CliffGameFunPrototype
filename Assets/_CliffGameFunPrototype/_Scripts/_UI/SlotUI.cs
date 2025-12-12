using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class SlotUI : MonoBehaviour
    {
        [SerializeField]
        private Image _itemImage;

        [SerializeField]
        private TextMeshProUGUI _itemQuantityText;

        [SerializeField]
        private GameObject _highlightedVisuals;

        private InventoryItem _item;

        private void Awake()
        {
            SetHighlighted(false);
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
