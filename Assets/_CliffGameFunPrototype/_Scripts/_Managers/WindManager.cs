using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CliffGame
{
    public class WindManager : MonoBehaviour
    {
        public static WindManager Instance;

        [Header("Wind Settings")]
        [SerializeField, Range(0, 1f)]
        private float _windSeverity = 0.2f;
        public float WindSeverity => _windSeverity;

        [SerializeField, Range(0, 1f)]
        private float _windPushesPlayerThreshold = 0.2f;
        public float WindPushesPlayerThreshold => _windPushesPlayerThreshold;

        [Header("Wind Tick Settings")]
        [SerializeField, Range(0f, 1f)]
        private float _windTickInterval = 0.25f;

        [SerializeField, Range(0f, 1f)]
        private float _minRattleChance = 0.02f;

        [SerializeField, Range(0f, 1f)]
        private float _maxRattleChance = 0.4f;

        [field: SerializeField]
        public float MaxWindForceAtFullSeverity { get; private set; } = 15f;

        [SerializeField]
        private AnimationCurve _rattleSeverityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Storm Settings")]
        [SerializeField, Tooltip("How often does a crack happen during a storm in seconds")]
        private float _secondsPerCrack = 3f;

        [SerializeField] 
        private float _timeInBetweenStormsInSec = 300f;

        [SerializeField]
        private float _radiusOfRattlingAroundPlayer = 15f;// Only rattle in the radius of the player since that is all that matters

        [SerializeField]
        private int _damagePerCrack = 25;

        [SerializeField] 
        private float _startSeverity = 0.14f, _peakSeverity = 1f, _rampUpTime = 5f, _holdTime = 15f, _rampDownTime = 10f;

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
        private readonly List<Platform> _foundationBuffer = new();

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            InvokeRepeating(nameof(WindTick), _windTickInterval, _windTickInterval);
            InvokeRepeating(nameof(StartScheduledWindStorm), _timeInBetweenStormsInSec, _timeInBetweenStormsInSec);
        }

        private void Update()
        {
            WindGameFeelHandler();
        }

        private void StartScheduledWindStorm()
        {
            if (_windStormRoutine != null)
                StopCoroutine(_windStormRoutine);

            _windStormRoutine = StartCoroutine(WindStormSequence(_startSeverity, _peakSeverity, _rampUpTime, _holdTime, _rampDownTime));
        }

        private void WindTick()
        {
            // Do nothing if wind is below the rattle threshold
            if (_windSeverity < _windPushesPlayerThreshold)
                return;

            // Normalize wind severity between threshold and full storm (0–1)
            float normalizedSeverity = Mathf.InverseLerp(
                _windPushesPlayerThreshold,
                1f,
                _windSeverity
            );

            // Shape the severity using the designer-controlled curve
            float curvedSeverity = _rattleSeverityCurve.Evaluate(normalizedSeverity);

            // Convert severity into a final rattle chance
            float rattleChance = Mathf.Lerp(
                _minRattleChance,
                _maxRattleChance,
                curvedSeverity
            );
            
            // Debug.Log($"Rattle Chance: {rattleChance}");

            Collider[] hits = Physics.OverlapSphere(
                Player.Instance.transform.position,
                _radiusOfRattlingAroundPlayer
            );

            if (UnityEngine.Random.value < rattleChance)
            {
                // Collect all valid, non-rattling foundations
                _foundationBuffer.Clear();

                foreach (var hit in hits)
                {
                    if (!hit.transform.root.TryGetComponent<Platform>(out var platform))
                        continue;

                    if (platform.IsRattling)
                        continue;

                    if (!_foundationBuffer.Contains(platform))
                        _foundationBuffer.Add(platform);
                }

                // If we found at least one valid platform, pick one at random
                if (_foundationBuffer.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, _foundationBuffer.Count);
                    _foundationBuffer[randomIndex].PlayRattleFeedbacks();
                }
            }
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
            yield return SustainWindSeverity(holdTime);

            // RAMP DOWN
            Debug.Log($"Start of wind storm ramp down");
            yield return LerpWindSeverity(peakSeverity, startSeverity, rampDownTime);

            Debug.Log($"End of wind storm, back to starting severity");
            _windSeverity = startSeverity;
            _windStormRoutine = null;
        }

        private IEnumerator SustainWindSeverity(float holdTime)
        {
            int crackCount = Mathf.Max(1, Mathf.FloorToInt(holdTime / _secondsPerCrack));

            // Generate random crack times within the hold duration
            List<float> crackTimes = new List<float>();
            for (int i = 0; i < crackCount; i++)
            {
                crackTimes.Add(UnityEngine.Random.Range(0f, holdTime));
            }

            // Sort so cracks happen in chronological order
            crackTimes.Sort();

            float elapsed = 0f;
            int nextCrackIndex = 0;

            while (elapsed < holdTime)
            {
                elapsed += Time.deltaTime;

                // Trigger cracks when their scheduled time is reached
                if (nextCrackIndex < crackTimes.Count && elapsed >= crackTimes[nextCrackIndex])
                {
                    CrackRandomPlatform();
                    nextCrackIndex++;
                }

                yield return null;
            }
        }

        private void CrackRandomPlatform()
        {
            Collider[] hits = Physics.OverlapSphere(
                Player.Instance.transform.position,
                _radiusOfRattlingAroundPlayer
            );

            List<Platform> validPlatforms = new();

            foreach (var hit in hits)
            {
                if (!hit.transform.root.TryGetComponent<Platform>(out var platform))
                    continue;

                if (!validPlatforms.Contains(platform))
                    validPlatforms.Add(platform);
            }

            if (validPlatforms.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, validPlatforms.Count);
                validPlatforms[randomIndex].AddFloorHp(-_damagePerCrack);
                Debug.Log($"Wind cracked platform: {validPlatforms[randomIndex].name}");
            }

            validPlatforms.Clear();
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
