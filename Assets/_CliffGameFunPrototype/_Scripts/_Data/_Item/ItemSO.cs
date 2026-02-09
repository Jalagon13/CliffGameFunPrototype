using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    [Serializable]
    public abstract class ItemSO : ScriptableObject
    {
        [field: SerializeField] public string InGameName { get; private set; }
        [field: SerializeField] public GameObject ItemWorldPrefab { get; private set; }
        [field: SerializeField] public Sprite UiDisplay { get; private set; }
        [field: SerializeField] public bool Stackable { get; private set; } = true;
        [field: TextArea]
        [field: SerializeField] public string Description { get; private set; }

        public abstract InventoryItem CreateInventoryItem(int quantity);
        public abstract string GetDescription();

        // Returns description with line breaks
        protected string GetDescriptionBreak()
        {
            string description = "";
            if (!string.IsNullOrWhiteSpace(Description))
                description += $"{Description}<br>";

            return description;
        }
    }
}