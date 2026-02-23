using FMODUnity;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CliffGame
{
    public class Resource : MonoBehaviour, IInteractable
    {
        [Header("Resource Settings")]
        [SerializeField] 
        private ToolType _requiredToolType;
    
        [SerializeField]
        private int _maxLife = 3;
        private int _currentLife;

        [System.Serializable]
        protected class HarvestDrop
        {
            [Tooltip("Item to drop when the resource is destroyed")]
            public ItemSO DropItem;

            [Tooltip("Minimum amount of this item to drop")]
            public int MinAmount = 1;

            [Tooltip("Maximum amount of this item to drop")]
            public int MaxAmount = 1;
        }

        [SerializeField]
        protected List<HarvestDrop> _harvestDrops = new List<HarvestDrop>();
        
        [Header("Feel Settings")]
        [SerializeField] 
        private ParticleSystem _crackParticles;
        
        [SerializeField]
        private EventReference _hitSFX;

        [SerializeField]
        protected EventReference _destroySFX;

        protected virtual void Awake()
        {
            _currentLife = _maxLife;
        }

        protected virtual void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.UnregisterResource(this);
            }
        }

        public ToolType BreakToolType => _requiredToolType;

        public virtual void OnInteractWith()
        {
            // Default resource interaction (can be empty)
        }

        public void Collect()
        {
            foreach (HarvestDrop drop in _harvestDrops)
            {
                if (drop.DropItem == null)
                    continue;

                int amount = Random.Range(drop.MinAmount, drop.MaxAmount + 1);

                if (amount <= 0)
                    continue;

                InventoryManager.Instance.AddItem(drop.DropItem, amount);
            }
            
            AudioManager.Instance.PlayOneShot(_destroySFX, transform.position);
            ResourceManager.Instance.UnregisterResource(this);
            Debug.Log($"Destroying resource: {gameObject.name}");
            Destroy(gameObject);
        }

        public virtual void OnHitWithTool()
        {
            if(Player.Instance.ToolHolder.CurrentHeldTool.ToolType != _requiredToolType) return;
        
            if(_crackParticles != null)
            {
                Instantiate(_crackParticles.gameObject, transform.position, Quaternion.identity);
            }

            _currentLife--;

            if (_currentLife <= 0)
            {
                Collect();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(_hitSFX, transform.position);
            }
        }
    }
}
