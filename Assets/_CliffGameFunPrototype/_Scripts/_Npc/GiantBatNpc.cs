using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(FlyingAI))]
    public class GiantBatNpc : Npc, ITetherReelableNpc
    {
        public enum BatState
        {
            Patrolling,
            Approaching,
            Attacking,
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

        [Header("Attack Settings")]
        [SerializeField] private float _attackIntervalMin = 55f;
        [SerializeField] private float _attackIntervalMax = 105f;
        [SerializeField] private float _attackSpeed = 15f;
        [SerializeField] private float _attackDuration = 5f;
        [SerializeField] private int _damagePerAttackDuration = 50;
        [SerializeField] private int _numberOfClosestPlatformsToConsider = 4;
        [SerializeField] private float _warningDistance = 10f;
        [SerializeField] private int _damageTakenForItToRetreat = 3;
        [SerializeField] protected EventReference _incomingSfx;


        [Header("Morning Flee Settings")]
        [SerializeField] private float _flyingDurationBeforeDespawn = 4f;
        [SerializeField] private float _lifeTime = 60f;
        
        [Header("Tether Settings")]
        [SerializeField] private float _tetherReelStopDistanceFromPlayer = 1.5f;
        [SerializeField] private float _tetherStunDuration = 0.5f;

        private Vector3 _patrolPivot; 
        private FlyingAI _flyingAI;
        private BatState _currentState;
        public BatState State => _currentState;
        
        private float _stateTimer;
        private float _lifeTimeTimer;
        private float _attackCooldownTimer;
        private float _damageAccumulator;
        private BuildPieceDurability _targetPlatform;
        public BuildPieceDurability TargetPlatform => _targetPlatform;
        
        private bool _hasPlayedWarning;
        private int _currentDamageTaken;
        private Coroutine _tetherStunCoroutine;

        public bool CanBeTethered => _currentState == BatState.Patrolling || _currentState == BatState.Attacking;
        public float TetherReelStopDistanceFromPlayer => _tetherReelStopDistanceFromPlayer;

        protected override void Awake()
        {
            base.Awake();
            _flyingAI = GetComponent<FlyingAI>();
        }

        private void Start()
        {
            NpcManager.Instance.OnMorningRise += OnMorningRise;

            if (_setPivotToStartPos)
            {
                _patrolPivot = transform.position;
            }

            _attackCooldownTimer = GetRandomAttackInterval();
            SetState(BatState.Patrolling);
        }

        protected void OnDestroy()
        {
            NpcManager.Instance.OnMorningRise -= OnMorningRise;
            
            if (_tetherStunCoroutine != null)
            {
                StopCoroutine(_tetherStunCoroutine);
                _tetherStunCoroutine = null;
            }

            if (_targetPlatform != null)
            {
                _targetPlatform.IsTargeted = false;
            }
        }

        private void Update()
        {
            if (_currentState == BatState.Patrolling)
            {
                _lifeTimeTimer += Time.deltaTime;
                if (_lifeTimeTimer >= _lifeTime)
                {
                    SetState(BatState.Despawning);
                }
            }

            switch (_currentState)
            {
                case BatState.Patrolling:
                    HandlePatrolling();
                    break;
                case BatState.Approaching:
                    HandleApproaching();
                    break;
                case BatState.Attacking:
                    HandleAttacking();
                    break;
                case BatState.Tethered:
                    break;
                case BatState.TetherStunned:
                    break;
                case BatState.Fleeing:
                    HandleFleeing();
                    break;
                case BatState.Despawning:
                    HandleDespawning();
                    break;
            }
        }

        public override void OnHitWithTool(int damage)
        {
            if (_currentState == BatState.Attacking)
            {
                _currentDamageTaken += damage;
                // Debug.Log($"GiantBat has been hit while attacking. Hits: {_currentHits}/{_numOfHitsForItToFlyAway}");
            }

            _lifeTimeTimer = 0f;

            // Still call the base method to allow it to take damage and eventually be destroyed.
            base.OnHitWithTool(damage);
        }

        private void OnMorningRise()
        {
            Debug.Log($"Morning flee triggered!");
            SetState(BatState.Despawning);
        }

        private void SetState(BatState newState)
        {
            if (_currentState == BatState.Despawning && newState == BatState.Despawning) return;

            // Debug.Log($"GiantBat: Entering state {newState}");
            _currentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case BatState.Patrolling:
                    _flyingAI.SetSpeed(_patrolSpeed);
                    PickNewPatrolPoint();
                    break;
                case BatState.Approaching:
                    _flyingAI.SetSpeed(_attackSpeed);
                    _hasPlayedWarning = false;
                    if (_targetPlatform != null)
                    {
                        _flyingAI.SetDestination(_targetPlatform.transform.position);
                    }
                    else
                    {
                        SetState(BatState.Patrolling);
                    }
                    break;
                case BatState.Attacking:
                    _flyingAI.Stop();
                    _damageAccumulator = 0f;
                    _currentDamageTaken = 0;
                    break;
                case BatState.Tethered:
                    _flyingAI.Stop();
                    if (_targetPlatform != null)
                    {
                        _targetPlatform.IsTargeted = false;
                        _targetPlatform = null;
                    }
                    break;
                case BatState.TetherStunned:
                    _flyingAI.Stop();
                    break;
                case BatState.Fleeing:
                    _flyingAI.SetSpeed(_attackSpeed);
                    if (_targetPlatform != null)
                    {
                        _targetPlatform.IsTargeted = false;
                        _targetPlatform = null;
                    }
                    PickNewPatrolPoint(); // Fly back to patrol area
                    break;
                case BatState.Despawning:
                    _flyingAI.SetSpeed(_attackSpeed);
                    
                    if (_targetPlatform != null)
                    {
                        _targetPlatform.IsTargeted = false;
                        _targetPlatform = null;
                    }
                    
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
            _attackCooldownTimer -= Time.deltaTime;
            if (_attackCooldownTimer <= 0f)
            {
                BuildPieceDurability target = FindTargetPlatform();
                if (target != null)
                {
                    // Debug.Log($"GiantBat: Target found: {target.name}");
                    _targetPlatform = target;
                    _targetPlatform.IsTargeted = true;
                    SetState(BatState.Approaching);
                    return;
                }
                else
                {
                    // Debug.Log("GiantBat: No target found, retrying later.");
                    _attackCooldownTimer = 5f; // Retry later if no target found
                }
            }

            if (_flyingAI.HasReachedDestination)
            {
                // Debug.Log("GiantBat: Reached patrol point, picking new one.");
                PickNewPatrolPoint();
            }
        }

        private void HandleApproaching()
        {
            if (_targetPlatform == null)
            {
                // Debug.Log("GiantBat: Target lost during approach.");
                SetState(BatState.Patrolling);
                return;
            }

            if (!_hasPlayedWarning && Vector3.Distance(transform.position, _targetPlatform.transform.position) <= _warningDistance)
            {
                AudioManager.Instance.PlayOneShot(_incomingSfx, transform.position);
                _hasPlayedWarning = true;
            }

            if (_flyingAI.HasReachedDestination)
            {
                // Debug.Log("GiantBat: Reached target, starting attack.");
                SetState(BatState.Attacking);
            }
        }

        private void HandleAttacking()
        {
            if (_targetPlatform == null)
            {
                // Debug.Log("GiantBat: Target lost during attack.");
                SetState(BatState.Fleeing);
                return;
            }

            if (_currentDamageTaken >= _damageTakenForItToRetreat)
            {
                // Debug.Log("GiantBat: Repelled by player hits!");
                SetState(BatState.Fleeing);
                return;
            }

            _stateTimer += Time.deltaTime;

            // Damage logic
            float damagePerSecond = (float)_damagePerAttackDuration / _attackDuration;
            _damageAccumulator += damagePerSecond * Time.deltaTime;
            if (_damageAccumulator >= 1f)
            {
                int damageToApply = Mathf.FloorToInt(_damageAccumulator);
                // Debug.Log($"GiantBat: Dealing {damageToApply} damage to {_targetPlatform.name}");
                _targetPlatform.AddHp(-damageToApply);

                if (_targetPlatform == null || _targetPlatform.CurrentHitPoints <= 0)
                {
                    // Debug.Log("GiantBat: Target destroyed.");
                    SetState(BatState.Fleeing);
                    return;
                }

                _targetPlatform.TryPlayRattleFeedbacks();
                _damageAccumulator -= damageToApply;
            }

            if (_stateTimer >= _attackDuration)
            {
                // Debug.Log("GiantBat: Attack finished.");
                SetState(BatState.Fleeing);
            }
        }

        private void HandleFleeing()
        {
            if (_flyingAI.HasReachedDestination)
            {
                // Debug.Log("GiantBat: Flee complete, returning to patrol.");
                _attackCooldownTimer = GetRandomAttackInterval();
                SetState(BatState.Patrolling);
            }
        }

        private void HandleDespawning()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= _flyingDurationBeforeDespawn)
            {
                Destroy(gameObject);
            }
        }
        
        private float GetRandomAttackInterval()
        {
            float attackInterval = Random.Range(_attackIntervalMin, _attackIntervalMax);
            Debug.Log($"GiantBat: New attack interval: {attackInterval}");
            return attackInterval;
        }

        private void PickNewPatrolPoint()
        {
            Vector3 randomOffset = Random.insideUnitSphere;
            randomOffset.x *= _patrolRadiusHorizontal;
            randomOffset.z *= _patrolRadiusHorizontal;
            randomOffset.y *= _patrolRadiusVertical;

            Vector3 potentialPoint = _patrolPivot + randomOffset;
            
            // Debug.Log($"GiantBat: Picking new patrol point: {potentialPoint}");
            _flyingAI.SetDestination(potentialPoint);
        }

        private BuildPieceDurability FindTargetPlatform()
        {
            if (BuildPieceIntegrityManager.Instance == null) return null;

            List<BuildPiece> allPieces = BuildPieceIntegrityManager.Instance.RegisteredBuildPieces.ToList();
            List<BuildPieceDurability> outerPieces = new();

            foreach (BuildPiece piece in allPieces)
            {
                int numOfBpdBuildPieces = 0;
                foreach (BuildPiece bp in piece.GetConnectedBuildPieces())
                {
                    if(bp.BuildType == BuildOption.Platform || bp.BuildType == BuildOption.Stairs || bp.BuildType == BuildOption.Fence)
                    {
                        numOfBpdBuildPieces++;
                    }
                }
                
                if (numOfBpdBuildPieces < 4 && piece.TryGetComponent(out BuildPieceDurability durability) && !durability.IsTargeted)
                {
                    outerPieces.Add(durability);
                }
            }

            if (outerPieces.Count == 0) return null;

            var closestPieces = outerPieces
                .OrderBy(p => Vector3.Distance(p.transform.position, transform.position))
                .Take(_numberOfClosestPlatformsToConsider)
                .ToList();

            if (closestPieces.Count == 0) return null;

            return closestPieces[Random.Range(0, closestPieces.Count)];
        }

        public bool CatchByTether(Transform spearTransform, bool ignoreStateCheck = false, bool preserveHitOffset = true)
        {
            if (spearTransform == null) return false;
            if (!ignoreStateCheck && !CanBeTethered) return false;

            // Preserve world-space hit offset so the bat does not snap unnaturally to spear center.
            transform.SetParent(spearTransform, preserveHitOffset);
            if (!preserveHitOffset)
            {
                transform.localPosition = Vector3.zero;
            }

            SetState(BatState.Tethered);
            return true;
        }

        public void ReleaseFromTetherAndFlee()
        {
            if (_currentState != BatState.Tethered) return;

            transform.SetParent(null, true);
            SetState(BatState.TetherStunned);

            if (_tetherStunCoroutine != null)
            {
                StopCoroutine(_tetherStunCoroutine);
            }

            _tetherStunCoroutine = StartCoroutine(TetherStunThenFlee());
        }

        private System.Collections.IEnumerator TetherStunThenFlee()
        {
            yield return new WaitForSeconds(_tetherStunDuration);

            if (_currentState == BatState.TetherStunned)
            {
                SetState(BatState.Fleeing);
            }

            _tetherStunCoroutine = null;
        }
    }
}
