using UnityEngine;

namespace CliffGame
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _craftingUIPanel;

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
        }

        private void Hide()
        {
            _craftingUIPanel.SetActive(false);
        }
    }
}
