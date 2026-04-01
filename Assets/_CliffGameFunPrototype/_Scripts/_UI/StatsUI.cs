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

        private TextMeshProUGUI _healthText;
        private TextMeshProUGUI _hungerText;
        private TextMeshProUGUI _staminaText;
        private float _previousStaminaPercent = 1f;

        private void Awake()
        {
            _healthText = _healthBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _hungerText = _hungerBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            HealthManager.Instance.OnHealthChanged += HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged += HandleHungerChanged;
            Player.Instance.OnStateChanged += OnMoveStateChanged;
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged -= HandleHungerChanged;
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
    }
}
