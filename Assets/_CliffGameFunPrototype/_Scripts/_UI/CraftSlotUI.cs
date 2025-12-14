using System;
using SingularityGroup.HotReload;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CliffGame
{
    public class CraftSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Refs")]
        [SerializeField]
        private Image _outputImage;

        [SerializeField]
        private TextMeshProUGUI _outputAmountText;

        private RecipeSO _recipeSO;
        private bool _canCraft = false;
        
        public void UpdateCraftStatus()
        {
            _canCraft = CraftingManager.Instance.HasAllIngredients(_recipeSO.RequiredItems);
            
            if(_canCraft)
            {
                
            }
            else
            {
                
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
    }
}
