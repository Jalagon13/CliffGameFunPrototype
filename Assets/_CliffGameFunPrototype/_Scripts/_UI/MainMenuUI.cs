using FMOD.Studio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _joinGameButton;
        [SerializeField] private Button _quitButton;

        private EventInstance _titleMenuMusicEventInstance;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            _newGameButton.onClick.AddListener(() =>
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Loader.Load(Loader.Scene.CliffGameFunPrototype);
            });

            _loadGameButton.onClick.AddListener(() =>
            {
                // _relay.JoinRelay(_joinInput.text);
            });

            _joinGameButton.onClick.AddListener(() =>
            {
                // _relay.JoinRelay(_joinInput.text);
            });

            _quitButton.onClick.AddListener(() =>
            {
                Application.Quit();
            });

            Time.timeScale = 1f;
        }

        private void Start()
        {
            // AudioManager.Instance.InitializeAmbience(FMODEvents.Instance.WindAmb);

            _titleMenuMusicEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.TitleMusic);
            _titleMenuMusicEventInstance.start();
        }

        private void OnDestroy()
        {
            // AudioManager.Instance.StopCurrentAmbience();
            _titleMenuMusicEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
