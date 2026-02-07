using System;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CliffGame
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _interactRadialBar;
        [SerializeField] private GameObject _structReqHolder;
        [SerializeField] private StructReqUI _structReqPrefab;
        [SerializeField] private Sprite _defaultSprite, _axeSprite, _hammerSprite, _spearSprite, _rawBirdSprite, _fiberSprite;
        [SerializeField] private GameObject _buildInstructionTextHolder;
        [SerializeField] private GameObject _repairInstructions;
        [SerializeField] private GameObject _textAboveCrosshair;
        [SerializeField] private GameObject _stairsChangeDirectionText;
        [SerializeField] private TMP_Text _textAboveCrosshairLabel;

        private Image _crosshairImage;
        
        private void Awake()
        {
            _crosshairImage = transform.GetChild(0).GetComponent<Image>();
            HideInteractableInfo();
            HideTextAboveCrosshair();
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHammer;
            BuildingManager.Instance.OnBuildTypeChanged += CheckForRepairState;

            _interactRadialBar.gameObject.SetActive(false);
            _repairInstructions.SetActive(false);
            _stairsChangeDirectionText.SetActive(false);
            HideTextAboveCrosshair();
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

            Timer activeTimer = null;

            // Priority order: Destroying > Eating
            Timer destroyTimer = BuildingManager.Instance.DestroyTimer;
            Timer eatTimer = HungerManager.Instance.EatTimer;
            Timer drinkTimer = ThirstManager.Instance.DrinkTimer;

            if (IsTimerActive(destroyTimer))
            {
                activeTimer = destroyTimer;
            }
            else if (IsTimerActive(eatTimer))
            {
                activeTimer = eatTimer;
            }
            else if (IsTimerActive(drinkTimer))
            {
                activeTimer = drinkTimer;
            }

            if (activeTimer != null)
            {
                _interactRadialBar.UpdateBar(activeTimer.PercentRemaining, 0f, 1f);
                _interactRadialBar.gameObject.SetActive(true);
            }
            else
            {
                _interactRadialBar.UpdateBar(0f, 0f, 1f);
                _interactRadialBar.gameObject.SetActive(false);
            }
        }

        private void CheckForRepairState(BuildOption type)
        {
            if (_structReqHolder.transform.childCount > 0)
            {
                for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                }
            }

            if (type == BuildOption.RepairMode)
            {
                foreach (InventoryItem item in BuildingManager.Instance.ItemsNeededForRepairing)
                {
                    StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform.position, Quaternion.identity);
                    structReq.transform.SetParent(_structReqHolder.transform);

                    structReq.Initialize(item);
                }

                _repairInstructions.SetActive(true);
                _stairsChangeDirectionText.SetActive(false);
            }
            else if(type == BuildOption.Fence || type == BuildOption.Platform)
            {
                PopulateBuildReqs();
                _repairInstructions.SetActive(false);
                _stairsChangeDirectionText.SetActive(false);
            }
            else if(type == BuildOption.Stairs)
            {
                PopulateBuildReqs();
                _repairInstructions.SetActive(false);

                _stairsChangeDirectionText.SetActive(true);
            }
        }

        private void ShowInteractableInfo()
        {
            IInteractable interactable = InteractionManager.Instance.CurrentlyHoveredInteractable;

            // Default visuals
            _crosshairImage.sprite = _defaultSprite;
            HideTextAboveCrosshair();

            // ---- Cooking Station ----
            if (interactable is CookingStation)
            {
                _crosshairImage.sprite = _rawBirdSprite;
                ShowTextAboveCrosshair("[E] <br> Cook / Collect Meat");
                SetCrosshairAlpha(1f);
                return;
            }

            // ---- Water Still ----
            if (interactable is WaterStill waterStill)
            {
                
                HandleWaterStillText(waterStill);
                SetCrosshairAlpha(1f);
                return;
            }

            // ---- Tool-based interactables ----
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

            SetCrosshairAlpha(1f);
        }
        
        private void HandleWaterStillText(WaterStill waterStill)
        {
            switch (waterStill.CurrentState)
            {
                case WaterStill.WaterStillState.Idle:
                case WaterStill.WaterStillState.CollectingFiber:
                    _crosshairImage.sprite = _fiberSprite;
                    ShowTextAboveCrosshair(
                        $"[E] <br> Insert Fiber ({waterStill.CurrentFiberStorage}/{waterStill.FiberNeededPerWaterUnit})"
                    );
                    break;

                case WaterStill.WaterStillState.ProcessingWater:
                    _crosshairImage.sprite = _defaultSprite;
                    ShowTextAboveCrosshair("Processing Water...");
                    break;

                case WaterStill.WaterStillState.WaterReady:
                    _crosshairImage.sprite = _defaultSprite;
                    ShowTextAboveCrosshair("[E] <br> Drink Water");
                    break;
            }
        }

        private void HideInteractableInfo()
        {
            _crosshairImage.sprite = _defaultSprite;
            HideTextAboveCrosshair();
            SetCrosshairAlpha(0.25f);
        }

        private void CheckForHammer(int arg1, InventoryItem item)
        {
            if(item.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer) 
            {
                if(BuildingManager.Instance.CurrentBuildType == BuildOption.DestroyMode)
                {
                    if (_structReqHolder.transform.childCount > 0)
                    {
                        for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                        {
                            Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                        }
                    }
                }
                else if(BuildingManager.Instance.CurrentBuildType == BuildOption.RepairMode)
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
                else if(BuildingManager.Instance.CurrentBuildType == BuildOption.Fence || BuildingManager.Instance.CurrentBuildType == BuildOption.Platform)
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

        private void ShowTextAboveCrosshair(string text)
        {
            _textAboveCrosshair.SetActive(true);
            _textAboveCrosshairLabel.text = text;
        }

        private void HideTextAboveCrosshair()
        {
            _textAboveCrosshair.SetActive(false);
            _textAboveCrosshairLabel.text = string.Empty;
        }

        private void SetCrosshairAlpha(float alpha)
        {
            Color c = _crosshairImage.color;
            c.a = alpha;
            _crosshairImage.color = c;
        }

        private bool IsTimerActive(Timer timer)
        {
            if (timer == null) return false;

            return timer.RemainingSeconds < timer.Duration &&
                   timer.RemainingSeconds > 0f;
        }
    }
}