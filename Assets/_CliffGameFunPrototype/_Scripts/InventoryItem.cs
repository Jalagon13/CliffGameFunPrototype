using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CliffGame
{
    // This class is the "manifestation" of the item that gets passed around in actual inventory slots
    [Serializable]
    public class InventoryItem
    {
        public ItemSO Item;
        public int Quantity;
        public bool HasItem => Item != null;
        public ulong Id { get; private set; }

        public InventoryItem()
        {
            Item = null;
            Quantity = 0;
        }

        public InventoryItem(ItemSO itemSO, int quantity)
        {
            Item = itemSO;

            if (Item != null)
            {
                Quantity = quantity;
                Id = IdGenerator.GenerateRandomId();
            }
        }

        public void SetId(ulong newId)
        {
            Id = newId;
        }
    }
}