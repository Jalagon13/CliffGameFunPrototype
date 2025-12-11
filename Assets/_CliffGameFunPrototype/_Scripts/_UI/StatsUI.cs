using System;
using UnityEngine;

namespace CliffGame
{
    public class StatsUI : MonoBehaviour
    {
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
            
        }

        private void HandleHungerChanged(int currentAmount, int maxAmount)
        {
            
        }

        private void HandleStaminaChanged(int currentAmount, int maxAmount)
        {
            
        }
    }
}
