using System;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class Grindstone : Placeable
    {
        public static Grindstone ActiveSharpeningStone { get; private set; }
        public static bool IsAnyRepairInProgress => ActiveSharpeningStone != null && ActiveSharpeningStone._isRepairing;
        public static Timer ActiveRepairTimer => ActiveSharpeningStone?._repairTimer;

        [Header("Sharpening Stone")]
        [SerializeField] private string _selectToolPrompt = "Select a tool to repair";
        [SerializeField] private string _toolFullyRepairedPrompt = "Tool is already fully repaired";
        [SerializeField] private string _missingRepairMaterialsPrompt = "Missing repair materials";
        [SerializeField] private string _noRepairRecipePrompt = "This tool cannot be repaired here";
        [SerializeField] private string _holdToRepairPrompt = "[Hold RMB] <br> Repair Tool";
        [SerializeField] private string _repairingPrompt = "Repairing...";

        private Timer _repairTimer;
        private InventoryItem _repairingToolInventoryItem;
        private ToolItemSO _repairingToolItem;
        private bool _isRepairing;
        private EventInstance _repairLoopEventInstance;

        private void Start()
        {
            GameInput.Instance.OnSecondaryInteract += TryStartRepair;
        }

        private void OnDestroy()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.OnSecondaryInteract -= TryStartRepair;
            }

            StopRepairLoopSFX();

            if (ActiveSharpeningStone == this)
            {
                ActiveSharpeningStone = null;
            }
        }

        private void Update()
        {
            if (!_isRepairing || _repairTimer == null)
            {
                return;
            }

            if (ShouldCancelRepair())
            {
                CancelRepair();
                return;
            }

            _repairTimer.Tick(Time.deltaTime);
        }

        public override void OnInteractWith()
        {
        }

        public static bool HoveredSharpeningStoneWantsSecondaryInteract()
        {
            if (IsAnyRepairInProgress)
            {
                return true;
            }

            if (InteractionManager.Instance == null || InteractionManager.Instance.CurrentlyHoveredInteractable is not Grindstone)
            {
                return false;
            }

            return InventoryManager.Instance != null &&
                   InventoryManager.Instance.HasSelectedItem &&
                   InventoryManager.Instance.SelectedInventoryItem.Item is ToolItemSO;
        }

        public string GetCrosshairPrompt()
        {
            if (_isRepairing)
            {
                return _repairingPrompt;
            }

            if (!TryGetSelectedRepairableTool(out InventoryItem toolInventoryItem, out ToolItemSO toolItem))
            {
                return _selectToolPrompt;
            }

            if (!toolInventoryItem.CanRepair())
            {
                return _toolFullyRepairedPrompt;
            }

            if (!HasRepairRecipe(toolItem))
            {
                return _noRepairRecipePrompt;
            }

            if (!InventoryManager.Instance.InventoryHasItems(toolItem.RepairIngredients))
            {
                return _missingRepairMaterialsPrompt;
            }

            return _holdToRepairPrompt;
        }

        public InventoryItem[] GetRepairRequirementsForSelectedTool()
        {
            if (!TryGetSelectedRepairableTool(out InventoryItem toolInventoryItem, out ToolItemSO toolItem))
            {
                return null;
            }

            if (!toolInventoryItem.CanRepair() || !HasRepairRecipe(toolItem))
            {
                return null;
            }

            return toolItem.RepairIngredients;
        }

        public string GetRequirementContextId()
        {
            if (!TryGetSelectedRepairableTool(out InventoryItem toolInventoryItem, out ToolItemSO toolItem))
            {
                return $"sharp_none_{GetInstanceID()}";
            }

            int ingredientCount = toolItem.RepairIngredients != null ? toolItem.RepairIngredients.Length : 0;
            return $"sharp_{GetInstanceID()}_{toolInventoryItem.Id}_{toolInventoryItem.CurrentDurability}_{ingredientCount}";
        }

        private void TryStartRepair(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || _isRepairing)
            {
                return;
            }

            if (ActiveSharpeningStone != null && ActiveSharpeningStone != this)
            {
                return;
            }

            if (!CanStartRepair(out InventoryItem toolInventoryItem, out ToolItemSO toolItem))
            {
                return;
            }

            StartRepair(toolInventoryItem, toolItem);
        }

        private bool CanStartRepair(out InventoryItem toolInventoryItem, out ToolItemSO toolItem)
        {
            toolInventoryItem = null;
            toolItem = null;

            if (InteractionManager.Instance == null || InteractionManager.Instance.CurrentlyHoveredInteractable != this)
            {
                return false;
            }

            if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead ||
                Player.Instance.PauseMenuUI.IsPauseMenuOpen ||
                CraftingManager.Instance.IsCraftingUIOpen ||
                BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen ||
                Player.Instance.ToolHolder.IsSwinging ||
                Player.Instance.FirstPersonLook.IsSequenceOngoing ||
                SpearTetherManager.Instance.SpearTetherHolder.SequenceExecuting)
            {
                return false;
            }

            if (!TryGetSelectedRepairableTool(out toolInventoryItem, out toolItem))
            {
                return false;
            }

            if (!toolInventoryItem.CanRepair() || !HasRepairRecipe(toolItem))
            {
                return false;
            }

            return InventoryManager.Instance.InventoryHasItems(toolItem.RepairIngredients);
        }

        private bool TryGetSelectedRepairableTool(out InventoryItem toolInventoryItem, out ToolItemSO toolItem)
        {
            toolInventoryItem = null;
            toolItem = null;

            if (InventoryManager.Instance == null || !InventoryManager.Instance.TryGetSelectedToolInventoryItem(out toolInventoryItem, out toolItem))
            {
                return false;
            }

            return toolInventoryItem != null && toolInventoryItem.HasItem && toolItem != null;
        }

        private bool HasRepairRecipe(ToolItemSO toolItem)
        {
            return toolItem != null && toolItem.RepairIngredients != null && toolItem.RepairIngredients.Length > 0;
        }

        private void StartRepair(InventoryItem toolInventoryItem, ToolItemSO toolItem)
        {
            _repairingToolInventoryItem = toolInventoryItem;
            _repairingToolItem = toolItem;
            _repairTimer = new Timer(Mathf.Max(0.05f, toolItem.RepairDurationSeconds));
            _repairTimer.OnTimerEnd += OnRepairTimerFinished;
            _isRepairing = true;
            ActiveSharpeningStone = this;
            StartRepairLoopSFX();
        }

        private bool ShouldCancelRepair()
        {
            return !GameInput.Instance.IsHoldingDownSecondaryInteract ||
                   Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead ||
                   Player.Instance.PauseMenuUI.IsPauseMenuOpen ||
                   CraftingManager.Instance.IsCraftingUIOpen ||
                   InteractionManager.Instance.CurrentlyHoveredInteractable != this ||
                   InventoryManager.Instance.SelectedInventoryItem != _repairingToolInventoryItem ||
                   _repairingToolInventoryItem == null ||
                   !_repairingToolInventoryItem.HasItem;
        }

        private void OnRepairTimerFinished(object sender, EventArgs e)
        {
            _repairTimer.OnTimerEnd -= OnRepairTimerFinished;
            _repairTimer = null;

            if (_repairingToolInventoryItem == null || _repairingToolItem == null)
            {
                FinishRepairState();
                return;
            }

            if (!InventoryManager.Instance.InventoryHasItems(_repairingToolItem.RepairIngredients))
            {
                FinishRepairState();
                return;
            }

            InventoryManager.Instance.RemoveItems(_repairingToolItem.RepairIngredients);
            _repairingToolInventoryItem.AddDurability(_repairingToolItem.MaxDurability);
            InventoryManager.Instance.InventoryModel.UpdateInventory();
            ShowRepairToast(_repairingToolItem);
            StopRepairLoopSFX();
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ToolRepairCompleteSFX, transform.position);

            bool canChainRepair = GameInput.Instance.IsHoldingDownSecondaryInteract &&
                                  InteractionManager.Instance.CurrentlyHoveredInteractable == this &&
                                  _repairingToolInventoryItem == InventoryManager.Instance.SelectedInventoryItem &&
                                  _repairingToolInventoryItem.CanRepair() &&
                                  InventoryManager.Instance.InventoryHasItems(_repairingToolItem.RepairIngredients);

            if (canChainRepair)
            {
                StartRepair(_repairingToolInventoryItem, _repairingToolItem);
                return;
            }

            FinishRepairState();
        }

        private void CancelRepair()
        {
            if (_repairTimer != null)
            {
                _repairTimer.OnTimerEnd -= OnRepairTimerFinished;
                _repairTimer = null;
            }

            StopRepairLoopSFX();
            FinishRepairState();
        }

        private void FinishRepairState()
        {
            _isRepairing = false;
            _repairingToolInventoryItem = null;
            _repairingToolItem = null;

            if (ActiveSharpeningStone == this)
            {
                ActiveSharpeningStone = null;
            }
        }

        private void ShowRepairToast(ToolItemSO toolItem)
        {
            if (toolItem == null || PickupPanelHandler.Instance == null)
            {
                return;
            }

            PickupPanelHandler.Instance.ShowMessage(null, $"{toolItem.InGameName} Repaired!");
        }

        private void StartRepairLoopSFX()
        {
            StopRepairLoopSFX();

            _repairLoopEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.ToolRepairLoopSFX);
            if (!_repairLoopEventInstance.isValid())
            {
                return;
            }

            _repairLoopEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            _repairLoopEventInstance.start();
        }

        private void StopRepairLoopSFX()
        {
            if (!_repairLoopEventInstance.isValid())
            {
                return;
            }

            _repairLoopEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            _repairLoopEventInstance.release();
            _repairLoopEventInstance = default;
        }
    }
}
