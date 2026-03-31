using System;
using UnityEngine;

namespace CliffGame
{
    public class PickupPanelHandler : MonoBehaviour
    {
        public static PickupPanelHandler Instance { get; private set; }

        [SerializeField] private PickupPanelUI _pickupPanelUIPrefab;
        [SerializeField] private float _itemPickupSFXCooldown = 0.2f;
        
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
            CreatePanel().Setup(item);
            
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
