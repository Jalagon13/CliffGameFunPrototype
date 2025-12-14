using UnityEngine;

namespace CliffGame
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject _craftingUIPanel, _mainInventoryUI;
        [SerializeField] private RectTransform _hotbarUI;
        [SerializeField] private float _hotbarRaisedYPosition = 200f;

        private void Start()
        {
            Hide();

            CraftingManager.Instance.OnCraftingUIOpened += CraftingManager_OnCraftingUIOpened;
            CraftingManager.Instance.OnCraftingUIClosed += CraftingManager_OnCraftingUIClosed;
        }

        private void OnDestroy()
        {
            CraftingManager.Instance.OnCraftingUIOpened -= CraftingManager_OnCraftingUIOpened;
            CraftingManager.Instance.OnCraftingUIClosed -= CraftingManager_OnCraftingUIClosed;
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
            // Debug.Log("Crafting UI Closed");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Hide();
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
