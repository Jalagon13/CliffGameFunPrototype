using MoreMountains.Feedbacks;
using UnityEngine;

namespace CliffGame
{
    public class NaturalResource : Resource
    {
        [Header("Natural Resource Settings")]
        [SerializeField] private MMF_Player _shakeFeedbacks;
        [SerializeField] private float _despawnTimerDuration;
        [SerializeField] private float _minShakeFrequency = 2f;
        [SerializeField] private float _maxShakeFrequency = 10f;
        [SerializeField, Range(0f, 1f)] private float _shakeThresholdPercentage = 0.8f;
        
        private Timer _despawnTimer;
        public Timer DespawnTimer => _despawnTimer;
        
        private float _shakeTimer;

        protected override void Awake()
        {
            _despawnTimer = new Timer(_despawnTimerDuration);
            _despawnTimer.OnTimerEnd += OnDespawnTimerFinished;
            
            base.Awake();
        }

        protected void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.UnregisterResource(this);
            }

            if (_despawnTimer != null)
            {
                _despawnTimer.OnTimerEnd -= OnDespawnTimerFinished;
                _despawnTimer = null;
            }
        }
        
        private void Update()
        {
            if (_despawnTimer != null)
            {
                _despawnTimer.Tick(Time.deltaTime);
                HandleShaking();
            }
        }

        public void SetRandomInitialTime(float minTime)
        {
            float setDuration = 0;
        
            if (minTime >= _despawnTimerDuration)
            {
                setDuration = _despawnTimerDuration;
            }
            else
            {
                setDuration = Random.Range(minTime, _despawnTimerDuration);
            }
            
            _despawnTimer.RemainingSeconds = setDuration;
        }

        private void HandleShaking()
        {
            float percentComplete = _despawnTimer.GetPercentComplete();

            if (percentComplete >= _shakeThresholdPercentage)
            {
                // Calculate t from 0 (at threshold) to 1 (at completion)
                float range = 1f - _shakeThresholdPercentage;
                float t = range > 0 ? (percentComplete - _shakeThresholdPercentage) / range : 1f;

                // Interpolate duration: Start slow (Max), end fast (Min)
                float currentShakeDuration = Mathf.Lerp(_maxShakeFrequency, _minShakeFrequency, t);

                _shakeTimer += Time.deltaTime;
                if (_shakeTimer >= currentShakeDuration)
                {
                    _shakeTimer = 0f;
                    _shakeFeedbacks?.PlayFeedbacks();
                }
            }
            else
            {
                _shakeTimer = 0f;
            }
        }

        private void OnDespawnTimerFinished(object sender, System.EventArgs e)
        {
            Collect(false);
        }

        public override void OnHitWithTool(int damage)
        {
            _despawnTimer.Reset();
            _shakeTimer = 0f;
        
            base.OnHitWithTool(damage);
            
        }
    }
}
