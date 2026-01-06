using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Plantable Item Data", menuName = "Item/PlantableData")]
    public class PlantableItemSO : ItemSO
    {
        [field: SerializeField]
        public float GrowthTimeInSeconds { get; private set; }
    
        [field: SerializeField]
        public GameObject PlantPrefab { get; private set; }

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
