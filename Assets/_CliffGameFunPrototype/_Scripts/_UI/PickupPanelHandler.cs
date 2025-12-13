using System;
using UnityEngine;

namespace CliffGame
{
    public class PickupPanelHandler : MonoBehaviour
    {
        [SerializeField] private PickupPanelUI _pickupPanelUIPrefab;
        [SerializeField] private float _itemPickupSFXCooldown = 0.2f;
        
        private Timer _itemPickupSFXTimer;
        
        private void Awake()
        {
            _itemPickupSFXTimer = new Timer(_itemPickupSFXCooldown);
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnItemPickup += InventoryManager_OnItemPickup;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnItemPickup -= InventoryManager_OnItemPickup;
        }
        
        private void Update()
        {
            _itemPickupSFXTimer.Tick(Time.deltaTime);
        }

        private void InventoryManager_OnItemPickup(InventoryItem item)
        {
            PickupPanelUI pickupPanelUI = Instantiate(_pickupPanelUIPrefab.gameObject, transform).GetComponent<PickupPanelUI>();
            pickupPanelUI.Setup(item);
            
            if(_itemPickupSFXTimer.RemainingSeconds <= 0f)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ItemPickupSFX, Player.Instance.transform.position);
                _itemPickupSFXTimer.Reset();
            }
        }
    }
}
