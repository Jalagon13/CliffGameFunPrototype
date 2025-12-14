using System;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Transform _hotbarSlotsUITransform;
        [SerializeField] private Transform _inventorySlotsUITransform;

        [SerializeField]
        private List<SlotUI> _slotUiList;

        private List<SlotUI> _inventorySlotUIList = new();

        private void Start()
        {
            InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
            InventoryManager.Instance.OnSelectedSlotChanged += InventoryManager_OnSelectedSlotChanged;

            InitializeSlots();
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
            InventoryManager.Instance.OnSelectedSlotChanged -= InventoryManager_OnSelectedSlotChanged;
        }

        public void InitializeSlots()
        {
            int indexCounter = 0;

            // For the first 9 inventory items, generate the slots as hotbar slots
            foreach (Transform hotbarSlot in _hotbarSlotsUITransform)
            {
                SlotUI hotbarIntSlotUI = hotbarSlot.gameObject.GetComponent<SlotUI>();
                hotbarIntSlotUI.InitializeInvSlotUI(indexCounter, InventoryManager.Instance.InventoryModel);
                indexCounter++;

                _inventorySlotUIList.Add(hotbarIntSlotUI);
            }

            foreach (Transform invSlot in _inventorySlotsUITransform)
            {
                SlotUI invSlotUI = invSlot.gameObject.GetComponent<SlotUI>();
                invSlotUI.InitializeInvSlotUI(indexCounter, InventoryManager.Instance.InventoryModel);
                indexCounter++;

                _inventorySlotUIList.Add(invSlotUI);
            }
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
