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
                    if (inventoryItem.CanStackWith(mouseItem))
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
                    if (!inventoryItem.IsStackable)
                    {
                        _mouseItemModel.MouseInventoryItem = inventoryItem;
                        inventory.InventoryItems[clickedInventorySlotIndex] = new();
                        didChange = true;
                        Tooltip.HideUI();
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

                    if (inventory.InventoryItems[clickedInventorySlotIndex].Quantity == 0)
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex] = new();
                    }

                    Tooltip.HideUI();
                    }
                }
            }
            else
            {
                if (mouseItem.HasItem)
                {
                    if (mouseItem.IsStackable)
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex].Item = mouseItem.Item;
                        inventory.InventoryItems[clickedInventorySlotIndex].Quantity = 1;
                        _mouseItemModel.MouseInventoryItem.Quantity -= 1;
                    }
                    else
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex] = mouseItem;
                        _mouseItemModel.MouseInventoryItem = new();
                    }

                    didChange = true;
                    if (_mouseItemModel.MouseInventoryItem.HasItem && _mouseItemModel.MouseInventoryItem.Quantity <= 0)
                    {
                        _mouseItemModel.Clear();
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
                    if (inventoryItem.CanStackWith(mouseItem))
                    {
                        inventory.InventoryItems[clickedInventorySlotIndex].Quantity += mouseItem.Quantity;
                        _mouseItemModel.MouseInventoryItem = new();

                        didChange = true;
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
            Tooltip.JustText(inventoryItem.GetTooltipText(), Color.white, fontSize: 12f);
        }
    }
}
