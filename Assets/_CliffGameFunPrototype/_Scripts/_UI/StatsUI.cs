using System;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace CliffGame
{
    public class StatsUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _healthBar;
        [SerializeField] private MMProgressBar _hungerBar;
        [SerializeField] private MMProgressBar _staminaBar;
    
        private void Start()
        {
            HealthManager.Instance.OnHealthChanged += HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged += HandleHungerChanged;
            StaminaManager.Instance.OnStaminaChanged += HandleStaminaChanged;
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
            HungerManager.Instance.OnHungerChanged -= HandleHungerChanged;
            StaminaManager.Instance.OnStaminaChanged -= HandleStaminaChanged;
        }

        private void HandleHealthChanged(int currentAmount, int maxAmount)
        {
            _healthBar.UpdateBar(currentAmount, 0, maxAmount);
            _healthBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = $"Life: {currentAmount}/{maxAmount}";
        }

        private void HandleHungerChanged(int currentAmount, int maxAmount)
        {
            _hungerBar.UpdateBar(currentAmount, 0, maxAmount);
            _hungerBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = $"Hunger: {currentAmount}/{maxAmount}";
        }

        private void HandleStaminaChanged(int currentAmount, int maxAmount)
        {
            _staminaBar.UpdateBar(currentAmount, 0, maxAmount);
            _staminaBar.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = $"Stamina: {currentAmount}/{maxAmount}";
        }
    }
}
