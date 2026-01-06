using UnityEngine;

namespace CliffGame
{
    public enum PlanterBoxState
    {
        Empty,
        Growing,
        Grown
    }

    public class PlanterBox : Resource
    {
        private PlantableItemSO _currentPlantable;
        private PlanterBoxState _currentState = PlanterBoxState.Empty;

        private Timer _growthTimer;

        private void Update()
        {
            if (_currentState == PlanterBoxState.Growing && _growthTimer != null)
            {
                _growthTimer.Tick(Time.deltaTime);
            }
        }

        public override void OnInteractWith()
        {
            base.OnInteractWith();

            switch (_currentState)
            {
                case PlanterBoxState.Growing:
                    CancelGrowth();
                    return;

                case PlanterBoxState.Empty:
                    TryPlantSelectedItem();
                    return;

                case PlanterBoxState.Grown:
                    // Reserved for harvest interaction later
                    return;
            }
        }

        public override void OnHitWithTool()
        {
            if (_currentState == PlanterBoxState.Empty) // Only execute hit if it is currently not in use
            {
                base.OnHitWithTool();
            }
        }

        private void TryPlantSelectedItem()
        {
            if (!InventoryManager.Instance.HasSelectedItem) return;

            InventoryItem selectedItem = InventoryManager.Instance.SelectedInventoryItem;

            if (selectedItem.Item is PlantableItemSO plantableItem)
            {
                StartGrowing(plantableItem);
                InventoryManager.Instance.RemoveItem(plantableItem, 1);
            }
        }

        private void StartGrowing(PlantableItemSO plantable)
        {
            _currentPlantable = plantable;
            _currentState = PlanterBoxState.Growing;

            _growthTimer = new Timer(plantable.GrowthTimeInSeconds);
            _growthTimer.OnTimerEnd += OnGrowthComplete;

            OnGrowthStarted();
        }

        private void OnGrowthComplete(object sender, System.EventArgs e)
        {
            _growthTimer.OnTimerEnd -= OnGrowthComplete;
            _currentState = PlanterBoxState.Grown;

            OnGrowthFinished();
        }

        private void CancelGrowth()
        {
            if (_currentPlantable != null)
            {
                InventoryManager.Instance.AddItem(_currentPlantable, 1);
            }

            _growthTimer = null;
            _currentPlantable = null;
            _currentState = PlanterBoxState.Empty;

            OnGrowthCancelled();
        }

        public void ClearPlanterBox()
        {
            _growthTimer = null;
            _currentPlantable = null;
            _currentState = PlanterBoxState.Empty;

            OnPlanterCleared();
        }

        // ----- Extension Hooks -----

        protected virtual void OnGrowthStarted()
        {
            // Add visuals, sounds, UI updates here
        }

        protected virtual void OnGrowthFinished()
        {
            // Spawn plant prefab, rewards, etc.
        }

        protected virtual void OnGrowthCancelled()
        {
            // Handle cancellation effects here
        }

        protected virtual void OnPlanterCleared()
        {
            // Logic for when planter is reset
        }
    }
}
