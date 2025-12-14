using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance;

        public event Action OnCraftingUIOpened;
        public event Action OnCraftingUIClosed;

        private bool _craftingMenuUIOpened;
        public bool CraftingMenuUIOpened => _craftingMenuUIOpened;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            GameInput.Instance.OnToggleCraftingMenu += OnCraftingToggle;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnToggleCraftingMenu -= OnCraftingToggle;
        }

        private void OnCraftingToggle(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead || Player.Instance.CurrentMoveStateType == PlayerMoveState.Climbing) return;

            // If trying to CLOSE the crafting menu while mouse has an item, block it
            if (_craftingMenuUIOpened && InventoryManager.Instance.MouseHasItem)
            {
                return;
            }

            // Otherwise toggle normally
            _craftingMenuUIOpened = !_craftingMenuUIOpened;

            if (_craftingMenuUIOpened)
                OnCraftingUIOpened?.Invoke();
            else
                OnCraftingUIClosed?.Invoke();
        }

        public bool HasAllIngredients(List<InventoryItem> recipe)
        {
            if (recipe == null) return false;

            foreach (InventoryItem ingredient in recipe)
            {
                int inventoryAmount = InventoryManager.Instance.InventoryModel.GetAmount(ingredient.Item);
                int requiredAmount = ingredient.Quantity;

                if (inventoryAmount < requiredAmount)
                {
                    return false;
                }
            }

            return true;
        }

        public void TryToCraft(RecipeSO recipe)
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            // Check if all required items are present
            foreach (InventoryItem requiredItem in recipe.RequiredItems)
            {
                if (!InventoryManager.Instance.InventoryModel.Contains(requiredItem))
                {
                    Debug.Log($"Cannot craft {recipe.ResultItem.InGameName}: missing {requiredItem.Item.InGameName} x{requiredItem.Quantity}");
                    return;
                }
            }

            // Remove required items
            foreach (InventoryItem requiredItem in recipe.RequiredItems)
            {
                InventoryManager.Instance.RemoveItem(requiredItem.Item, requiredItem.Quantity);
            }

            // Add crafted item
            InventoryManager.Instance.AddItem(recipe.ResultItem, recipe.ResultAmount);
        }
    }
}
