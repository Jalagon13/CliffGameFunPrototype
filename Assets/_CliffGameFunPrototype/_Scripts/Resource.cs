using FMODUnity;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CliffGame
{
    [System.Serializable]
    public class HarvestDrop
    {
        [Tooltip("Item to drop when the resource is destroyed")]
        public ItemSO DropItem;

        [Tooltip("Minimum amount of this item to drop")]
        public int MinAmount = 1;

        [Tooltip("Maximum amount of this item to drop")]
        public int MaxAmount = 1;
    }

    public class Resource : MonoBehaviour, IInteractable
    {
        [Header("Base Resource Settings")]
        [SerializeField] 
        private ToolType _requiredToolType;
    
        [SerializeField]
        private int _maxLife = 3;
        private int _currentLife;
        public int CurrentLife => _currentLife;
        public int MaxLife => _maxLife;

        [SerializeField]
        protected List<HarvestDrop> _harvestDrops = new List<HarvestDrop>();
        
        [Header("Feel Settings")]
        [SerializeField] 
        protected ParticleSystem _crackParticles;

        [SerializeField]
        protected ParticleSystem _destroyParticles;

        [SerializeField]
        private EventReference _hitSFX;

        [SerializeField]
        protected EventReference _destroySFX;

        public ToolType BreakToolType => _requiredToolType;

        protected virtual void Awake()
        {
            _currentLife = _maxLife;
        }

        public virtual void OnInteractWith()
        {
            // Default resource interaction (can be empty)
        }

        public virtual void Collect(bool giveItems = true)
        {
            if(giveItems)
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
            }

            if (_destroyParticles != null && gameObject.scene.isLoaded)
            {
                Instantiate(_destroyParticles.gameObject, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }

        public virtual void OnHitWithTool(int damage)
        {
            OnHitWithTool(new ResourceHitResult(damage, false, false));
        }

        public virtual void OnHitWithTool(ResourceHitResult hitResult)
        {
            if (Player.Instance.ToolHolder.CurrentHeldTool == null || Player.Instance.ToolHolder.CurrentHeldTool.ToolType != _requiredToolType)
            {
                return;
            }
        
            if(_crackParticles != null)
            {
                Instantiate(_crackParticles.gameObject, transform.position, Quaternion.identity);
            }

            _currentLife -= hitResult.Damage;
            _currentLife = Mathf.Clamp(_currentLife, 0, _maxLife);
            ConsumeSelectedToolDurability();
            ResourceDamagePopup.Create(
                GetDamagePopupSpawnPosition(),
                hitResult.Damage,
                hitResult.WasCriticalHit,
                hitResult.UsedDepletedTool);

            if (hitResult.WasCriticalHit)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.CritStrikeSFX, transform.position);
            }

            Debug.Log($"Resource hit: {gameObject.name} took {hitResult.Damage} damage. HP: {_currentLife}/{_maxLife}");

            if (_currentLife <= 0)
            {
                Collect();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(_hitSFX, transform.position);
            }
        }

        private void ConsumeSelectedToolDurability()
        {
            if (!InventoryManager.Instance.TryGetSelectedToolInventoryItem(out InventoryItem selectedToolItem, out _))
            {
                return;
            }

            selectedToolItem.ConsumeDurability(1);
            InventoryManager.Instance.InventoryModel.UpdateInventory();
        }

        private Vector3 GetDamagePopupSpawnPosition()
        {
            if (InteractionManager.Instance != null &&
                InteractionManager.Instance.CurrentlyHoveredInteractable == this)
            {
                return InteractionManager.Instance.CurrentHoveredInteractableHitPoint;
            }

            Collider resourceCollider = GetComponent<Collider>();
            if (resourceCollider == null)
            {
                resourceCollider = GetComponentInChildren<Collider>();
            }

            if (resourceCollider != null)
            {
                Bounds bounds = resourceCollider.bounds;
                return bounds.center + Vector3.up * (bounds.extents.y + 0.1f);
            }

            return transform.position + Vector3.up * 1f;
        }
    }
}
