using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Plantable Item Data", menuName = "Item/PlantableData")]
    public class PlantableItemSO : ItemSO
    {
        [field: SerializeField]
        public GameObject PlantedModel { get; private set; }

        [field: SerializeField]
        public Resource EndResultResource { get; private set; }

        [field: SerializeField]
        public float GrowthTimeInSeconds { get; private set; }

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
