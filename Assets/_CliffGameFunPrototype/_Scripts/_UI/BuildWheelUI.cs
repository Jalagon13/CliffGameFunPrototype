using System;
using System.Collections.Generic;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class BuildWheelUI : MonoBehaviour
    {
        [SerializeField] private float _defaultScale = 1f;
        [SerializeField] private float _selectedScale = 1.5f;

        [SerializeField] private GameObject _buildOptions;
    
        private GameObject _buildMenuUI;
        private Transform _lastClosestUI = null;
        private OptionUI _selectedOption;

        public bool BuildWheelUIOpen { get; private set; }
        
        private void Awake()
        {
            _buildMenuUI = transform.GetChild(0).gameObject;

            Hide();
        }
    
        private void Start()
        {
            GameInput.Instance.OnSecondaryInteract += GameInput_OnSecondaryInteract;
            GameInput.Instance.OnPrimaryInteract += GameInput_OnPrimaryInteract;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnSecondaryInteract -= GameInput_OnSecondaryInteract;
            GameInput.Instance.OnPrimaryInteract -= GameInput_OnPrimaryInteract;
        }

        private void Update()
        {
            Vector2 screenMousePosition = Mouse.current.position.ReadValue();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GetComponent<RectTransform>(),
                screenMousePosition,
                null,
                out Vector2 mousePosition
            );

            Transform closestUI = null;
            float closestDistance = float.MaxValue;

            foreach (Transform ui in _buildOptions.transform)
            {
                RectTransform rt = ui.GetComponent<RectTransform>();
                float distance = Vector2.Distance(mousePosition, rt.anchoredPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestUI = ui;
                }
            }

            foreach (Transform ui in _buildOptions.transform)
            {
                ui.transform.localScale = (ui == closestUI) ? Vector3.one * _selectedScale : Vector3.one * _defaultScale;
            }

            if (closestUI != _lastClosestUI)
            {
                _lastClosestUI = closestUI;
                
                
                
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SlotClickedSFX, transform.position);
            }
        }

        private void GameInput_OnPrimaryInteract(object sender, InputAction.CallbackContext e)
        {
            if(!BuildWheelUIOpen) return;
        
            if(e.started && _lastClosestUI != null)
            {
                _selectedOption = _lastClosestUI.GetComponent<OptionUI>();
                
                SetSelectedBuildOption(_selectedOption);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Hide();
            }
        }

        private void SetSelectedBuildOption(OptionUI buildOption)   
        {
            buildOption.OnSelected();

            foreach (Transform ui in _buildOptions.transform)
            {
                if (ui != _lastClosestUI)
                {
                    ui.GetComponent<OptionUI>().OnDeselected();
                }
            }
        }

        private void GameInput_OnSecondaryInteract(object sender, InputAction.CallbackContext e)
        {
            if(!BuildingManager.Instance.IsBuilding || CraftingManager.Instance.CraftingMenuUIOpened) return;
        
            if (e.started)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Show();
            }
            else if(e.canceled)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                
                Hide();
            }
        }
        
        private void Show()
        {
            _buildMenuUI.SetActive(true);
            BuildWheelUIOpen = true;
        }
        
        private void Hide()
        {
            _buildMenuUI.SetActive(false);
            BuildWheelUIOpen = false;
        }
    }
}
