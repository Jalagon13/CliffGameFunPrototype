using System;
using MoreMountains.Tools;
using UnityEngine;

namespace CliffGame
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _interactRadialBar;
        [SerializeField] private GameObject _structReqHolder;
        [SerializeField] private StructReqUI _structReqPrefab;

        private bool _wasHarvesting = false; // Track previous state
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHammer;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForHammer;
        }

        private void Update()
        {
            bool isHarvesting = InteractionManager.Instance.IsHarvesting;

            if (isHarvesting)
            {
                if (!_wasHarvesting)
                {
                    OnHarvestStarted();
                }

                _interactRadialBar.gameObject.SetActive(true);
                _interactRadialBar.UpdateBar(InteractionManager.Instance.HarvestTimer.PercentRemaining, 0, 1);
            }
            else
            {
                _interactRadialBar.gameObject.SetActive(false);
            }

            _wasHarvesting = isHarvesting; // Update previous state
        }

        private void OnHarvestStarted()
        {
            _interactRadialBar.UpdateBar(0, 0, 1);
        }


        private void CheckForHammer(int arg1, InventoryItem item)
        {
            if(item.Item is HammerItemSO hammer) 
            {
                PopulateBuildReqs();        
            }
            else
            {
                for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                }
            }
        }

        private void PopulateBuildReqs()
        {
            foreach (InventoryItem item in BuildingManager.Instance.ItemsNeededForBuilding)
            {
                StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform.position, Quaternion.identity);
                structReq.transform.SetParent(_structReqHolder.transform);
                
                structReq.Initialize(item);
            }
        }
    }
}