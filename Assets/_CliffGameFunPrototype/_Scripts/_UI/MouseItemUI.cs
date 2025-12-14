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

        private void Awake()
        {
            _itemImage.color = new Vector4(1, 1, 1, 0);
            _itemImage.sprite = null;
            _itemQuantityText.text = string.Empty;
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
        }
    }
}
