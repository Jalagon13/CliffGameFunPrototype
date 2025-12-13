using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Resource Item Data", menuName = "Item/ResourceData")]
    public class ResourceDataSO : ItemSO
    {
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
