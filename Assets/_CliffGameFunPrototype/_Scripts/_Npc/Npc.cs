using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace CliffGame
{
    public class Npc : MonoBehaviour, IInteractable
    {
        [Header("Base Resource Settings")]
        [SerializeField]
        private int _maxLife = 20;

        [SerializeField]
        protected List<HarvestDrop> _harvestDrops = new List<HarvestDrop>();
        
        private int _currentLife;

        [Header("Feel Settings")]
        [SerializeField]
        protected ParticleSystem _hitParticles;

        [SerializeField]
        protected ParticleSystem _killParticles;

        [SerializeField]
        private EventReference _hitSFX;

        [SerializeField]
        protected EventReference _killSFX;

        public ToolType BreakToolType => ToolType.None;

        protected virtual void Awake()
        {
            _currentLife = _maxLife;
        }

        public virtual void OnHitWithTool(int damage)
        {
            _currentLife -= damage;
            if(_currentLife > 0)
            {
                AudioManager.Instance.PlayOneShot(_hitSFX, transform.position);
            }
            
            Debug.Log($"Damaged {gameObject.name} for {damage}, hp: {_currentLife}/{_maxLife}");

            if (_currentLife <= 0)
            {
                Collect();
            }
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

            if (_killParticles != null && gameObject.scene.isLoaded)
            {
                Instantiate(_killParticles.gameObject, transform.position, Quaternion.identity);
            }

            AudioManager.Instance.PlayOneShot(_killSFX, transform.position);
            Destroy(gameObject);
        }

        public void OnInteractWith()
        {
            
        }
    }
}
