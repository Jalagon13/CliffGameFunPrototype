using System.Collections;
using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(FlyingAI))]
    public class BirdNpc : Npc, ITetherReelableNpc
    {
        private enum BirdState
        {
            Patrolling,
            Tethered,
            TetherStunned,
            Fleeing,
            Despawning
        }

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolRadiusHorizontal = 20f;
        [SerializeField] private float _patrolRadiusVertical = 5f;
        [SerializeField] private float _patrolSpeed = 8f;
        [SerializeField] private bool _setPivotToStartPos = true;
        [SerializeField] private float _flightDurationBeforeDespawn = 4f;
        [SerializeField] private float _lifeTime = 60f;
        
        [Header("Tether Settings")]
        [SerializeField] private float _tetherReelStopDistanceFromPlayer = 1.5f;
        [SerializeField] private float _tetherStunDuration = 0.5f;

        private Vector3 _patrolPivot;
        private FlyingAI _flyingAI;
        private BirdState _currentState;
        private float _stateTimer;
        private float _lifeTimeTimer;
        private Coroutine _tetherStunCoroutine;

        public bool CanBeTethered => _currentState == BirdState.Patrolling;
        public float TetherReelStopDistanceFromPlayer => _tetherReelStopDistanceFromPlayer;

        protected override void Awake()
        {
            _flyingAI = GetComponent<FlyingAI>();
            base.Awake();
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
            if (_tetherStunCoroutine != null)
            {
                StopCoroutine(_tetherStunCoroutine);
                _tetherStunCoroutine = null;
            }
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
                case BirdState.Tethered:
                    break;
                case BirdState.TetherStunned:
                    break;
                case BirdState.Fleeing:
                    HandleFleeing();
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
                case BirdState.Tethered:
                    _flyingAI.Stop();
                    break;
                case BirdState.TetherStunned:
                    _flyingAI.Stop();
                    break;
                case BirdState.Fleeing:
                    _flyingAI.SetSpeed(_patrolSpeed);
                    PickNewPatrolPoint();
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

        private void HandleFleeing()
        {
            if (_flyingAI.HasReachedDestination)
            {
                SetState(BirdState.Patrolling);
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

        public bool CatchByTether(Transform spearTransform, bool ignoreStateCheck = false, bool preserveHitOffset = true)
        {
            if (spearTransform == null) return false;
            if (!ignoreStateCheck && !CanBeTethered) return false;

            // Preserve world-space hit offset so the bird stays impaled where it was struck.
            transform.SetParent(spearTransform, preserveHitOffset);
            if (!preserveHitOffset)
            {
                transform.localPosition = Vector3.zero;
            }

            SetState(BirdState.Tethered);
            return true;
        }

        public void ReleaseFromTetherAndFlee()
        {
            if (_currentState != BirdState.Tethered) return;

            transform.SetParent(null, true);
            SetState(BirdState.TetherStunned);

            if (_tetherStunCoroutine != null)
            {
                StopCoroutine(_tetherStunCoroutine);
            }

            _tetherStunCoroutine = StartCoroutine(TetherStunThenFlee());
        }

        private IEnumerator TetherStunThenFlee()
        {
            yield return new WaitForSeconds(_tetherStunDuration);

            if (_currentState == BirdState.TetherStunned)
            {
                SetState(BirdState.Fleeing);
            }

            _tetherStunCoroutine = null;
        }
    }
}
