using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _interactRadialBar;
        [SerializeField] private GameObject _structReqHolder;
        [SerializeField] private StructReqUI _structReqPrefab;
        [SerializeField] private Sprite _defaultSprite, _axeSprite, _hammerSprite, _spearSprite, _rawBirdSprite, _combatSprite;
        [SerializeField] private GameObject _buildInstructionTextHolder;
        [SerializeField] private GameObject _repairInstructions;
        [SerializeField] private Material _repairHoverMaterial;
        [SerializeField] private GameObject _textAboveCrosshair;
        [SerializeField] private GameObject _stairsChangeDirectionText;
        [SerializeField] private TMP_Text _textAboveCrosshairLabel;

        private Image _crosshairImage;
        private bool _isHoldingHammer;
        private BuildPiece _lastHoveredBuildPiece;
        private BuildPiece _lastHighlightedBuildPiece;
        private readonly List<Material[]> _lastHighlightOriginalMaterials = new();
        private string _lastRequirementContextId = string.Empty;

        private void Awake()
        {
            _crosshairImage = transform.GetChild(0).GetComponent<Image>();
            HideInteractableInfo();
            HideTextAboveCrosshair();
        }

        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHammer;
            BuildingManager.Instance.OnBuildTypeChanged += HandleBuildTypeChanged;

            _interactRadialBar.gameObject.SetActive(false);
            _repairInstructions.SetActive(false);
            _stairsChangeDirectionText.SetActive(false);
            HideTextAboveCrosshair();
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForHammer;
            BuildingManager.Instance.OnBuildTypeChanged -= HandleBuildTypeChanged;
            RestoreRepairHoverHighlight();
        }

        private void Update()
        {
            if (InteractionManager.Instance.CurrentlyHoveredInteractable != null)
            {
                ShowInteractableInfo();
            }
            else if (_isHoldingHammer && BuildingManager.Instance.CurrentBuildType == BuildOption.RepairMode && InteractionManager.Instance.CurrentlyHoveredBuildPiece != null)
            {
                ShowBuildPieceInfo(InteractionManager.Instance.CurrentlyHoveredBuildPiece);
            }
            else
            {
                HideInteractableInfo();
            }

            UpdateRadialProgressBar();
            UpdateRequirementDisplay();
        }

        private void UpdateRadialProgressBar()
        {
            Timer activeTimer = null;
            Timer destroyTimer = BuildingManager.Instance.DestroyTimer;
            Timer repairTimer = Grindstone.ActiveRepairTimer;
            Timer spearTetherTimer = SpearTetherManager.Instance.SpearTetherChargeTimer;
            Timer eatTimer = HungerManager.Instance.EatTimer;

            if (IsTimerActive(destroyTimer))
            {
                activeTimer = destroyTimer;
            }
            else if (IsTimerActive(repairTimer))
            {
                activeTimer = repairTimer;
            }
            else if (IsTimerActive(spearTetherTimer))
            {
                activeTimer = spearTetherTimer;
            }
            else if (IsTimerActive(eatTimer))
            {
                activeTimer = eatTimer;
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

        private void UpdateRequirementDisplay()
        {
            if (InteractionManager.Instance.CurrentlyHoveredInteractable is Grindstone sharpeningStone)
            {
                RestoreRepairHoverHighlight();
                SetRequirementDisplay(sharpeningStone.GetRequirementContextId(), sharpeningStone.GetRepairRequirementsForSelectedTool(), false, false, false);
                return;
            }

            if (_isHoldingHammer && BuildingManager.Instance.CurrentBuildType == BuildOption.RepairMode)
            {
                BuildPiece hoveredPiece = InteractionManager.Instance.CurrentlyHoveredBuildPiece;
                _lastHoveredBuildPiece = hoveredPiece;
                UpdateRepairHoverHighlight(hoveredPiece);
                SetRequirementDisplay(
                    hoveredPiece != null ? $"buildrepair_{hoveredPiece.GetInstanceID()}" : "buildrepair_none",
                    hoveredPiece != null ? hoveredPiece.ItemsNeededForRepairing : null,
                    false,
                    true,
                    false);
                return;
            }

            RestoreRepairHoverHighlight();
            _lastHoveredBuildPiece = null;

            if (_isHoldingHammer && IsBuildPlacementMode())
            {
                BuildPiece currentBuild = BuildingManager.Instance.GetCurrentBuild();
                SetRequirementDisplay(
                    currentBuild != null ? $"build_{BuildingManager.Instance.CurrentBuildType}_{currentBuild.GetInstanceID()}" : "build_none",
                    currentBuild != null ? currentBuild.ItemsNeededForBuilding : null,
                    true,
                    false,
                    BuildingManager.Instance.CurrentBuildType == BuildOption.Stairs);
                return;
            }

            SetRequirementDisplay("none", null, false, false, false);
        }

        private bool IsBuildPlacementMode()
        {
            BuildOption buildType = BuildingManager.Instance.CurrentBuildType;
            return buildType == BuildOption.Platform || buildType == BuildOption.Fence || buildType == BuildOption.Stairs;
        }

        private void SetRequirementDisplay(string contextId, InventoryItem[] requirements, bool showBuildInstructions, bool showRepairInstructions, bool showStairsText)
        {
            _buildInstructionTextHolder.SetActive(showBuildInstructions);
            _repairInstructions.SetActive(showRepairInstructions);
            _stairsChangeDirectionText.SetActive(showStairsText);

            bool hasRequirements = requirements != null && requirements.Length > 0;
            if (!hasRequirements)
            {
                if (_lastRequirementContextId != string.Empty)
                {
                    ClearStructReqs();
                    _lastRequirementContextId = string.Empty;
                }
                return;
            }

            if (_lastRequirementContextId == contextId)
            {
                return;
            }

            ClearStructReqs();
            _lastRequirementContextId = contextId;

            foreach (InventoryItem item in requirements)
            {
                StructReqUI structReq = Instantiate(_structReqPrefab, _structReqHolder.transform);
                structReq.Initialize(item);
            }
        }

        private void HandleBuildTypeChanged(BuildOption type)
        {
            _lastRequirementContextId = string.Empty;

            if (type != BuildOption.RepairMode)
            {
                RestoreRepairHoverHighlight();
            }
        }

        private void ShowInteractableInfo()
        {
            IInteractable interactable = InteractionManager.Instance.CurrentlyHoveredInteractable;
            _crosshairImage.sprite = _defaultSprite;
            HideTextAboveCrosshair();

            if (interactable is Npc)
            {
                _crosshairImage.sprite = _combatSprite;
                SetCrosshairAlpha(1f);
                return;
            }

            if (interactable is CookingStation)
            {
                _crosshairImage.sprite = _rawBirdSprite;
                ShowTextAboveCrosshair("[E] <br> Cook / Collect Meat");
                SetCrosshairAlpha(1f);
                return;
            }

            if (interactable is Grindstone sharpeningStone)
            {
                _crosshairImage.sprite = _defaultSprite;
                ShowTextAboveCrosshair(sharpeningStone.GetCrosshairPrompt());
                SetCrosshairAlpha(1f);
                return;
            }

            if (interactable is CraftingTable)
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

        private void HandlePlanterBoxText(PlanterBox planterBox)
        {
            switch (planterBox.CurrentState)
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

        private void ShowBuildPieceInfo(BuildPiece buildPiece)
        {
            if (BuildingManager.Instance.CurrentBuildType != BuildOption.RepairMode)
            {
                HideTextAboveCrosshair();
                return;
            }

            _crosshairImage.sprite = _hammerSprite;
            BuildPieceDurability durability = buildPiece.GetComponent<BuildPieceDurability>();
            ShowTextAboveCrosshair(durability != null
                ? $"HP: {durability.CurrentHitPoints}/{durability.MaxHitPoints}"
                : "HP: N/A");
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
            _isHoldingHammer = item != null && item.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer;
            _lastRequirementContextId = string.Empty;

            if (!_isHoldingHammer)
            {
                ClearStructReqs();
                _buildInstructionTextHolder.SetActive(false);
                _repairInstructions.SetActive(false);
                _stairsChangeDirectionText.SetActive(false);
            }
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
            Color color = _crosshairImage.color;
            color.a = alpha;
            _crosshairImage.color = color;
        }

        private bool IsTimerActive(Timer timer)
        {
            if (timer == null) return false;
            return timer.RemainingSeconds < timer.Duration && timer.RemainingSeconds > 0f;
        }

        private void ClearStructReqs()
        {
            for (int i = _structReqHolder.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_structReqHolder.transform.GetChild(i).gameObject);
            }
        }

        private void UpdateRepairHoverHighlight(BuildPiece hoveredPiece)
        {
            if (_repairHoverMaterial == null)
            {
                RestoreRepairHoverHighlight();
                return;
            }

            if (hoveredPiece == _lastHighlightedBuildPiece)
            {
                return;
            }

            RestoreRepairHoverHighlight();

            if (hoveredPiece == null)
            {
                return;
            }

            MeshRenderer[] renderers = GetBuildPieceRenderers(hoveredPiece);
            if (renderers.Length == 0)
            {
                return;
            }

            _lastHighlightOriginalMaterials.Clear();

            foreach (MeshRenderer renderer in renderers)
            {
                Material[] originalMaterials = renderer.materials;
                _lastHighlightOriginalMaterials.Add(originalMaterials);

                Material[] highlightMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightMaterials.Length; i++)
                {
                    highlightMaterials[i] = _repairHoverMaterial;
                }

                renderer.materials = highlightMaterials;
            }

            _lastHighlightedBuildPiece = hoveredPiece;
        }

        private void RestoreRepairHoverHighlight()
        {
            if (_lastHighlightedBuildPiece == null)
            {
                return;
            }

            MeshRenderer[] renderers = GetBuildPieceRenderers(_lastHighlightedBuildPiece);
            int count = Mathf.Min(renderers.Length, _lastHighlightOriginalMaterials.Count);
            for (int i = 0; i < count; i++)
            {
                renderers[i].materials = _lastHighlightOriginalMaterials[i];
            }

            _lastHighlightOriginalMaterials.Clear();
            _lastHighlightedBuildPiece = null;
        }

        private MeshRenderer[] GetBuildPieceRenderers(BuildPiece piece)
        {
            if (piece == null)
            {
                return Array.Empty<MeshRenderer>();
            }

            Transform modelRoot = piece.transform.childCount > 0 ? piece.transform.GetChild(0) : piece.transform;
            return modelRoot.GetComponentsInChildren<MeshRenderer>(true);
        }
    }
}
