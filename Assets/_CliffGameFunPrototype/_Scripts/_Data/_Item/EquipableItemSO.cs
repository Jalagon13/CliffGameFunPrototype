using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Equipable Item Data", menuName = "Item/EquipableData")]
    public class EquipableItemSO : ItemSO
    {
        public override InventoryItem CreateInventoryItem(int quantity)
        {
            return new(this, quantity);
        }

        public override string GetDescription()
        {
            return GetDescriptionBreak();
        }
    }
}
