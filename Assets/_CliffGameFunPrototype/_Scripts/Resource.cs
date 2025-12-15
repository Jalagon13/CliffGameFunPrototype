using UnityEngine;

namespace CliffGame
{
    public class Resource : MonoBehaviour, IInteractable
    {
        [SerializeField] 
        private float _harvestDuration = 1f;
        
        [SerializeField] 
        private ItemSO _harvestItem;
        
        [SerializeField] 
        private int _harvestAmount = 1;
    
        public float InteractionTime => _harvestDuration <= 0 ? 0.01f : _harvestDuration;

        public void ExecuteInteraction()
        {
            InventoryManager.Instance.AddItem(_harvestItem, _harvestAmount);
            Destroy(gameObject);
        }
    }
}
