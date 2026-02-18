using System;
using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public enum BirdState { Patrolling, Approaching, Latched, Fleeing }

    public class BirdController : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _hitsRequiredToRepel = 5;
        [SerializeField] private float _latchDuration = 10f; // How long before platform breaks
        [SerializeField] private float _latchModelSwitchDistance = 10f;
        [SerializeField] private ToolType _hitToolType;

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolSpeed = 8f;
        [SerializeField] private float _attackSpeed = 15f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private Transform[] _patrolWaypoints;

        [Header("Visual Models")]
        [SerializeField] private GameObject _patrollingModel;
        [SerializeField] private GameObject _approachingModel;
        [SerializeField] private GameObject _latchedModel;
        [SerializeField] private GameObject _fleeingModel;

        private BirdState _currentState;
        private int _currentHits;
        public bool IsAttacking { get; private set; }
        
        private Coroutine _activeBehavior;
        private int _currentWaypointIndex = 0;

        public ToolType BreakToolType => _hitToolType;

        private void Start()
        {
            if (_patrolWaypoints != null && _patrolWaypoints.Length > 0)
            {
                transform.position = _patrolWaypoints[0].position;
                StartPatrolling();
            }
        }

        public void StartPatrolling()
        {
            if (_activeBehavior != null) StopCoroutine(_activeBehavior);
            _activeBehavior = StartCoroutine(PatrolRoutine());
        }

        private IEnumerator PatrolRoutine()
        {
            SetState(BirdState.Patrolling);
            Debug.Log($"Patrol routine started");
            
            if (_patrolWaypoints == null || _patrolWaypoints.Length == 0) yield break;

            while (true)
            {
                Transform targetWaypoint = _patrolWaypoints[_currentWaypointIndex];
                
                // Fly to waypoint
                yield return FlyTo(targetWaypoint.position, _patrolSpeed);

                // Update index for next time
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _patrolWaypoints.Length;
            }
        }

        public void OnHitWithTool()
        {
            if (Player.Instance.ToolHolder.CurrentHeldTool.ToolType != _hitToolType) return;

            if (_currentState == BirdState.Latched)
            {
                _currentHits++;
                Debug.Log($"SMACK! Current hits: {_currentHits} out of {_hitsRequiredToRepel} needed to repel.");
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.MonsterBirdHurt, transform.position);
                // Play "Squawk" sound
                // Spawn feather particles
            }
        }

        public void SetState(BirdState newState)
        {
            _currentState = newState;
            Debug.Log($"Bird state changed to: {_currentState}");
            
            UpdateVisuals(_currentState);
        }

        private void UpdateVisuals(BirdState state)
        {
            if (_patrollingModel != null) _patrollingModel.SetActive(false);
            if (_approachingModel != null) _approachingModel.SetActive(false);
            if (_latchedModel != null) _latchedModel.SetActive(false);
            if (_fleeingModel != null) _fleeingModel.SetActive(false);

            switch (state)
            {
                case BirdState.Patrolling:
                    if (_patrollingModel != null) _patrollingModel.SetActive(true);
                    break;
                case BirdState.Approaching:
                    if (_approachingModel != null) _approachingModel.SetActive(true);
                    break;
                case BirdState.Latched:
                    if (_latchedModel != null) _latchedModel.SetActive(true);
                    break;
                case BirdState.Fleeing:
                    if (_fleeingModel != null) _fleeingModel.SetActive(true);
                    break;
            }
        }

        // Called by the Manager to start the sequence
        public IEnumerator ExecuteAttackSequence(BuildPieceDurability target)
        {
            if (IsAttacking) yield break;
            IsAttacking = true;

            // Interrupt patrol
            if (_activeBehavior != null) StopCoroutine(_activeBehavior);

            // Phase 1: Approach
            SetState(BirdState.Approaching);
            // MoveTo(target.transform.position)... wait until arrived
            Debug.Log($"Moving to target platform: {target.gameObject.name} {target.transform.position}");
            yield return FlyTo(target.transform.position, _attackSpeed);
            Debug.Log($"Arrived at target platform: {target.gameObject.name} {target.transform.position}");

            // Phase 2: Latch and Pry
            SetState(BirdState.Latched);
            _currentHits = 0;
            float timer = 0f;

            if (target != null)
            {
                Debug.Log($"Trying to pry off platform {target.name}...");
                float damagePerSecond = (float)target.MaxHitPoints / _latchDuration;
                float accumulatedDamage = 0f;

                // Wait while prying, checking for player hits or timeout
                while (timer < _latchDuration)
                {
                    if (target == null) break;

                    timer += Time.deltaTime;

                    // Apply damage
                    accumulatedDamage += damagePerSecond * Time.deltaTime;
                    if (accumulatedDamage >= 1f)
                    {
                        int damageToApply = Mathf.FloorToInt(accumulatedDamage);
                        target.AddHp(-damageToApply);
                        target.TryPlayRattleFeedbacks();
                        accumulatedDamage -= damageToApply;
                    }

                    if (target == null || target.CurrentHitPoints <= 0)
                    {
                        Debug.Log("Platform destroyed by bird!");
                        break;
                    }

                    if (_currentHits >= _hitsRequiredToRepel)
                    {
                        Debug.Log("Bird repelled!");
                        break; // Player saved the platform!
                    }
                    yield return null;
                }
            }

            // Phase 4: Flee
            SetState(BirdState.Fleeing);
            // Fly away to a safe patrol point
            Debug.Log($"Flying away...");

            // Return to the waypoint we were heading to before interruption
            Transform returnPoint = _patrolWaypoints[_currentWaypointIndex];
            yield return FlyTo(returnPoint.position, _attackSpeed);
            
            Debug.Log($"Has flown away");

            IsAttacking = false;
            
            // Resume patrol
            StartPatrolling();

            // Return control to the Manager loop
        }

        private IEnumerator FlyTo(Vector3 targetPosition, float speed)
        {
            Vector3 startPos = transform.position;
            float distance = Vector3.Distance(startPos, targetPosition);
            float duration = distance / speed;
            float elapsed = 0f;

            if (duration <= 0.01f)
            {
                transform.position = targetPosition;
                yield break;
            }

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / duration);
                
                // Face direction
                Vector3 direction = (targetPosition - startPos).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _rotationSpeed);
                }

                if (_currentState == BirdState.Approaching)
                {
                    if (Vector3.Distance(transform.position, targetPosition) <= _latchModelSwitchDistance)
                    {
                        if (_approachingModel != null && _approachingModel.activeSelf)
                        {
                            _approachingModel.SetActive(false);
                            if (_latchedModel != null)
                            {
                                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.MonsterBirdWarning, transform.position);
                                _latchedModel.SetActive(true);
                            }
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
        }

        public void OnInteractWith()
        {
            
        }

        // ... Movement coroutines (MoveToTargetRoutine, FlyAwayRoutine) go here
    }
}
