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
        private bool _isHoldingHammer;
        private BuildPiece _lastHoveredBuildPiece;
        
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
            else if (_isHoldingHammer && InteractionManager.Instance.CurrentlyHoveredBuildPiece != null)
            {
                ShowBuildPieceInfo(InteractionManager.Instance.CurrentlyHoveredBuildPiece);
            }
            else
            {
                HideInteractableInfo();
            }

            Timer activeTimer = null;

            // Priority order: Destroying > Eating
            Timer destroyTimer = BuildingManager.Instance.DestroyTimer;
            Timer spearTetherTimer = SpearTetherManager.Instance.SpearTetherChargeTimer;
            Timer eatTimer = HungerManager.Instance.EatTimer;
            Timer drinkTimer = ThirstManager.Instance.DrinkTimer;

            if (IsTimerActive(destroyTimer))
            {
                activeTimer = destroyTimer;
            }
            else if (IsTimerActive(spearTetherTimer))
            {
                activeTimer = spearTetherTimer;
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

            HandleRepairUI();
        }

        private void CheckForRepairState(BuildOption type)
        {
            ClearStructReqs();

            if (type == BuildOption.RepairMode)
            {
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

            if (interactable is CookingStation)
            {
                _crosshairImage.sprite = _rawBirdSprite;
                ShowTextAboveCrosshair("[E] <br> Cook / Collect Meat");
                SetCrosshairAlpha(1f);
                return;
            }

            if (interactable is WaterStill waterStill)
            {
                HandleWaterStillText(waterStill);
                SetCrosshairAlpha(1f);
                return;
            }
            
            if(interactable is CraftingTable craftingTable)
            {
                ShowTextAboveCrosshair("[E] <br> Open Crafting Menu");
                SetCrosshairAlpha(1f);
                return;
            }
            
            
            if (interactable is PlanterBox planterBox)
            {
                HandlePlanterBoxText(planterBox);
                SetCrosshairAlpha(1f);
                return;
            }
            
            
            if(interactable is Growable growable)
            {
                HandleGrowableText(growable);
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

        private void HandleGrowableText(Growable growable)
        {
            if(growable.CanBeHarvested)
            {
                ShowTextAboveCrosshair("[E] <br> Harvest");
            }
            else
            {
                ShowTextAboveCrosshair("Growing...");
            }
        }

        private void HandlePlanterBoxText(PlanterBox planterBox)
        {
            switch(planterBox.CurrentState)
            {
                case PlanterBoxState.Empty:
                    ShowTextAboveCrosshair("[E] <br> Plant Cliff Seed");
                    break;
                case PlanterBoxState.Growing:
                    ShowTextAboveCrosshair("Growing...");
                    break;
                case PlanterBoxState.Grown:
                    ShowTextAboveCrosshair("[E] <br> Harvest");
                    break;
            }       
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

        private void ShowBuildPieceInfo(BuildPiece buildPiece)
        {
            _crosshairImage.sprite = _hammerSprite;

            if (buildPiece.IsAnchored)
            {
                ShowTextAboveCrosshair("Stability: 100%");
            }
            else
            {
                int currentDist = buildPiece.DistanceFromAnchor;
                int maxDist = BuildPieceIntegrityManager.Instance.MaxSupportedDistance;
                float stability = Mathf.Clamp01(1.1f - ((float)currentDist / maxDist));
                
                ShowTextAboveCrosshair($"Stability: {stability:P0}");
            }
            SetCrosshairAlpha(1f);
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
                _isHoldingHammer = true;
                if(BuildingManager.Instance.CurrentBuildType == BuildOption.DestroyMode)
                {
                    ClearStructReqs();
                }
                else if(BuildingManager.Instance.CurrentBuildType == BuildOption.RepairMode)
                {
                    ClearStructReqs();

                    _repairInstructions.SetActive(true);
                }
                else if(BuildingManager.Instance.CurrentBuildType == BuildOption.Fence || BuildingManager.Instance.CurrentBuildType == BuildOption.Platform)
                {
                    PopulateBuildReqs();
                }
            }
            else
            {
                _isHoldingHammer = false;
                ClearStructReqs();

                HideBuildInstructionTexts();
            }
        }

        private void PopulateBuildReqs()
        {
            foreach (InventoryItem item in BuildingManager.Instance.GetCurrentBuild().ItemsNeededForBuilding)
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

        private void HandleRepairUI()
        {
            if (_isHoldingHammer && BuildingManager.Instance.CurrentBuildType == BuildOption.RepairMode)
            {
                BuildPiece hovered = InteractionManager.Instance.CurrentlyHoveredBuildPiece;
                if (hovered != _lastHoveredBuildPiece)
                {
                    _lastHoveredBuildPiece = hovered;
                    UpdateRepairRequirements(hovered);
                }
            }
            else
            {
                if (_lastHoveredBuildPiece != null)
                {
                    ClearStructReqs();
                    _lastHoveredBuildPiece = null;
                }
            }
        }

        private void UpdateRepairRequirements(BuildPiece piece)
        {
            ClearStructReqs();

            if (piece != null)
            {
                foreach (InventoryItem item in piece.ItemsNeededForRepairing)
                {
                    StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform.position, Quaternion.identity);
                    structReq.transform.SetParent(_structReqHolder.transform);
                    structReq.Initialize(item);
                }
            }
        }

        private void ClearStructReqs()
        {
            if (_structReqHolder.transform.childCount > 0)
            {
                for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(_structReqHolder.transform.GetChild(i).gameObject);
                }
            }
        }
    }
}
