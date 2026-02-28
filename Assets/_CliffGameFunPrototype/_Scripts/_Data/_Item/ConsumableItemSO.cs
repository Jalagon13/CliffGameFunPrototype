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
        public int HungerAmount { get; private set; }

        [field: SerializeField]
        public int ThirstAmount { get; private set; }

        [field: SerializeField]
        public float ConsumeDuration { get; private set; } = 1f;

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
