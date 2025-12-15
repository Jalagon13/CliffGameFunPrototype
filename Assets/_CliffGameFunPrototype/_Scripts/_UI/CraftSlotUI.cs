using System;
using AdvancedTooltips.Core;
using SingularityGroup.HotReload;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CliffGame
{
    public class CraftSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Refs")]
        [SerializeField]
        private Image _outputImage;

        [SerializeField]
        private TextMeshProUGUI _outputAmountText;

        private RecipeSO _recipeSO;
        private bool _canCraft = false, _hovered;
        private CanvasGroup _canvasGroup;

        private void OnDisable()
        {
            if (_hovered)
            {
                Tooltip.HideUI();
            }
        }

        public void UpdateCraftStatus()
        {
            if(_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        
            _canCraft = CraftingManager.Instance.HasAllIngredients(_recipeSO.RequiredItems);
            
            if(_canCraft)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
            }
            else
            {
                _canvasGroup.alpha = 0.5f;
                _canvasGroup.interactable = false;
            }
            
            if(_hovered)
            {
                Tooltip.HideUI();
                Tooltip.ShowNew();
                Tooltip.CraftingRecipeDisplay(_recipeSO, fontSize: 12f, iconScale: 0.6f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(_canCraft)
            {
                CraftingManager.Instance.TryToCraft(_recipeSO);
            }
        }
        
        public void InitializeCraftNode(RecipeSO recipeData)
        {
            _recipeSO = recipeData;
            _outputImage.sprite = _recipeSO.ResultItem.UiDisplay;
            _outputAmountText.text = _recipeSO.ResultAmount == 1 ? string.Empty : _recipeSO.ResultAmount.ToString();

            UpdateCraftStatus();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.ShowNew();
            Tooltip.CraftingRecipeDisplay(_recipeSO, fontSize: 12f, iconScale: 0.6f);
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideUI();
            _hovered = false;
        }
    }
}
