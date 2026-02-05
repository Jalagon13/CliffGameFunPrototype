using System;
using System.Collections;
using Sirenix.OdinInspector;
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
        private float _noVitalsHealthDrainPerSecond = 1f, _passiveHealthGenPerSecond = 0.25f;

        private PlayerStat _healthStat;

        public int CurrentHealth => _healthStat.Current;

        private void Awake()
        {
            Instance = this;

            _healthStat = new PlayerStat(_maxHealth, _noVitalsHealthDrainPerSecond, _passiveHealthGenPerSecond);

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
            if (HungerManager.Instance.CurrentHungerState == HungerState.Starving || ThirstManager.Instance.CurrentThirstState == ThirstState.Thirsty)
            {
                _healthStat.UpdateStat(Time.deltaTime, true);
            }
            else
            {
                _healthStat.UpdateStat(Time.deltaTime, false);
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

        [Button("TestDamage")]
        public void DamageHealth(int amount)
        {
            int amountToDamage = Mathf.Abs(amount);
            _healthStat.ChangeCurrent(-amountToDamage);
            
            if(_healthStat.Current <= 0)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerHurtSFX, Player.Instance.transform.position);
                OnPlayerDeath?.Invoke();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerHurtSFX, Player.Instance.transform.position);
            }
        }
    }
}
