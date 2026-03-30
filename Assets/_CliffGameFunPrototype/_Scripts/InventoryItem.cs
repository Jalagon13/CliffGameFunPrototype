using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CliffGame
{
    public enum ToolDurabilityState
    {
        Operational,
        Depleted
    }

    // This class is the "manifestation" of the item that gets passed around in actual inventory slots
    [Serializable]
    public class InventoryItem
    {
        public ItemSO Item;
        public int Quantity;
        public bool HasItem => Item != null;
        public ulong Id { get; private set; }
        public int CurrentDurability { get; private set; }
        public int MaxDurability { get; private set; }
        public bool UsesDurability => Item is ToolItemSO;
        public bool IsStackable => HasItem && !UsesDurability && Item.Stackable;
        public ToolDurabilityState DurabilityState => !UsesDurability || CurrentDurability > 0
            ? ToolDurabilityState.Operational
            : ToolDurabilityState.Depleted;
        public float DurabilityNormalized => !UsesDurability || MaxDurability <= 0
            ? 1f
            : Mathf.Clamp01((float)CurrentDurability / MaxDurability);

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
                InitializeRuntimeItemState();
            }
        }

        public void SetId(ulong newId)
        {
            Id = newId;
        }

        public bool CanStackWith(InventoryItem other)
        {
            if (other == null || !HasItem || !other.HasItem)
            {
                return false;
            }

            return IsStackable &&
                   other.IsStackable &&
                   Item.InGameName == other.Item.InGameName;
        }

        public void AddDurability(int amount)
        {
            if (!UsesDurability || amount <= 0)
            {
                return;
            }

            CurrentDurability = Mathf.Clamp(CurrentDurability + amount, 0, MaxDurability);
        }

        public void ConsumeDurability(int amount)
        {
            if (!UsesDurability || amount <= 0)
            {
                return;
            }

            CurrentDurability = Mathf.Clamp(CurrentDurability - amount, 0, MaxDurability);
        }

        public int RollResourceDamage()
        {
            if (Item is not ToolItemSO toolItem)
            {
                return 0;
            }

            int minDamage = Mathf.Min(toolItem.ResourceDamageMin, toolItem.ResourceDamageMax);
            int maxDamage = Mathf.Max(toolItem.ResourceDamageMin, toolItem.ResourceDamageMax);
            int rolledDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

            if (DurabilityState == ToolDurabilityState.Depleted)
            {
                rolledDamage = Mathf.Max(1, Mathf.RoundToInt(rolledDamage * 0.5f));
            }

            return rolledDamage;
        }

        public float GetResourceSwingCooldownMultiplier()
        {
            return DurabilityState == ToolDurabilityState.Depleted ? 2f : 1f;
        }

        public string GetTooltipText()
        {
            if (!HasItem)
            {
                return string.Empty;
            }

            string quantityString = Quantity > 1 ? $"({Quantity})" : string.Empty;
            string itemText = $"{Item.InGameName} {quantityString}<br>{Item.GetDescription()}";

            if (UsesDurability)
            {
                itemText += $"<br>Durability: {CurrentDurability}/{MaxDurability}";
                itemText += $"<br>State: {DurabilityState}";
            }

            return itemText;
        }

        public InventoryItem CreateIndependentCopy(int quantity)
        {
            InventoryItem copy = new InventoryItem(Item, quantity);

            if (UsesDurability)
            {
                copy.MaxDurability = MaxDurability;
                copy.CurrentDurability = CurrentDurability;
            }

            return copy;
        }

        private void InitializeRuntimeItemState()
        {
            if (Item is not ToolItemSO toolItem)
            {
                CurrentDurability = 0;
                MaxDurability = 0;
                return;
            }

            MaxDurability = Mathf.Max(1, toolItem.MaxDurability);
            CurrentDurability = MaxDurability;
        }
    }
}
