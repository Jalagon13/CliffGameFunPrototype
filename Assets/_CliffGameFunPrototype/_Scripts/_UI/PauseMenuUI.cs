using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static Action OnPauseMenuOpened;
        public static Action OnPauseMenuClosed;
    
        [SerializeField] private GameObject _pauseMenuUI;
    
        private bool _pauseMenuOpen;
        public bool PauseMenuOpen => _pauseMenuOpen;
    
        private void Start()
        {
            Hide();
        
            GameInput.Instance.OnTogglePauseMenu += GameInput_OnTogglePauseMenu;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnTogglePauseMenu -= GameInput_OnTogglePauseMenu;
        }

        private void GameInput_OnTogglePauseMenu(object sender, InputAction.CallbackContext e)
        {
            if(CraftingManager.Instance.IsCraftingUIOpen)
            {
                CraftingManager.Instance.ToggleCraftingUI();   
                return;
            }
            else if(BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen)
            {
                BuildingManager.Instance.BuildWheelUI.ToggleBuildWheelUI();
                return;
            }
        
            _pauseMenuOpen = !_pauseMenuOpen;
            
            if (_pauseMenuOpen)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        public void ResumeButtonPressed()
        {
            _pauseMenuOpen = false;
            Hide();
        }
        
        public void QuitToMainMenuButtonPressed()
        {
            Time.timeScale = 1f;
            // Scene loading stuff here
            Loader.Load(Loader.Scene.MainMenuScene);
        }
        
        private void Show()
        {
            OnPauseMenuOpened?.Invoke();
            
            _pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void Hide()
        {
            OnPauseMenuClosed?.Invoke();
            
            _pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
