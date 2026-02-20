using UnityEngine;

namespace CliffGame
{
    public class Growable : Resource
    {
        private float _growthPercentage; // 0 to 1
        private bool _canBeHarvested;
        private PlanterBox _parentPlanterBox;
        private Collider _growableCollider;

        protected override void Awake()
        {
            base.Awake();

            _growableCollider = GetComponent<Collider>();
            _growableCollider.enabled = false;

            SetVisualScale(0f);
        }
        
        private void OnDestroy()
        {
            _parentPlanterBox?.ClearPlanterBox();
        }

        public override void OnInteractWith()
        {
            if(_canBeHarvested)
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
                Debug.Log($"Harvested resource: {gameObject.name}");
                Destroy(gameObject);
            }
        }


        public void InitializeGrowable(PlanterBox planterBox)
        {
            _parentPlanterBox = planterBox;
        }

        public void UpdateGrowthPercentage(float percentage)
        {
            _growthPercentage = Mathf.Clamp01(percentage);

            SetVisualScale(_growthPercentage);

            if (_growthPercentage >= 1f)
            {
                _canBeHarvested = true;
                OnFullyGrown();
            }
        }

        private void SetVisualScale(float percentage)
        {
            float scale = Mathf.Lerp(0.1f, 1f, percentage);
            transform.localScale = Vector3.one * scale;
        }

        public override void OnHitWithTool()
        {
            
        }

        protected virtual void OnFullyGrown()
        {
            // Hook for visuals, VFX, sounds, etc when fully grown
            _growableCollider.enabled = true;

        }
    }
}
