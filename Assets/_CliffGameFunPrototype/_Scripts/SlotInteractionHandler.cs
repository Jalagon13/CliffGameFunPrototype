using System;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using UnityEngine;

namespace CliffGame
{
    public class SlotInteractionHandler
    {
        public Action OnInventorySlotClicked;

        private InventoryModel _inventoryModel;
        private MouseItemModel _mouseItemModel;
    
        public SlotInteractionHandler(InventoryModel inventoryModel, MouseItemModel mouseItemModel)
        {
            _inventoryModel = inventoryModel;
            _mouseItemModel = mouseItemModel;
        }
    
        public void InventorySlotRightClicked(int clickedInventorySlotIndex, InventoryModel inventory)
        {
            bool didChange = false;

            InventoryItem inventoryItem = inventory.InventoryItems[clickedInventorySlotIndex];
            InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;

            if (inventoryItem.HasItem)
            {
                if (mouseItem.HasItem) // Normal functionality
                {
                    if (inventoryItem.Item.InGameName == mouseItem.Item.InGameName)
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex].Quantity += 1;
                        _mouseItemModel.MouseInventoryItem.Quantity -= 1;

                        didChange = true;

                        if (_mouseItemModel.MouseInventoryItem.Quantity <= 0)
                        {
                            _mouseItemModel.Clear();
                        }
                    }
                    else
                    {
                        // Swap the two items
                        InventoryItem tempItem = inventoryItem;

                        inventory.InventoryItems[clickedInventorySlotIndex] = mouseItem;
                        _mouseItemModel.MouseInventoryItem = tempItem;

                        didChange = true;
                    }
                }
                else
                {
                    int inventoryItemQuantity = inventoryItem.Quantity;
                    int newInventoryItemQuantity = inventoryItemQuantity / 2;
                    int newMouseItemQuantity = inventoryItemQuantity - newInventoryItemQuantity;

                    inventory.InventoryItems[clickedInventorySlotIndex].Quantity = newInventoryItemQuantity;

                    _mouseItemModel.MouseInventoryItem.Item = inventoryItem.Item;
                    _mouseItemModel.MouseInventoryItem.Quantity = newMouseItemQuantity;

                    didChange = true;

                    if (inventoryItem.Quantity == 0)
                    {

                        inventory.InventoryItems[clickedInventorySlotIndex].Item = null;
                    }

                    Tooltip.HideUI();
                }
            }
            else
            {
                if (mouseItem.HasItem)
                {

                    inventory.InventoryItems[clickedInventorySlotIndex].Item = mouseItem.Item;
                    inventory.InventoryItems[clickedInventorySlotIndex].Quantity = 1;

                    _mouseItemModel.MouseInventoryItem.Quantity -= 1;
                    didChange = true;
                    if (_mouseItemModel.MouseInventoryItem.Quantity <= 0)
                    {
                        _mouseItemModel.Clear();

                        ShowInventoryItemTooltip(_mouseItemModel.MouseInventoryItem);
                    }
                }
            }

            if (didChange)
            {
                _inventoryModel.UpdateInventory();
                OnInventorySlotClicked?.Invoke();
                PlayClickFeedbacks();
            }
        }

        public void InventorySlotLeftClicked(int clickedInventorySlotIndex, InventoryModel inventory)
        {
            bool didChange = false;

            InventoryItem inventoryItem = inventory.InventoryItems[clickedInventorySlotIndex];
            InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;

            if (inventoryItem.HasItem)
            {
                if (mouseItem.HasItem)
                {
                    if (inventoryItem.Item.InGameName == mouseItem.Item.InGameName && mouseItem.Item.Stackable)
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex].Quantity += mouseItem.Quantity;
                        _mouseItemModel.MouseInventoryItem = new();

                        didChange = true;

                        ShowInventoryItemTooltip(_mouseItemModel.MouseInventoryItem);
                    }
                    else
                    {
                        // Swap the two items
                        InventoryItem tempItem = inventoryItem;

                        inventory.InventoryItems[clickedInventorySlotIndex] = mouseItem;
                        _mouseItemModel.MouseInventoryItem = tempItem;

                        didChange = true;
                    }
                }
                else
                {
                    _mouseItemModel.MouseInventoryItem = inventoryItem;
                    inventory.InventoryItems[clickedInventorySlotIndex] = new();

                    didChange = true;

                    Tooltip.HideUI();
                }
            }
            else
            {
                if (mouseItem.HasItem)
                {
                    inventory.InventoryItems[clickedInventorySlotIndex] = mouseItem;
                    _mouseItemModel.MouseInventoryItem = new();

                    didChange = true;

                    ShowInventoryItemTooltip(inventory.InventoryItems[clickedInventorySlotIndex]);
                }
            }

            if (didChange)
            {
                PlayClickFeedbacks();
                _inventoryModel.UpdateInventory();
                OnInventorySlotClicked?.Invoke();
            }
        }

        public void PlayClickFeedbacks()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SlotClickedSFX, Player.Instance.transform.position);
        }

        public void ShowInventoryItemTooltip(InventoryItem inventoryItem)
        {
            if (!inventoryItem.HasItem)
            {
                Debug.LogWarning($"Trying to display an inventory item that does not exists for {inventoryItem}");
                return;
            }

            Tooltip.ShowNew();

            string quantityString = inventoryItem.Quantity > 1 ? $"[{inventoryItem.Quantity}]" : string.Empty;
            string itemText = $"{inventoryItem.Item.InGameName} {quantityString}<br>{inventoryItem.Item.GetDescription()}";

            Tooltip.JustText(itemText, Color.white, fontSize: 12f);
        }
    }
}
