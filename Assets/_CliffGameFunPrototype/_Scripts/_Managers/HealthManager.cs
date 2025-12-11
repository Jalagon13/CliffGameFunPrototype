using System;
using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class HealthManager : MonoBehaviour
    {
        public static HealthManager Instance;
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnPlayerDeath;

        [SerializeField]
        private int _maxHealth = 100;

        [SerializeField]
        private float _noHungerHealthDrainPerSecond = 1f;

        private PlayerStat _healthStat;

        public int CurrentHealth => _healthStat.Current;

        private void Awake()
        {
            Instance = this;

            _healthStat = new PlayerStat(_maxHealth, _noHungerHealthDrainPerSecond);

            _healthStat.OnValueChanged += (current, max) =>
            {
                OnHealthChanged?.Invoke(current, max);
            };
        }

        private IEnumerator Start()
        {
            Player.Instance.OnPlayerRespawn += OnRespawn;

            yield return null;
            OnHealthChanged?.Invoke(CurrentHealth, _healthStat.Max);
        }

        private void OnDestroy()
        {
            Player.Instance.OnPlayerRespawn -= OnRespawn;
        }

        private void Update()
        {
            if (HungerManager.Instance.CurrentHungerState == HungerState.Hungry)
            {
                _healthStat.UpdateStat(Time.deltaTime, true);
            }
        }

        private void OnRespawn()
        {
            RestoreHealth(_maxHealth);
        }

        public void RestoreHealth(int amount)
        {
            _healthStat.ChangeCurrent(amount);
        }

        public void DamageHealth(int amount)
        {
            int amountToDamage = Mathf.Abs(amount);
            _healthStat.ChangeCurrent(-amountToDamage);
            
            if(_healthStat.Current <= 0)
            {
                OnPlayerDeath?.Invoke();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerHurtSFX, Player.Instance.transform.position);
            }
        }
    }
}
