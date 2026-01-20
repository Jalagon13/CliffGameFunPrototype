using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CliffGame
{
    public class EquipmentSlotUI : SlotUI
    {
        // TEMP PROTOTYPE CODE
        public static bool PREVENT_WIND_WITH_BOOTS = false;
    
        [Header("Equipment Slot UI")]
        [SerializeField] private Image _equipmentEmblemIcon;
        [SerializeField] private Image _equippedItemIcon;
    
        private EquipableItemSO _currentlyEquippedItem;
        public bool EquipSlotEquipped => _currentlyEquippedItem != null;
        
        private void OnDestroy()
        {
            PREVENT_WIND_WITH_BOOTS = false;
        }
        
        private void OnDisable()
        {
            Tooltip.HideUI();
        }
    
        public override void OnPointerClick(PointerEventData eventData)
        {
            InventoryItem mouseItem = InventoryManager.Instance.MouseItemModel.MouseInventoryItem;

            if (EquipSlotEquipped)
            {
                // If there's already armor equipped in this slot
                if (mouseItem.HasItem)
                {
                    if (mouseItem.Item is EquipableItemSO equipableItem)
                    {
                        // Swap the equipped armor with the armor held by the mouse
                        InventoryManager.Instance.MouseItemModel.MouseInventoryItem.Item = Swap(equipableItem);
                        InventoryManager.Instance.MouseItemModel.MouseInventoryItem.Quantity = 1;
                    }
                }
                else
                {
                    // Otherwise, place the unequipped armor on the mouse
                    InventoryManager.Instance.MouseItemModel.MouseInventoryItem.Item = UnEquip();

                    Tooltip.HideUI();
                }
            }
            else if (mouseItem.HasItem && mouseItem.Item is EquipableItemSO mouseEquipableItem)
            {
                // If no armor is equipped and the mouse is holding armor, equip it
                Equip(mouseEquipableItem);
                InventoryManager.Instance.MouseItemModel.MouseInventoryItem = new();
                InventoryManager.Instance.ShowInventoryItemTooltip(new InventoryItem(_currentlyEquippedItem, 1));
            }

            UpdateSlotUI();

            // Notify the inventory system to update
            InventoryManager.Instance.InventoryModel.UpdateInventory();
        }
        
        private void Equip(EquipableItemSO equipableItemSO)
        {
            _currentlyEquippedItem = equipableItemSO;

            InventoryManager.Instance.SlotInteractionHandler.PlayClickFeedbacks();
            
            UpdateSlotUI();

            // Temp code here for exclusive boots equip logic for wind prevention
            PREVENT_WIND_WITH_BOOTS = true;


        }
        
        private EquipableItemSO UnEquip()
        {
            // Unequip the armor and return it
            EquipableItemSO unequippedItem = _currentlyEquippedItem;
            _currentlyEquippedItem = null;

            InventoryManager.Instance.SlotInteractionHandler.PlayClickFeedbacks();
            UpdateSlotUI();

            // Temp code here for exclusive boots equip logic for wind prevention
            PREVENT_WIND_WITH_BOOTS = false;

            return unequippedItem;
        }
        
        private EquipableItemSO Swap(EquipableItemSO equipableItemSO)
        {
            // Swap the currently equipped armor with the new armor
            EquipableItemSO oldEquipable = _currentlyEquippedItem;
            Equip(equipableItemSO);

            InventoryManager.Instance.SlotInteractionHandler.PlayClickFeedbacks();

            // Temp code here for exclusive boots equip logic for wind prevention
            PREVENT_WIND_WITH_BOOTS = true;

            return oldEquipable;
        }
        
        public void UpdateSlotUI()
        {
            // // Enable or disable the icon based on whether armor is equipped
            _equippedItemIcon.enabled = EquipSlotEquipped;
            _equipmentEmblemIcon.enabled = !EquipSlotEquipped;

            if (EquipSlotEquipped)
            {
                // Update the icon to display the equipped armor's sprite
                _equippedItemIcon.sprite = _currentlyEquippedItem.UiDisplay;
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            InventoryManager.Instance.ShowInventoryItemTooltip(new InventoryItem(_currentlyEquippedItem, 1));
        }
    }
}
