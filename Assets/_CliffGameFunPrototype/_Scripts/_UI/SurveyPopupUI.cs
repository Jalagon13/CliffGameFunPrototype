using TMPro;
using UnityEngine;

namespace CliffGame
{
    public class SurveyPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _surveyPopupText;
        [SerializeField] private float _timeUntilPopup;
        [SerializeField] private float _timeUntilItDisappearsAfterPopup;

        private float _popupTimer;
        private float _hideTimer;
        private bool _isPopupShowing;
        private bool _popupFinished;

        private void Start()
        {
            if (_surveyPopupText == null)
            {
                return;
            }

            _surveyPopupText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_surveyPopupText == null || _popupFinished || Time.timeScale <= 0f)
            {
                return;
            }

            if (!_isPopupShowing)
            {
                _popupTimer += Time.deltaTime;
                if (_popupTimer >= _timeUntilPopup)
                {
                    _isPopupShowing = true;
                    _hideTimer = 0f;
                    _surveyPopupText.gameObject.SetActive(true);
                }

                return;
            }

            _hideTimer += Time.deltaTime;
            if (_hideTimer >= _timeUntilItDisappearsAfterPopup)
            {
                _surveyPopupText.gameObject.SetActive(false);
                _popupFinished = true;
            }
        }
    }
}
