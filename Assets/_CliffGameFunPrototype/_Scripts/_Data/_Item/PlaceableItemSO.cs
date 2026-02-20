using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Placeable Item Data", menuName = "Item/PlaceableItemData")]
    public class PlaceableItemSO : ItemSO
    {
        [field: SerializeField]
        public GameObject PlaceablePrefab { get; private set; }
        
        [field: SerializeField]
        public bool IsCliffPlaceable { get; private set; } = false;

        private float _minFloorPlacementAngle = 0f;
        private float _maxFloorPlacementAngle = 45f;
        private float _minCliffPlacementAngle = 80f;
        private float _maxCliffPlacementAngle = 100f;

        public bool IsPlacementAngleValid(float angle)
        {
            if(IsCliffPlaceable)
            {
                return angle >= _minCliffPlacementAngle && angle <= _maxCliffPlacementAngle;
            }
            else
            {
                return angle >= _minFloorPlacementAngle && angle <= _maxFloorPlacementAngle;
            }
        }

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
