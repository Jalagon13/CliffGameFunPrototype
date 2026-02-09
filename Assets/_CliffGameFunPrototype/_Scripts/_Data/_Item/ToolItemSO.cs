using UnityEngine;

namespace CliffGame
{
    public enum ToolType
    {
        Hammer,
        Axe,
        Pickaxe,
        Spear
    }

    [CreateAssetMenu(fileName = "New Tool Item Data", menuName = "Item/Tool")]
    public class ToolItemSO : ItemSO
    {
        [field: SerializeField]
        public ToolType ToolType { get; private set; }
        
        [field: SerializeField]
        public float SwingCooldownInSeconds { get; private set; } = 0.325f;
        
        [field: SerializeField]
        public int IntValue { get; private set; } // Can be damage or repair amount depending on tool type

        [field: SerializeField]
        public GameObject HeldToolPrefab { get; private set; } 

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
