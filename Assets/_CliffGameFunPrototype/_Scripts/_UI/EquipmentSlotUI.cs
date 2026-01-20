using UnityEngine;
using UnityEngine.EventSystems;

namespace CliffGame
{
    public class EquipmentSlotUI : SlotUI
    {
        private EquipableItemSO _currentlyEquippedItem;
        public bool EquipSlotEquipped => _currentlyEquippedItem != null;
    
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
                    InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = UnequipArmor();

                    Tooltip.HideUI();
                }
            }
            else if (mouseItem.HasItem && mouseItem.Item is ArmorItemSO mouseArmorItem && mouseArmorItem.ArmorType == _armorType)
            {
                // If no armor is equipped and the mouse is holding armor, equip it
                EquipArmor(mouseArmorItem);
                InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
                InventoryManager.Instance.ShowInventoryItemTooltip(new InventoryItem(_armorEquipped, 1));
            }

            UpdateSlotUI();

            // Notify the inventory system to update
            InventoryManager.Instance.GetInventoryModel().UpdateInventory();
        }
        
        private void Equip(EquipableItemSO equipableItemSO)
        {
            _currentlyEquippedItem = equipableItemSO;

            // UpdateSlotUI();
        }
        
        private void UnEquip()
        {
            
        }
        
        private EquipableItemSO Swap(EquipableItemSO equipableItemSO)
        {
            // Swap the currently equipped armor with the new armor
            EquipableItemSO oldEquipable = _currentlyEquippedItem;
            Equip(equipableItemSO);

            return oldEquipable;
        }
    }
}
