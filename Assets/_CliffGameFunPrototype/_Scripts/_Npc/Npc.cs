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
        
        [SerializeField]
        private EventReference _tetherStabSFX;

        public ToolType BreakToolType => ToolType.None;

        protected virtual void Awake()
        {
            _currentLife = _maxLife;
        }

        public virtual void OnHitWithTool(int damage)
        {
            OnHitWithTool(new ResourceHitResult(damage, false, false));
        }

        public virtual void OnHitWithTool(ResourceHitResult hitResult)
        {
            _currentLife -= hitResult.Damage;
            InventoryManager.Instance.TryConsumeSelectedToolDurability();
            if(_currentLife > 0)
            {
                AudioManager.Instance.PlayOneShot(_hitSFX, transform.position);
            }

            if (hitResult.WasCriticalHit)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.CritStrikeSFX, transform.position);
            }
            
            ResourceDamagePopup.Create(
                GetDamagePopupSpawnPosition(),
                hitResult.Damage,
                hitResult.WasCriticalHit,
                hitResult.UsedDepletedTool);

            Debug.Log($"Damaged {gameObject.name} for {hitResult.Damage}, hp: {_currentLife}/{_maxLife}");
            if(_hitParticles != null && gameObject.scene.isLoaded)
                Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);

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

        public virtual void OnTetherStabbed()
        {
            if (_tetherStabSFX.IsNull) return;
            AudioManager.Instance.PlayOneShot(_tetherStabSFX, transform.position);
            if (_hitParticles != null && gameObject.scene.isLoaded)
                Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);
        }

        public void OnInteractWith()
        {
            
        }

        private Vector3 GetDamagePopupSpawnPosition()
        {
            if (InteractionManager.Instance != null &&
                InteractionManager.Instance.CurrentlyHoveredInteractable == this)
            {
                return InteractionManager.Instance.CurrentHoveredInteractableHitPoint;
            }

            Collider npcCollider = GetComponent<Collider>();
            if (npcCollider == null)
            {
                npcCollider = GetComponentInChildren<Collider>();
            }

            if (npcCollider != null)
            {
                Bounds bounds = npcCollider.bounds;
                return bounds.center + Vector3.up * (bounds.extents.y + 0.1f);
            }

            return transform.position + Vector3.up * 1f;
        }
    }
}
