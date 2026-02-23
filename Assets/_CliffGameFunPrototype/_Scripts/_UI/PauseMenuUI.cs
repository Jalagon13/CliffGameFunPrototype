using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static Action OnPauseMenuOpened;
        public static Action OnPauseMenuClosed;
    
        [SerializeField] private GameObject _pauseMenuUI;
        [SerializeField] private float _shortUnpauseDelay = 0.2f;
    
        private bool _pauseMenuOpen;
        public bool IsPauseMenuOpen => _pauseMenuOpen;
    
        private void Start()
        {
            Hide(false);
        
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
                CraftingManager.Instance.ToggleCraftingUI(false);   
                return;
            }
            else if(BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen)
            {
                BuildingManager.Instance.BuildWheelUI.ToggleBuildWheelUI();
                return;
            }
        
            if (!_pauseMenuOpen)
            {
                Show();
            }
            else
            {
                Hide(false);
            }
        }
        
        public void ResumeButtonPressed()
        {
            Hide(true);
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
            _pauseMenuOpen = true;
            
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void Hide(bool delay)
        {
            if(delay)
            {
                StartCoroutine(Delay());
            }
            else
            {
                OnPauseMenuClosed?.Invoke();

                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                _pauseMenuUI.SetActive(false);
                _pauseMenuOpen = false;
            }
        }
        
        private IEnumerator Delay()
        {
            Time.timeScale = 1f;
            
            yield return new WaitForSecondsRealtime(_shortUnpauseDelay);
            OnPauseMenuClosed?.Invoke();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _pauseMenuUI.SetActive(false);
            _pauseMenuOpen = false;
        }
    }
}
