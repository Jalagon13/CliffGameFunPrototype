using UnityEngine;
using FMOD.Studio;
using System;

namespace CliffGame
{
    public class CookingStation : Resource
    {
        [Header("Cooking Station Settings")]
        [SerializeField] private ConsumableItemSO _foodItemToCook;
        [SerializeField] private ConsumableItemSO _foodItemToPickup;
        [SerializeField] private float _cookingDuration = 3f;
        [SerializeField] private GameObject _idleModel;
        [SerializeField] private GameObject _cookingModel;
        [SerializeField] private GameObject _readyToPickupModel;

        public GameObject GameObject => gameObject;

        private Timer _cookingTimer;

        private bool _isCooking => _cookingTimer != null;
        public bool IsCooking => _isCooking;

        private bool _hasFoodToPickup;
        public bool HasFoodToPickUp => _hasFoodToPickup;

        private EventInstance _cookingEventInstance;

        private void Start()
        {
            ChangeToIdleModel();
        }

        private void Update()
        {
            if (_cookingTimer != null)
            {
                _cookingTimer.Tick(Time.deltaTime);
            }
        }

        public override void OnInteractWith()
        {
            base.OnInteractWith();

            if (InventoryManager.Instance.HasSelectedItem)
            {
                if (InventoryManager.Instance.SelectedInventoryItem.Item == _foodItemToCook)
                {
                    if (!_isCooking)
                    {
                        if (!_hasFoodToPickup)
                        {
                            StartCooking();
                        }
                        else
                        {
                            TryToCollectCookedMeat();
                        }
                    }
                }
                else
                {
                    // Has inventory item just not sparrow
                    TryToCollectCookedMeat();
                }
            }
            else
            {
                TryToCollectCookedMeat();
            }
        }

        private void StartCooking()
        {
            _cookingTimer = new(_cookingDuration);
            _cookingTimer.OnTimerEnd += OnCookingFinished;
            _hasFoodToPickup = false;

            InventoryManager.Instance.RemoveItem(_foodItemToCook, 1);

            ChangeToCookingModel();
        }

        private void OnCookingFinished(object sender, EventArgs e)
        {
            _cookingTimer.OnTimerEnd -= OnCookingFinished;
            _cookingTimer = null;

            _hasFoodToPickup = true;

            ChangeToReadyToPickupModel();
        }

        private void TryToCollectCookedMeat()
        {
            if (!_isCooking && _hasFoodToPickup)
            {
                InventoryManager.Instance.AddItem(_foodItemToPickup, 1);
                _hasFoodToPickup = false;

                ChangeToIdleModel();
            }
        }

        private void ChangeToIdleModel()
        {
            Debug.Log($"Changing model to idle");
            _idleModel.SetActive(true);
            _cookingModel.SetActive(false);
            _readyToPickupModel.SetActive(false);
        }

        private void ChangeToCookingModel()
        {
            Debug.Log($"Changing model to cooking");
            _idleModel.SetActive(false);
            _cookingModel.SetActive(true);
            _readyToPickupModel.SetActive(false);

            _cookingEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.CampfireCooking);
            _cookingEventInstance.start();
        }

        private void ChangeToReadyToPickupModel()
        {
            Debug.Log($"Changing model to ready to pickup");
            _idleModel.SetActive(false);
            _cookingModel.SetActive(false);
            _readyToPickupModel.SetActive(true);

            _cookingEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
