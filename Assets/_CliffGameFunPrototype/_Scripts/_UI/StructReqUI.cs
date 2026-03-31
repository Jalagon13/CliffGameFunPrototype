using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class StructReqUI : MonoBehaviour
    {
        [SerializeField] private Image _itemImage;
        [SerializeField] private TextMeshProUGUI _amountText;

        private InventoryItem _reqs;

        private void Start()
        {
            InventoryManager.Instance.OnInventoryUpdated += UpdateStructReqUIs;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryUpdated -= UpdateStructReqUIs;
        }

        public void Initialize(InventoryItem structReqs)
        {
            _reqs = structReqs;
            _itemImage.sprite = structReqs.Item.UiDisplay;
            UpdateText();
        }

        private void UpdateStructReqUIs(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (_reqs == null || _reqs.Item == null)
            {
                return;
            }

            int itemAmountInInventory = InventoryManager.Instance.InventoryModel.GetAmount(_reqs.Item);
            bool hasEnough = itemAmountInInventory >= _reqs.Quantity;
            string colorTagStart = hasEnough ? string.Empty : "<color=red>";
            string colorTagEnd = hasEnough ? string.Empty : "</color>";

            _amountText.text = $"{colorTagStart}{_reqs.Item.InGameName}<br>{itemAmountInInventory}/{_reqs.Quantity} {colorTagEnd}";
        }
    }
}
