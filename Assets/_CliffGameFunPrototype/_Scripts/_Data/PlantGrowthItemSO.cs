using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Plant Growth Item Data", menuName = "Item/PlantGrowthData")]
    public class PlantGrowthItemSO : ItemSO
    {
        [field: SerializeField]
        public int TimeToSubtractFromGrowthTimer { get; private set; } = 45;
    
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
