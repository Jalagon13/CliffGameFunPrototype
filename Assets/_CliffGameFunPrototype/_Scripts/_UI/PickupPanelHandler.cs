using System;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class PickupPanelHandler : MonoBehaviour
    {
        public static PickupPanelHandler Instance { get; private set; }

        [SerializeField] private PickupPanelUI _pickupPanelUIPrefab;
        [SerializeField] private float _itemPickupSFXCooldown = 0.2f;
        
        private List<PickupPanelUI> _activePanels = new List<PickupPanelUI>();

        private Timer _itemPickupSFXTimer;
        
        private void Awake()
        {
            Instance = this;
            _itemPickupSFXTimer = new Timer(_itemPickupSFXCooldown);
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnItemPickup += InventoryManager_OnItemPickup;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnItemPickup -= InventoryManager_OnItemPickup;

            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void Update()
        {
            _itemPickupSFXTimer.Tick(Time.deltaTime);
        }

        private void InventoryManager_OnItemPickup(InventoryItem item)
        {
            // Remove any references to panels that have already been destroyed/expired
            _activePanels.RemoveAll(p => p == null);

            // Check if we already have a panel active for this specific ItemSO
            PickupPanelUI existingPanel = _activePanels.Find(p => p.Item != null && p.Item.Item == item.Item);
            int totalAmount = item.Quantity;

            if (existingPanel != null)
            {
                totalAmount += existingPanel.Item.Quantity;
                _activePanels.Remove(existingPanel);
                Destroy(existingPanel.gameObject);
            }

            PickupPanelUI newPanel = CreatePanel();
            newPanel.Setup(item.CreateIndependentCopy(totalAmount));
            _activePanels.Add(newPanel);
            
            if(_itemPickupSFXTimer.RemainingSeconds <= 0f)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ItemPickupSFX, Player.Instance.transform.position);
                _itemPickupSFXTimer.Reset();
            }
        }

        public void ShowMessage(Sprite icon, string nameText, string amountText = "")
        {
            CreatePanel().SetupCustom(icon, nameText, amountText);
        }

        private PickupPanelUI CreatePanel()
        {
            return Instantiate(_pickupPanelUIPrefab.gameObject, transform).GetComponent<PickupPanelUI>();
        }
    }
}
