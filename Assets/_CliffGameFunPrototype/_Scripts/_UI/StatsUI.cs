using System;
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
        [SerializeField] private MMProgressBar _thirstBar;
        
        private TextMeshProUGUI _healthText;
        private TextMeshProUGUI _hungerText;
        private TextMeshProUGUI _thirstText;

        private void Awake()
        {
            _healthText = _healthBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _hungerText = _hungerBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _thirstText = _thirstBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
        }
        
        private void Start()
        {
            HealthManager.Instance.OnHealthChanged += HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged += HandleHungerChanged;
            ThirstManager.Instance.OnThirstChanged += HandleThirstChanged;
            Player.Instance.OnStateChanged += OnMoveStateChanged;
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged -= HandleHungerChanged;
            ThirstManager.Instance.OnThirstChanged -= HandleThirstChanged;
            Player.Instance.OnStateChanged -= OnMoveStateChanged;
        }

        private void OnMoveStateChanged(PlayerMoveState prevState, PlayerMoveState newState)
        {
            // if (newState == PlayerMoveState.Climbing)
            // {
            //     _staminaBar.ShowBar();
            //     OnStaminaBarShown();
            // }
        }

        private void HandleThirstChanged(int currentAmount, int maxAmount)
        {
            _thirstBar.UpdateBar(currentAmount, 0, maxAmount);
            _thirstText.text = $"Thirst: {currentAmount}%";
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
