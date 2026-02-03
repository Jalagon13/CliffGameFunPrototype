using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class StartingItemsHandler : MonoBehaviour
    {
        [SerializeField]
        private float _initialDelay = 0.375f, _delayBetweenItemsGiven = 0.15f;

        [SerializeField]
        private InventoryItem[] _startingItems;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(_initialDelay); // Wait a frame to ensure InventoryManager is initialized

            foreach (var item in _startingItems)
            {
                InventoryManager.Instance.AddItem(item.Item, item.Quantity);
                yield return new WaitForSeconds(_delayBetweenItemsGiven);
            }
            
            InventoryManager.Instance.InventoryModel.UpdateInventory();
        }
    }
}
