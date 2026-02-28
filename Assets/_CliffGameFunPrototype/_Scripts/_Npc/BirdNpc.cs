using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(FlyingAI))]
    public class BirdNpc : Npc
    {
        private enum BirdState
        {
            Patrolling,
            Caught,
            Despawning
        }

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolRadiusHorizontal = 20f;
        [SerializeField] private float _patrolRadiusVertical = 5f;
        [SerializeField] private float _patrolSpeed = 8f;
        [SerializeField] private bool _setPivotToStartPos = true;
        [SerializeField] private float _flightDurationBeforeDespawn = 4f;
        [SerializeField] private float _lifeTime = 60f;

        private Vector3 _patrolPivot;
        private FlyingAI _flyingAI;
        private BirdState _currentState;
        private float _stateTimer;
        private float _lifeTimeTimer;

        protected override void Awake()
        {
            base.Awake();
            _flyingAI = GetComponent<FlyingAI>();
        }

        private void Start()
        {
            NpcManager.Instance.OnNightRise += OnNightRise;

            if (_setPivotToStartPos)
            {
                _patrolPivot = transform.position;
            }

            SetState(BirdState.Patrolling);
        }

        protected void OnDestroy()
        {
            NpcManager.Instance.OnNightRise -= OnNightRise;
        }

        private void OnNightRise()
        {
            SetState(BirdState.Despawning);
        }

        private void Update()
        {
            if (_currentState == BirdState.Patrolling)
            {
                _lifeTimeTimer += Time.deltaTime;
                if (_lifeTimeTimer >= _lifeTime)
                {
                    SetState(BirdState.Despawning);
                }
            }

            switch (_currentState)
            {
                case BirdState.Patrolling:
                    HandlePatrolling();
                    break;
                case BirdState.Caught:
                    break;
                case BirdState.Despawning:
                    HandleDespawning();
                    break;
            }
        }

        private void HandleDespawning()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= _flightDurationBeforeDespawn)
            {
                Destroy(gameObject);
            }
        }

        private void SetState(BirdState newState)
        {
            if (_currentState == BirdState.Despawning && newState == BirdState.Despawning) return;

            _currentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case BirdState.Patrolling:
                    _flyingAI.SetSpeed(_patrolSpeed);
                    PickNewPatrolPoint();
                    break;
                case BirdState.Caught:
                    _flyingAI.Stop();
                    break;
                case BirdState.Despawning:
                    _flyingAI.SetSpeed(_patrolSpeed);

                    if (Player.Instance != null)
                    {
                        Vector3 directionAway = (transform.position - Player.Instance.transform.position).normalized;
                        // Fly far away in the opposite direction
                        _flyingAI.SetDestination(transform.position + directionAway * 200f);
                    }
                    break;
            }
        }

        private void HandlePatrolling()
        {
            if (_flyingAI.HasReachedDestination)
            {
                PickNewPatrolPoint();
            }
        }

        private void PickNewPatrolPoint()
        {
            Vector3 randomOffset = Random.insideUnitSphere;
            randomOffset.x *= _patrolRadiusHorizontal;
            randomOffset.z *= _patrolRadiusHorizontal;
            randomOffset.y *= _patrolRadiusVertical;

            Vector3 potentialPoint = _patrolPivot + randomOffset;
            
            _flyingAI.SetDestination(potentialPoint);
        }

        public override void OnHitWithTool(int damage)
        {
            _lifeTimeTimer = 0f;
            base.OnHitWithTool(damage);
        }

        public void Catch(Transform hookTransform)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.BirdCaughtSFX, transform.position);

            GetComponent<Collider>().enabled = false; // Prevent further collisions

            transform.SetParent(hookTransform);
            transform.localPosition = Vector3.zero;

            SetState(BirdState.Caught);
        }
    }
}
