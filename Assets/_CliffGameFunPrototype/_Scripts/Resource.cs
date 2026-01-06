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

        [SerializeField]
        private List<InventoryItem> _harvestItems = new List<InventoryItem>();
        
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
                foreach (InventoryItem item in _harvestItems)
                {
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        InventoryManager.Instance.AddItem(item.Item, 1);
                    }
                }
                Destroy(gameObject);
            }
        }
    }
}
