using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Structure Item Data", menuName = "Item/StructureItemData")]
    public class StructureItemSO : ItemSO
    {
        [field: SerializeField]
        public GameObject StructurePrefab { get; private set; }

        public override InventoryItem CreateInventoryItem(int quantity)
        {
            return new InventoryItem(this, quantity);
        }

        public override string GetDescription()
        {
            return GetDescriptionBreak();
        }
    }
}
