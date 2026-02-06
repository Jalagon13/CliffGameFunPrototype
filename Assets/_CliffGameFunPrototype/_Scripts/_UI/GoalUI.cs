using UnityEngine;

namespace CliffGame
{
    public class GoalUI : MonoBehaviour
    {
        [Header("End Message Settings")]
        [SerializeField] private GameObject _endMessageGO;

        [Tooltip("Seconds before the end message appears (e.g. 900 for 15 minutes)")]
        [SerializeField] private float _timeBeforeMessage = 900f;

        [Header("Flash Settings")]
        [Tooltip("Total time the message flashes on and off")]
        [SerializeField] private float _flashDuration = 3f;

        [Tooltip("Time between each flash toggle")]
        [SerializeField] private float _flashInterval = 0.3f;

        [Header("Linger Settings")]
        [Tooltip("How long the message stays visible after flashing")]
        [SerializeField] private float _lingerDuration = 15f;

        private Timer _endMessageTimer;

        private void Start()
        {
            if (_endMessageGO != null)
            {
                _endMessageGO.SetActive(false);
            }

            _endMessageTimer = new Timer(_timeBeforeMessage);
            _endMessageTimer.OnTimerEnd += HandleTimerEnd;
        }
        
        private void OnDestroy()
        {
            if (_endMessageTimer != null)
            {
                _endMessageTimer.OnTimerEnd -= HandleTimerEnd;
            }
        }

        private void Update()
        {
            if (_endMessageTimer == null) return;

            // Pause timer when pause menu is open
            _endMessageTimer.IsPaused = Player.Instance != null &&
                                        Player.Instance.PauseMenuUI != null &&
                                        Player.Instance.PauseMenuUI.IsPauseMenuOpen;

            _endMessageTimer.Tick(Time.deltaTime);
        }

        private void HandleTimerEnd(object sender, System.EventArgs e)
        {
            ShowEndMessage();
        }

        private void ShowEndMessage()
        {
            if (_endMessageGO == null) return;

            StartCoroutine(EndMessageRoutine());
        }

        private System.Collections.IEnumerator EndMessageRoutine()
        {
            float elapsed = 0f;
            bool visible = false;

            // Flash phase
            while (elapsed < _flashDuration)
            {
                visible = !visible;
                _endMessageGO.SetActive(visible);

                yield return new WaitForSeconds(_flashInterval);
                elapsed += _flashInterval;
            }

            // Ensure message is visible
            _endMessageGO.SetActive(true);

            // Linger phase
            yield return new WaitForSeconds(_lingerDuration);

            // Disable message
            _endMessageGO.SetActive(false);
        }
    }
}
