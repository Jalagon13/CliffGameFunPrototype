using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    [CreateAssetMenu(fileName = "New Recipe Data", menuName = "RecipeData")]
    public class RecipeSO : ScriptableObject
    {
        [field: SerializeField]
        public ItemSO ResultItem { get; private set; }

        [field: SerializeField]
        public int ResultAmount { get; private set; }

        [field: SerializeField]
        public List<InventoryItem> RequiredItems { get; private set; }
    }
}
