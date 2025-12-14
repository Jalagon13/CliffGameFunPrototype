using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class StartingItemsHandler : MonoBehaviour
    {
        [SerializeField]
        private float _delayBeforeGivingItems = 0.15f;

        [SerializeField]
        private InventoryItem[] _startingItems;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(_delayBeforeGivingItems); // Wait a frame to ensure InventoryManager is initialized

            foreach (var item in _startingItems)
            {
                InventoryManager.Instance.AddItem(item.Item, item.Quantity);
                yield return new WaitForSeconds(_delayBeforeGivingItems);
            }
            
            Debug.Log("Starting items added to inventory.");
            InventoryManager.Instance.InventoryModel.UpdateInventory();
        }
    }
}
