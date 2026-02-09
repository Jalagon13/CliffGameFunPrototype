using UnityEngine;

namespace CliffGame
{
    public enum ConsumableType
    {
        Food,
        Drink
    }

    [CreateAssetMenu(fileName = "New Consumable Item Data", menuName = "Item/ConsumableData")]
    public class ConsumableItemSO : ItemSO
    {
        [field: SerializeField]
        public int HealAmount { get; private set; }

        [field: SerializeField]
        public float ConsumeDuration { get; private set; } = 1f;
        
        [field: SerializeField]
        public ConsumableType ConsumableType { get; private set; }

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
