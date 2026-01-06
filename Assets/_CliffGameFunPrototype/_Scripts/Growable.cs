using UnityEngine;

namespace CliffGame
{
    public class Growable : Resource
    {
        private float _growthPercentage; // 0 to 1
        private bool _canBeHit;
        private PlanterBox _parentPlanterBox;

        protected override void Awake()
        {
            base.Awake();
        
            SetVisualScale(0f);
        }
        
        private void OnDestroy()
        {
            _parentPlanterBox?.ClearPlanterBox();
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
                _canBeHit = true;
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
            if (!_canBeHit) return;

            base.OnHitWithTool();
        }

        protected virtual void OnFullyGrown()
        {
            // Hook for visuals, VFX, sounds, etc when fully grown
        }
    }
}
