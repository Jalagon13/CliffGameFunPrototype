using System;
using UnityEngine;

namespace CliffGame
{
    public class WindManager : MonoBehaviour
    {
        public static WindManager Instance;

        [Header("Storm Settings")]

        [Header("Wind Settings")]
        [SerializeField, Range(0, 1f)]
        private float _windSeverity = 0.2f;
        // public float WindSeverity => _windSeverity;

        [SerializeField, Range(0, 1f)]
        private float _windPushesPlayerThreshold = 0.2f;
        // public float WindPushesPlayerThreshold => _windPushesPlayerThreshold;

        // [SerializeField, Tooltip("Multiplies this with the wind severity to determine the chance to rattle platforms when a platform is chosen")]
        // private float _severityMultiplier = 0.5f;
        [SerializeField, Tooltip("Interval between each attempt to rattle a platform depending on the wind severity")]
        private float _windTickInterval = 0.15f;

        // [SerializeField] private int _minPlatformDamagePerRattle = 8;
        // [SerializeField] private int _maxPlatformDamagePerRattle = 16;

        // [Header("Crit Rattle Settings")]
        // [SerializeField, Range(0f, 1f)]
        // private float _minCritChance = 0.05f;

        // [SerializeField, Range(0f, 1f)]
        // private float _maxCritChance = 0.4f;

        // [SerializeField]
        // private float _critDamageMultiplier = 3f;

        // [field: SerializeField]
        // public float MaxWindForceAtFullSeverity { get; private set; } = 15f;

        [Header("Wind Particles Settings")]
        [SerializeField] private float _minWindParticleSpeed = 10f;
        [SerializeField] private float _maxWindParticleSpeed = 25f;
        [SerializeField] private float _minWindParticleRateOverTime = 20f;
        [SerializeField] private float _maxWindParticleRateOverTime = 80f;
        [SerializeField] private float _minWindParticleStartSize = 0.125f;
        [SerializeField] private float _maxWindParticleStartSize = 0.25f;

        [SerializeField] private ParticleSystem _windStormParticles;
        [SerializeField] private Transform _playerTransform;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InvokeRepeating(nameof(WindTick), _windTickInterval, _windTickInterval);
        }

        private void WindTick()
        {
            Transform player = _playerTransform;
            float radius = 50f;

            Collider[] hits = Physics.OverlapSphere(player.position, radius);

            foreach (var hit in hits)
            {
                // if (!hit.gameObject.transform.root.TryGetComponent<Floor>(out var platform))
                //     continue;

                // if (platform.IsRattling)
                //     continue;

                // float chance = _windSeverity * _severityMultiplier;
                // if (UnityEngine.Random.value < chance)
                // {
                //     int baseDamage = Mathf.RoundToInt(Mathf.Lerp(_minPlatformDamagePerRattle, _maxPlatformDamagePerRattle, _windSeverity));
                //     float critChance = Mathf.Lerp(_minCritChance, _maxCritChance, _windSeverity);
                //     bool isCrit = UnityEngine.Random.value < critChance;

                //     int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * _critDamageMultiplier) : baseDamage;

                //     platform.RattleFloor(finalDamage);
                // }
            }

            // Update wind particle settings based on current storm severity
            var main = _windStormParticles.main;
            var emission = _windStormParticles.emission;

            // Lerp particle speed
            float particleSpeed = Mathf.Lerp(_minWindParticleSpeed, _maxWindParticleSpeed, _windSeverity);
            main.startSpeed = particleSpeed;

            // Lerp particle rate over time
            float particleRate = Mathf.Lerp(_minWindParticleRateOverTime, _maxWindParticleRateOverTime, _windSeverity);
            emission.rateOverTime = particleRate;

            // Lerp particle start size
            float particleSize = Mathf.Lerp(_minWindParticleStartSize, _maxWindParticleStartSize, _windSeverity);
            main.startSize = particleSize;

            AudioManager.Instance.SetWindSeverity(_windSeverity);
        }
    }
}
