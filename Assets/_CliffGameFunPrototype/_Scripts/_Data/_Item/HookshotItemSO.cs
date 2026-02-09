using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Hookshot Item Data", menuName = "Item/HookshotData")]
    public class HookshotItemSO : ItemSO
    {
        [field: SerializeField]
        public float ChargeDuration { get; private set; } = 1f;

        [field: SerializeField]
        public float HookRange { get; private set; } = 15f;

        [field: SerializeField]
        public float HookSequenceDuration { get; private set; } = 1.85f;

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
