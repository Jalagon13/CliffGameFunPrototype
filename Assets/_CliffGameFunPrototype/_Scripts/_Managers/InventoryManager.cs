using System;
using System.Collections;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CliffGame
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;
        public bool MouseHasItem { get; private set; }

        public Action<InventoryItem> OnMouseItemUpdated;
        public event EventHandler<OnInventoryUpdatedEventArgs> OnInventoryUpdated;
        public class OnInventoryUpdatedEventArgs : EventArgs
        {
            public List<InventoryItem> InventoryItems;
        }
        
        public Action<InventoryItem> OnItemPickup;

        [SerializeField]
        private int _slotAmount = 10, _hotbarSlotAmount = 9;

        private InventoryModel _inventoryModel;
        public InventoryModel InventoryModel => _inventoryModel;

        [SerializeField]
        private int _startingHotbarIndex = 1;

        private int _selectedSlotIndexBacking;
        public event Action<int, InventoryItem> OnSelectedSlotChanged;
        public int SelectedSlotIndex
        {
            get => _selectedSlotIndexBacking;
            set
            {
                if (_selectedSlotIndexBacking != value)
                {
                    _selectedSlotIndexBacking = value;
                    InventoryItem selectedItem = null;
                    if (_inventoryModel != null && _inventoryModel.InventoryItems.Count > _selectedSlotIndexBacking)
                        selectedItem = _inventoryModel.InventoryItems[_selectedSlotIndexBacking];
                    OnSelectedSlotChanged?.Invoke(_selectedSlotIndexBacking, selectedItem);
                }
            }
        }

        public bool HasSelectedItem => SelectedInventoryItem.Item != null;
        public InventoryItem SelectedInventoryItem
        {
            get => _inventoryModel.InventoryItems[_selectedSlotIndexBacking];
        }
        
        private SlotInteractionHandler _slotInteractionHandler;
        public SlotInteractionHandler SlotInteractionHandler => _slotInteractionHandler;
        
        private MouseItemModel _mouseItemModel;
        private bool _gotItemThisFrame, _gaveItemThisFrame;

        private void Awake()
        {
            Instance = this;

            _selectedSlotIndexBacking = _startingHotbarIndex;

            _inventoryModel = new(_slotAmount);
            _inventoryModel.OnInventoryUpdate += InventoryModel_OnInventoryUpdate;

            _mouseItemModel = new();

            _slotInteractionHandler = new(_inventoryModel, _mouseItemModel);
        }
        
        private IEnumerator Start()
        {
            HealthManager.Instance.OnPlayerDeath += OnPlayerDeath;
            GameInput.Instance.OnScrollWheel += OnScrollWheel;
            GameInput.Instance.OnSelectSlot += OnSelectSlot;

            yield return null; // waits one frame
            _inventoryModel.UpdateInventory();
            OnSelectedSlotChanged?.Invoke(_startingHotbarIndex, _inventoryModel.InventoryItems[_startingHotbarIndex]); // manually set it to the second index
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnPlayerDeath -= OnPlayerDeath;
            GameInput.Instance.OnScrollWheel -= OnScrollWheel;
            GameInput.Instance.OnSelectSlot -= OnSelectSlot;

            _inventoryModel.OnInventoryUpdate -= InventoryModel_OnInventoryUpdate;
        }

        private void Update()
        {
            if (_mouseItemModel.MouseInventoryItem.HasItem)
            {
                if (_gotItemThisFrame) return;

                UpdateMouseItem();

                _gotItemThisFrame = true;
                _gaveItemThisFrame = false;

                MouseHasItem = true;
                // Tooltip.HideUI();
            }
            else if (!_gaveItemThisFrame)
            {
                UpdateMouseItem();

                _gaveItemThisFrame = true;
                _gotItemThisFrame = false;

                MouseHasItem = false;
            }
        }

        private void UpdateMouseItem()
        {
            OnMouseItemUpdated?.Invoke(_mouseItemModel.MouseInventoryItem);
        }

        private void OnPlayerDeath()
        {
            for (int i = _inventoryModel.InventoryItems.Count - 1; i >= 0; i--)
            {
                InventoryItem item = _inventoryModel.InventoryItems[i];

                if (!item.HasItem) continue;
                if (UnityEngine.Random.value < 0.5f) continue;

                // 50% chance the item gets deleted
                int itemAmount = item.Quantity;
                ItemSO invItem = item.Item;

                // If there's only one item in the slot and it was chosen to be deleted, remove it.
                if (itemAmount <= 1)
                {
                    RemoveItem(invItem, 1);
                    continue;
                }

                // Use float division so stacks like 3/2 => 1.5 and RoundToInt behaves as expected.
                int amountToLose = Mathf.RoundToInt(itemAmount / 2f);
                if (amountToLose <= 0) amountToLose = 1; // ensure at least one is removed for stacks > 1

                RemoveItem(invItem, amountToLose);
            }
        }


        private void InventoryModel_OnInventoryUpdate(List<InventoryItem> items)
        {
            UpdateMouseItem();

            OnInventoryUpdated?.Invoke(this, new OnInventoryUpdatedEventArgs
            {
                InventoryItems = items
            });
        }

        public bool InventoryHasItems(InventoryItem[] itemList)
        {
            foreach (InventoryItem item in itemList)
            {
                if (!InventoryHasItem(item.Item, item.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool InventoryHasItem(ItemSO item, int quantity)
        {
            return _inventoryModel.Contains(new(item, quantity));
        }

        public void AddItem(ItemSO ItemToAdd, int quantity)
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            var itemToAdd = ItemToAdd.CreateInventoryItem(quantity);
            
            _inventoryModel.AddItem(itemToAdd);
            OnItemPickup?.Invoke(itemToAdd);
        }

        public void AddItems(InventoryItem[] itemsToAdd)
        {
            foreach (InventoryItem item in itemsToAdd)
            {
                AddItem(item.Item, item.Quantity);
            }
        }

        public void TryToCraft(RecipeDataSO recipe)
        {
            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;

            _inventoryModel.TryToCraft(recipe);
        }

        public void RemoveItem(ItemSO ItemToAdd, int quantity)
        {
            _inventoryModel.RemoveItem(ItemToAdd, quantity);
        }

        public void RemoveItems(InventoryItem[] itemsToRemove)
        {
            foreach (InventoryItem item in itemsToRemove)
            {
                RemoveItem(item.Item, item.Quantity);
            }
        }

        private void OnScrollWheel(object sender, InputAction.CallbackContext context)
        {
            if (!context.performed || Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead 
            || CraftingManager.Instance.CraftingMenuUIOpened /* || InteractionManager.Instance.IsChargingItem */) return;

            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = _hotbarSlotAmount;
            if (itemCount == 0) return;

            if (scrollDelta.y > 0f) // Scroll up
            {
                int upcomingIndex = SelectedSlotIndex - 1;
                if (upcomingIndex < 0)
                {
                    SelectedSlotIndex = itemCount - 1; // Wrap to last item
                }
                else
                {
                    SelectedSlotIndex--;
                }
            }
            else if (scrollDelta.y < 0f) // Scroll down
            {
                int upcomingIndex = SelectedSlotIndex + 1;
                if (upcomingIndex >= itemCount)
                {
                    SelectedSlotIndex = 0; // Wrap to first item
                }
                else
                {
                    SelectedSlotIndex++;
                }
            }
        }

        private void OnSelectSlot(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead
            || CraftingManager.Instance.CraftingMenuUIOpened /* || InteractionManager.Instance.IsChargingItem */) return;

            var control = context.control; // The control (key/button) that triggered this

            // If the key is Digit1–Digit8
            if (control is KeyControl key)
            {
                int slotIndex = key.keyCode - Key.Digit1; // Convert Key.Digit1 to 0, Digit2 to 1, etc.
                if (slotIndex >= 0 && slotIndex < _inventoryModel.InventoryItems.Count)
                {
                    SelectedSlotIndex = slotIndex;
                }
            }
        }
    }
}
