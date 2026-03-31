using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Consumable Item Data", menuName = "Item/ConsumableData")]
    public class ConsumableItemSO : ItemSO
    {
        [field: SerializeField]
        public int HungerAmount { get; private set; }

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
