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

        // private bool _wasHarvesting = false; // Track previous state
        private Image _crosshairImage;
        
        private void Awake()
        {
            _crosshairImage = transform.GetChild(0).GetComponent<Image>();
            HideInteractableInfo();
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnSelectedSlotChanged += CheckForHammer;

            _interactRadialBar.gameObject.SetActive(false); // TEMP. Delete this later
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedSlotChanged -= CheckForHammer;
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
        }
        
        private void ShowInteractableInfo()
        {
            IInteractable interactable = InteractionManager.Instance.CurrentlyHoveredInteractable;

            if(interactable is CookingStation)
            {
                _crosshairImage.sprite = _rawBirdSprite;
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

            // Set full alpha
            Color c = _crosshairImage.color;
            c.a = 1f;
            _crosshairImage.color = c;
        }
        
        private void HideInteractableInfo()
        {
            _crosshairImage.sprite = _defaultSprite;

            Color c = _crosshairImage.color;
            c.a = 0.25f;
            _crosshairImage.color = c;
        }

        // private void Update()
        // {
        //     bool isHarvesting = InteractionManager.Instance.IsHarvesting;

        //     if (isHarvesting)
        //     {
        //         _interactRadialBar.UpdateBar(InteractionManager.Instance.HarvestTimer.PercentRemaining, 0, 1);
        //         _interactRadialBar.gameObject.SetActive(true);
                
        //         if (!_wasHarvesting)
        //         {
        //             OnHarvestStarted();
        //         }
        //     }
        //     else
        //     {
        //         _interactRadialBar.UpdateBar(1, 0, 1);
        //         _interactRadialBar.gameObject.SetActive(false);
        //     }

        //     _wasHarvesting = isHarvesting; // Update previous state
        // }

        // private void OnHarvestStarted()
        // {
        //     _interactRadialBar.UpdateBar(0, 0, 1);
        // }


        private void CheckForHammer(int arg1, InventoryItem item)
        {
            if(item.Item is ToolItemSO toolItem && toolItem.ToolType == ToolType.Hammer) 
            {
                PopulateBuildReqs();        
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