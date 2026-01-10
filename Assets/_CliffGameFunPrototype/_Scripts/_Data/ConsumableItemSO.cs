using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Consumable Item Data", menuName = "Item/ConsumableData")]
    public class ConsumableItemSO : ItemSO
    {
        [field: SerializeField]
        public int HealAmount { get; private set; }
    
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
