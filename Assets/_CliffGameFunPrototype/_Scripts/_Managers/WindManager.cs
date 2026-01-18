using System;
using System.Collections;
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
        public float WindSeverity => _windSeverity;

        [SerializeField, Range(0, 1f)]
        private float _windPushesPlayerThreshold = 0.2f;
        public float WindPushesPlayerThreshold => _windPushesPlayerThreshold;

        [field: SerializeField]
        public float MaxWindForceAtFullSeverity { get; private set; } = 15f;

        [Header("Wind Particles Settings")]
        [SerializeField] private float _minWindParticleSpeed = 10f;
        [SerializeField] private float _maxWindParticleSpeed = 25f;
        [SerializeField] private float _minWindParticleRateOverTime = 20f;
        [SerializeField] private float _maxWindParticleRateOverTime = 80f;
        [SerializeField] private float _minWindParticleStartSize = 0.125f;
        [SerializeField] private float _maxWindParticleStartSize = 0.25f;
        [SerializeField] private ParticleSystem _windStormParticles;
        [SerializeField] private Transform _playerTransform;

        private Coroutine _windStormRoutine;

        private void Awake()
        {
            Instance = this;
        }
        
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(3f);

            StartWindStorm(0.125f, 0.75f, 5, 10, 5);
        }

        private void Update()
        {
            WindGameFeelHandler();
        }

        public void StartWindStorm(float startSeverity, float peakSeverity, float rampUpTime, float holdTime, float rampDownTime)
        {
            if (_windStormRoutine != null)
                StopCoroutine(_windStormRoutine);

            _windStormRoutine = StartCoroutine(
                WindStormSequence(startSeverity, peakSeverity, rampUpTime, holdTime, rampDownTime)
            );
        }

        private IEnumerator WindStormSequence(float startSeverity, float peakSeverity, float rampUpTime, float holdTime, float rampDownTime)
        {
            // Clamp for safety
            startSeverity = Mathf.Clamp01(startSeverity);
            peakSeverity = Mathf.Clamp01(peakSeverity);

            // Start at initial severity
            _windSeverity = startSeverity;

            // RAMP UP
            Debug.Log($"Start of wind storm ramp up");
            yield return LerpWindSeverity(startSeverity, peakSeverity, rampUpTime);

            // HOLD
            Debug.Log($"Start of wind storm hold");
            yield return new WaitForSeconds(holdTime);

            // RAMP DOWN
            Debug.Log($"Start of wind storm ramp down");
            yield return LerpWindSeverity(peakSeverity, startSeverity, rampDownTime);

            Debug.Log($"End of wind storm, back to starting severity");
            _windSeverity = startSeverity;
            _windStormRoutine = null;
        }

        private IEnumerator LerpWindSeverity(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _windSeverity = to;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _windSeverity = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _windSeverity = to;
        }

        private void WindGameFeelHandler()
        {
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
