using System;
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

        private void Start()
        {
            if (Player.Instance.FirstPersonLook.ExecuteStartingSequence)
            {
                Player.Instance.FirstPersonLook.OnStartSequenceFinished += GiveStartingItems;
            }
            else
            {
                GiveStartingItems();
            }
        }
        
        private void OnDestroy()
        {
            if (Player.Instance.FirstPersonLook.ExecuteStartingSequence)
            {
                Player.Instance.FirstPersonLook.OnStartSequenceFinished -= GiveStartingItems;
            }
        }

        private void GiveStartingItems()
        {
            StartCoroutine(GivePlayerStartingItems());
        }

        private IEnumerator GivePlayerStartingItems()
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
