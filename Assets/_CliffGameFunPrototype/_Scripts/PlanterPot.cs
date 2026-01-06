using System;
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
        [SerializeField] private Transform _growableGrowPoint;
    
        private PlantableItemSO _currentPlantableItem;
        private PlanterBoxState _currentState = PlanterBoxState.Empty;
        public PlanterBoxState CurrentState => _currentState;

        private Timer _growthTimer;
        private Growable _currentGrowableInstance;

        private void Update()
        {
            if (_currentState == PlanterBoxState.Growing && _growthTimer != null)
            {
                OnGrowthTick();
            }
        }

        public override void OnInteractWith()
        {
            base.OnInteractWith();

            switch (_currentState)
            {
                case PlanterBoxState.Growing:

                    if (InventoryManager.Instance.HasSelectedItem && InventoryManager.Instance.SelectedInventoryItem.Item is PlantGrowthItemSO plantGrowthItem && _currentState == PlanterBoxState.Growing)
                    {
                        _growthTimer.SubtractTime(plantGrowthItem.TimeToSubtractFromGrowthTimer);
                        InventoryManager.Instance.RemoveItem(plantGrowthItem, 1);
                    }
                    else
                    {
                        CancelGrowth();
                    }
                    return;

                case PlanterBoxState.Empty:
                    TryPlantSelectedItem();
                    return;

                case PlanterBoxState.Grown:
                    // Reserved for harvest interaction later
                    return;
            }
        }
        
        public void SubtractTime(int seconds)
        {
            _growthTimer.SubtractTime(seconds);
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
            _currentPlantableItem = plantable;
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
            if (_currentPlantableItem != null)
            {
                InventoryManager.Instance.AddItem(_currentPlantableItem, 1);
            }

            _growthTimer = null;
            _currentPlantableItem = null;
            _currentState = PlanterBoxState.Empty;

            OnGrowthCancelled();
        }

        public void ClearPlanterBox()
        {
            _growthTimer = null;
            _currentPlantableItem = null;
            _currentState = PlanterBoxState.Empty;
            _currentGrowableInstance = null;

            OnPlanterCleared();
        }

        protected virtual void OnGrowthStarted()
        {
            // Add visuals, sounds, UI updates here
            _currentGrowableInstance = Instantiate(_currentPlantableItem.GrowablePrefab, _growableGrowPoint.position, Quaternion.identity);
            _currentGrowableInstance.InitializeGrowable(this);
        }

        protected virtual void OnGrowthTick()
        {
            // Update growth visuals, UI progress bars, etc.
            _growthTimer.Tick(Time.deltaTime);
            _currentGrowableInstance.UpdateGrowthPercentage(_growthTimer.GetPercentComplete());
        }

        protected virtual void OnGrowthFinished()
        {
            // Spawn plant prefab, rewards, etc.
            _currentGrowableInstance.UpdateGrowthPercentage(1f);
        }

        protected virtual void OnGrowthCancelled()
        {
            // Handle cancellation effects here
            Destroy(_currentGrowableInstance.gameObject);
        }

        protected virtual void OnPlanterCleared()
        {
            // Logic for when planter is reset
        }
    }
}
