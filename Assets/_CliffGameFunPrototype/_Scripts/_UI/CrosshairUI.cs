using System;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _interactRadialBar;
        [SerializeField] private GameObject _structReqHolder;
        [SerializeField] private StructReqUI _structReqPrefab;
        [SerializeField] private Sprite _defaultSprite, _axeSprite, _hammerSprite, _spearSprite, _rawBirdSprite;
        [SerializeField] private GameObject _buildInstructionTextHolder;
        [SerializeField] private GameObject _repairInstructions;
        [SerializeField] private GameObject _campfireInstructions;

        private Image _crosshairImage;
        
        private void Awake()
        {
            _crosshairImage = transform.GetChild(0).GetComponent<Image>();
            HideInteractableInfo();
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHammer;
            BuildingManager.Instance.OnBuildTypeChanged += CheckForRepairState;

            _interactRadialBar.gameObject.SetActive(false); // TEMP. Delete this later
            _repairInstructions.SetActive(false);
            _campfireInstructions.SetActive(false);
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForHammer;
            BuildingManager.Instance.OnBuildTypeChanged -= CheckForRepairState;
        }

        private void Update()
        {
            if(InteractionManager.Instance.CurrentlyHoveredInteractable != null)
            {
                ShowInteractableInfo();
            }
            else
            {
                HideInteractableInfo();
            }

            Timer destroyTimer = BuildingManager.Instance.DestroyTimer;


            // NEXT: FIgure out how to access the interact radial bar

            // Timer is considered active once it has started ticking (not full duration, not zero)
            bool destroyTimerActive =
                destroyTimer != null &&
                destroyTimer.RemainingSeconds < destroyTimer.Duration &&
                destroyTimer.RemainingSeconds > 0f;

            if (destroyTimerActive)
            {
                // PercentRemaining already goes 0 → 1 over the timer duration
                _interactRadialBar.UpdateBar(destroyTimer.PercentRemaining, 0f, 1f);
                _interactRadialBar.gameObject.SetActive(true);
            }
            else
            {
                // Ensure clean reset when not destroying
                _interactRadialBar.UpdateBar(0f, 0f, 1f);
                _interactRadialBar.gameObject.SetActive(false);
            }
        }

        private void CheckForRepairState(SelectedBuildType type)
        {
            if (_structReqHolder.transform.childCount > 0)
            {
                for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                }
            }

            if (type == SelectedBuildType.RepairMode)
            {
                foreach (InventoryItem item in BuildingManager.Instance.ItemsNeededForRepairing)
                {
                    StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform.position, Quaternion.identity);
                    structReq.transform.SetParent(_structReqHolder.transform);

                    structReq.Initialize(item);
                }

                _repairInstructions.SetActive(true);
            }
            else if(type == SelectedBuildType.Wall || type == SelectedBuildType.Floor)
            {
                PopulateBuildReqs();
                _repairInstructions.SetActive(false);
            }
        }

        private void ShowInteractableInfo()
        {
            IInteractable interactable = InteractionManager.Instance.CurrentlyHoveredInteractable;

            if(interactable is CookingStation)
            {
                _crosshairImage.sprite = _rawBirdSprite;
                _campfireInstructions.SetActive(true);

                // Set full alpha
                Color co = _crosshairImage.color;
                co.a = 1f;
                _crosshairImage.color = co;
                return;
            }
            else
            {
                switch (interactable.BreakToolType)
                {
                    case ToolType.Axe:
                        _crosshairImage.sprite = _axeSprite;
                        break;
                    case ToolType.Hammer:
                        _crosshairImage.sprite = _hammerSprite;
                        break;
                    case ToolType.Spear:
                        _crosshairImage.sprite = _spearSprite;
                        break;
                    default:
                        _crosshairImage.sprite = _defaultSprite;
                        break;
                }
            }

            _campfireInstructions.SetActive(false);

            // Set full alpha
            Color c = _crosshairImage.color;
            c.a = 1f;
            _crosshairImage.color = c;
        }
        
        private void HideInteractableInfo()
        {
            _crosshairImage.sprite = _defaultSprite;
            _campfireInstructions.SetActive(false);

            Color c = _crosshairImage.color;
            c.a = 0.25f;
            _crosshairImage.color = c;
        }

        private void CheckForHammer(int arg1, InventoryItem item)
        {
            if(item.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer) 
            {
                if(BuildingManager.Instance.CurrentBuildType == SelectedBuildType.DestroyMode)
                {
                    if (_structReqHolder.transform.childCount > 0)
                    {
                        for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                        {
                            Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                        }
                    }
                }
                else if(BuildingManager.Instance.CurrentBuildType == SelectedBuildType.RepairMode)
                {
                    if (_structReqHolder.transform.childCount > 0)
                    {
                        for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                        {
                            Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                        }
                    }

                    foreach (InventoryItem item1 in BuildingManager.Instance.ItemsNeededForRepairing)
                    {
                        StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform.position, Quaternion.identity);
                        structReq.transform.SetParent(_structReqHolder.transform);

                        structReq.Initialize(item1);
                    }

                    _repairInstructions.SetActive(true);
                }
                else if(BuildingManager.Instance.CurrentBuildType == SelectedBuildType.Wall || BuildingManager.Instance.CurrentBuildType == SelectedBuildType.Floor)
                {
                    PopulateBuildReqs();
                }
            }
            else
            {
                for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                }

                HideBuildInstructionTexts();
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

            ShowBuildInstructionTexts();
        }
        
        private void ShowBuildInstructionTexts()
        {
            _buildInstructionTextHolder.SetActive(true);
        }
        
        private void HideBuildInstructionTexts()
        {
            _buildInstructionTextHolder.SetActive(false);
        }
    }
}