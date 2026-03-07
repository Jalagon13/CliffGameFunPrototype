using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class TutorialMenuUI : MonoBehaviour
    {
        [SerializeField] 
        private GameObject _tutorialMenuUI;
        
        [SerializeField] 
        private TextMeshProUGUI _tutorialToggleText;

        [Header("First Toggle Prompt Pulse")]
        [SerializeField]
        private bool _pulseTutorialToggleTextUntilFirstOpen = true;

        [SerializeField, Min(0f)]
        private float _pulseMinScale = 1f;

        [SerializeField, Min(0f)]
        private float _pulseMaxScale = 2f;

        [SerializeField, Min(0.01f)]
        private float _pulseCycleDuration = 1f;

        [SerializeField]
        private Color _pulsePeakColor = Color.yellow;
        
        private bool _tutorialMenuOpen;
        public bool IsTutorialMenuOpen => _tutorialMenuOpen;

        private bool _hasToggledTutorialMenuOnce;
        private bool _isPulsingTutorialToggleText;
        private Vector3 _defaultToggleTextScale;
        private Color _defaultToggleTextColor;
        private float _pulseTimer;
    
        private void Start()
        {
            GameInput.Instance.OnToggleJournalMenu += HandleToggleJournalMenu;

            CacheTutorialToggleTextDefaults();
            TryStartTutorialTogglePulse();
            Hide();
        }

        private void Update()
        {
            if (!_isPulsingTutorialToggleText || _tutorialToggleText == null)
            {
                return;
            }

            _pulseTimer += Time.deltaTime;
            float phase = Mathf.PingPong(_pulseTimer / _pulseCycleDuration, 1f);
            float scaleFactor = Mathf.Lerp(_pulseMinScale, _pulseMaxScale, phase);

            _tutorialToggleText.rectTransform.localScale = _defaultToggleTextScale * scaleFactor;
            _tutorialToggleText.color = Color.Lerp(_defaultToggleTextColor, _pulsePeakColor, phase);
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnToggleJournalMenu -= HandleToggleJournalMenu;
        }

        private void HandleToggleJournalMenu(object sender, InputAction.CallbackContext e)
        {
            if (!_hasToggledTutorialMenuOnce)
            {
                _hasToggledTutorialMenuOnce = true;
                StopTutorialTogglePulseAndReset();
            }

            _tutorialMenuOpen = !_tutorialMenuOpen;

            if (_tutorialMenuOpen)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        private void Show()
        {
            _tutorialMenuUI.SetActive(true);
        }
        
        private void Hide()
        {
            _tutorialMenuUI.SetActive(false);
        }

        private void CacheTutorialToggleTextDefaults()
        {
            if (_tutorialToggleText == null)
            {
                return;
            }

            _defaultToggleTextScale = _tutorialToggleText.rectTransform.localScale;
            _defaultToggleTextColor = _tutorialToggleText.color;
        }

        private void TryStartTutorialTogglePulse()
        {
            if (_tutorialToggleText == null || !_pulseTutorialToggleTextUntilFirstOpen || _hasToggledTutorialMenuOnce)
            {
                _isPulsingTutorialToggleText = false;
                return;
            }

            _pulseTimer = 0f;
            _isPulsingTutorialToggleText = true;
        }

        private void StopTutorialTogglePulseAndReset()
        {
            _isPulsingTutorialToggleText = false;
            _pulseTimer = 0f;

            if (_tutorialToggleText == null)
            {
                return;
            }

            _tutorialToggleText.rectTransform.localScale = _defaultToggleTextScale;
            _tutorialToggleText.color = _defaultToggleTextColor;
        }
    }
}
