using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace CliffGame
{
    public class StatsUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _healthBar;
        [SerializeField] private MMProgressBar _hungerBar;

        [Header("Stamina Settings")]
        [SerializeField] private MMProgressBar _staminaBar;
        [SerializeField, Range(0f, 1f)] private float _staminaDropThreshold = 0.2f;
        [SerializeField] private MMF_Player _staminaDropThresholdFeedback;

        private TextMeshProUGUI _healthText;
        private TextMeshProUGUI _hungerText;
        private TextMeshProUGUI _staminaText;
        private float _previousStaminaPercent = 1f;

        private void Awake()
        {
            _healthText = _healthBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _hungerText = _hungerBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _staminaText = _staminaBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            HealthManager.Instance.OnHealthChanged += HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged += HandleHungerChanged;
            StaminaManager.Instance.OnStaminaChanged += HandleStaminaChanged;
            Player.Instance.OnStateChanged += OnMoveStateChanged;
            _staminaBar.HideBar(0f);
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged -= HandleHungerChanged;
            StaminaManager.Instance.OnStaminaChanged -= HandleStaminaChanged;
            Player.Instance.OnStateChanged -= OnMoveStateChanged;
        }

        private void OnMoveStateChanged(PlayerMoveState prevState, PlayerMoveState newState)
        {
        }

        private void HandleHealthChanged(int currentAmount, int maxAmount)
        {
            _healthBar.UpdateBar(currentAmount, 0, maxAmount);
            _healthText.text = $"Life: {currentAmount}%";
        }

        private void HandleHungerChanged(int currentAmount, int maxAmount)
        {
            _hungerBar.UpdateBar(currentAmount, 0, maxAmount);
            _hungerText.text = $"Hunger: {currentAmount}%";
        }

        private void HandleStaminaChanged(int currentAmount, int maxAmount)
        {
            float currentPercent = (float)currentAmount / maxAmount;
            bool isFull = currentPercent >= 1f;

            if (_previousStaminaPercent > _staminaDropThreshold && currentPercent <= _staminaDropThreshold)
            {
                _staminaDropThresholdFeedback.PlayFeedbacks();
            }

            _previousStaminaPercent = currentPercent;
            _staminaBar.UpdateBar(currentAmount, 0, maxAmount, false);
            _staminaText.text = $"Stamina: {Mathf.RoundToInt(currentPercent * 100f)}%";

            if (isFull)
            {
                _staminaBar.HideBar(0f);
            }
        }
    }
}
