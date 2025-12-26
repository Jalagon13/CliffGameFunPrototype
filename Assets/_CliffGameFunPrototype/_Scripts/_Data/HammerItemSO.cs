using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Hammer Item Data", menuName = "Item/HammerData")]
    public class HammerItemSO : ItemSO
    {
        public int RepairAmount = 100;

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
