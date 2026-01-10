using FMODUnity;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CliffGame
{
    public class Resource : MonoBehaviour, IInteractable
    {
        [Header("Game Settings")]
        [SerializeField] 
        private ToolType _requiredToolType;
    
        [SerializeField]
        private int _maxLife = 3;
        private int _currentLife;

        [System.Serializable]
        private class HarvestDrop
        {
            [Tooltip("Item to drop when the resource is destroyed")]
            public ItemSO DropItem;

            [Tooltip("Minimum amount of this item to drop")]
            public int MinAmount = 1;

            [Tooltip("Maximum amount of this item to drop")]
            public int MaxAmount = 1;
        }

        [SerializeField]
        private List<HarvestDrop> _harvestDrops = new List<HarvestDrop>();
        
        [Header("Feel Settings")]
        [SerializeField] 
        private ParticleSystem _crackParticles;
        
        [SerializeField]
        private EventReference _hitSFX;

        [SerializeField]
        private EventReference _destroySFX;

        protected virtual void Awake()
        {
            _currentLife = _maxLife;
        }

        private void OnDestroy()
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

        public virtual void OnHitWithTool()
        {
            if(Player.Instance.ToolHolder.CurrentHeldTool.ToolType != _requiredToolType) return;
        
            AudioManager.Instance.PlayOneShot(_hitSFX, transform.position);
            
            if(_crackParticles != null)
            {
                Instantiate(_crackParticles.gameObject, transform.position, Quaternion.identity);
            }

            _currentLife--;

            if (_currentLife <= 0)
            {
                foreach (HarvestDrop drop in _harvestDrops)
                {
                    if (drop.DropItem == null)
                        continue;

                    int amount = Random.Range(drop.MinAmount, drop.MaxAmount + 1);

                    if (amount <= 0)
                        continue;
                        
                    for (int i = 0; i < amount; i++)
                    {
                        InventoryManager.Instance.AddItem(drop.DropItem, 1);
                    }
                }
                
                ResourceManager.Instance.UnregisterResource(this);
                Destroy(gameObject);
            }
        }
    }
}
