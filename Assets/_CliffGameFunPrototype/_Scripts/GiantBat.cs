using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(FlyingAI))]
    public class GiantBat : Resource
    {
        private enum BatState
        {
            Patrolling,
            Approaching,
            Attacking,
            Fleeing,
            MorningFlee
        }

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolRadiusHorizontal = 20f;
        [SerializeField] private float _patrolRadiusVertical = 5f;
        [SerializeField] private float _patrolSpeed = 8f;
        [SerializeField] private bool _setPivotToStartPos = true;

        [Header("Attack Settings")]
        [SerializeField] private float _attackInterval = 30f;
        [SerializeField] private float _attackSpeed = 15f;
        [SerializeField] private float _attackDuration = 5f;
        [SerializeField] private int _damagePerAttackDuration = 50;
        [SerializeField] private int _numberOfClosestPlatformsToConsider = 4;
        [SerializeField] private float _warningDistance = 10f;
        [SerializeField] private int _numOfHitsForItToFlyAway = 3;

        [Header("Morning Flee Settings")]
        [SerializeField] private float _morningFleeDuration = 4f;

        private Vector3 _patrolPivot; 
        private FlyingAI _flyingAI;
        private BatState _currentState;
        private float _stateTimer;
        private float _attackCooldownTimer;
        private float _damageAccumulator;
        private BuildPieceDurability _targetPlatform;
        private bool _hasPlayedWarning;
        private int _currentHits;

        protected override void Awake()
        {
            base.Awake();
            _flyingAI = GetComponent<FlyingAI>();
        }

        protected override void OnDestroy()
        {
            if (NpcManager.Instance != null)
            {
                NpcManager.Instance.OnMorningRise -= OnMorningRise;
            }

            if (_targetPlatform != null)
            {
                _targetPlatform.IsTargeted = false;
            }
            base.OnDestroy();
        }

        private void Start()
        {
            if (NpcManager.Instance != null)
            {
                NpcManager.Instance.OnMorningRise += OnMorningRise;
            }

            if (_setPivotToStartPos)
            {
                _patrolPivot = transform.position;
            }

            _attackCooldownTimer = _attackInterval;
            SetState(BatState.Patrolling);
        }

        private void Update()
        {
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
                case BatState.Fleeing:
                    HandleFleeing();
                    break;
                case BatState.MorningFlee:
                    HandleMorningFlee();
                    break;
            }
        }

        public override void OnHitWithTool()
        {
            if (Player.Instance.ToolHolder.CurrentHeldTool.ToolType == BreakToolType && _currentState == BatState.Attacking)
            {
                _currentHits++;
                // Debug.Log($"GiantBat has been hit while attacking. Hits: {_currentHits}/{_numOfHitsForItToFlyAway}");
            }
        
            // Still call the base method to allow it to take damage and eventually be destroyed.
            base.OnHitWithTool();
        }

        private void OnMorningRise()
        {
            Debug.Log($"Morning flee triggered!");
            SetState(BatState.MorningFlee);
        }

        private void SetState(BatState newState)
        {
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
                    _currentHits = 0;
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
                case BatState.MorningFlee:
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
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.GiantBatWarningSFX, transform.position);
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

            if (_currentHits >= _numOfHitsForItToFlyAway)
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
                _attackCooldownTimer = _attackInterval;
                SetState(BatState.Patrolling);
            }
        }

        private void HandleMorningFlee()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= _morningFleeDuration)
            {
                Destroy(gameObject);
            }
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
                int neighborCount = piece.GetConnectedBuildPieces().Count();
                
                if (neighborCount < 4 && !piece.IsAnchored && piece.TryGetComponent(out BuildPieceDurability durability) && !durability.IsTargeted)
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
    }
}
