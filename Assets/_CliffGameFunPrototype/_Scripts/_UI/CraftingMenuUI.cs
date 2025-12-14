using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;

namespace CliffGame
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject _craftingUIPanel, _mainInventoryUI;
        [SerializeField] private RectTransform _hotbarUI;
        [SerializeField] private float _hotbarRaisedYPosition = 200f;
        
        [Header("Crafting")]
        [SerializeField] private GameObject _craftSlotHolder;
        [SerializeField] private CraftSlotUI _craftSlotUIPrefab;
        [SerializeField] private List<RecipeSO> _availableRecipes;

        private void Start()
        {
            Hide();
            InitializeCraftingSlots();

            CraftingManager.Instance.OnCraftingUIOpened += CraftingManager_OnCraftingUIOpened;
            CraftingManager.Instance.OnCraftingUIClosed += CraftingManager_OnCraftingUIClosed;
            InventoryManager.Instance.OnInventoryUpdated += UpdateCraftStatus;
        }
        
        private void OnDestroy()
        {
            CraftingManager.Instance.OnCraftingUIOpened -= CraftingManager_OnCraftingUIOpened;
            CraftingManager.Instance.OnCraftingUIClosed -= CraftingManager_OnCraftingUIClosed;
            InventoryManager.Instance.OnInventoryUpdated -= UpdateCraftStatus;
        }

        private void UpdateCraftStatus(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            // Update all craft slots
            foreach (Transform child in _craftSlotHolder.transform)
            {
                CraftSlotUI craftSlotUI = child.GetComponent<CraftSlotUI>();
                if (craftSlotUI != null)
                {
                    craftSlotUI.UpdateCraftStatus();
                }
            }
        }

        private void CraftingManager_OnCraftingUIOpened()
        {
            // Show crafting UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Show();
        }

        private void CraftingManager_OnCraftingUIClosed()
        {
            // Hide crafting UI
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Hide();
        }

        private void InitializeCraftingSlots()
        {
            foreach (RecipeSO recipe in _availableRecipes)
            {
                CraftSlotUI craftSlotUI = Instantiate(_craftSlotUIPrefab, _craftSlotHolder.transform);
                craftSlotUI.InitializeCraftNode(recipe);
            }
        }

        private void Show()
        {
            _craftingUIPanel.SetActive(true);
            _mainInventoryUI.SetActive(true);

            Vector2 anchoredPos = _hotbarUI.anchoredPosition;
            anchoredPos.y = _hotbarRaisedYPosition;
            _hotbarUI.anchoredPosition = anchoredPos;
        }

        private void Hide()
        {
            _craftingUIPanel.SetActive(false);
            _mainInventoryUI.SetActive(false);

            Vector2 anchoredPos = _hotbarUI.anchoredPosition;
            anchoredPos.y = 0f;
            _hotbarUI.anchoredPosition = anchoredPos;
        }
    }
}
