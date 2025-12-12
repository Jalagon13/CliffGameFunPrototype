using System;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField]
        private List<SlotUI> _slotUiList;

        private void Start()
        {
            InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
            InventoryManager.Instance.OnSelectedSlotChanged += InventoryManager_OnSelectedSlotChanged;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
            InventoryManager.Instance.OnSelectedSlotChanged -= InventoryManager_OnSelectedSlotChanged;
        }

        private void InventoryManager_OnSelectedSlotChanged(int selectedSlotIndex, InventoryItem selectedItem)
        {
            for (int i = 0; i < _slotUiList.Count; i++)
            {
                if (i == selectedSlotIndex)
                {
                    // Highlight the selected slot
                    _slotUiList[i].SetHighlighted(true);
                }
                else
                {
                    _slotUiList[i].SetHighlighted(false);
                }
            }
        }

        private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            for (int i = 0; i < _slotUiList.Count; i++)
            {
                SlotUI slot = _slotUiList[i];
                InventoryItem inventoryItem = e.InventoryItems[i];

                slot.UpdateDisplayUI(inventoryItem);
            }
        }
    }
}
