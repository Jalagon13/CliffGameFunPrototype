using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace CliffGame
{
    public class SpearTetherHolder : MonoBehaviour
    {
        [FormerlySerializedAs("_hook")]
        [SerializeField] private GameObject _spearProjectile;
        [SerializeField] private Transform _shootPoint;
        [SerializeField] private float _throwPitchUpDegrees = 20f;
        [SerializeField] private float _reelCompleteDistance = 0.3f;
        [SerializeField] private bool _alignRotationToVelocity = true;
        [SerializeField] private float _velocityAlignSlerpSpeed = 14f;
        [SerializeField] private float _minVelocityForRotation = 0.05f;
        [SerializeField] private float _npcReelDelaySeconds = 0.375f;
        public Transform ShootPoint => _shootPoint;

        public bool SequenceExecuting { get; private set; }
        public bool IsOutboundPhase => SequenceExecuting && !_isReturningPhase;
        private bool _shouldRetractEarly;
        private bool _npcCatchLockedThisSequence;
        private ITetherReelableNpc _pendingNpcCatch;
        private Transform _pendingStabAnchor;
        private Coroutine _pendingNpcCatchCoroutine;
        private bool _isWaitingForNpcReelDelay;
        private bool _isReturningPhase;

        private void Awake()
        {
            _spearProjectile.SetActive(false);
        }

        private void Start()
        {
            SpearTetherManager.Instance.OnSpearTetherRelease += OnSpearTetherRelease;
        }

        private void OnDestroy()
        {
            SpearTetherManager.Instance.OnSpearTetherRelease -= OnSpearTetherRelease;
            ClearPendingNpcCatch();
        }

        private void OnSpearTetherRelease(float chargePercent)
        {
            if (SequenceExecuting || chargePercent <= 0f) return;

            ToolItemSO currentTool = SpearTetherManager.Instance.CurrentSpearTetherTool;
            if (currentTool == null) return;

            InventoryManager.Instance.TryConsumeSelectedToolDurability();

            float duration = currentTool.TetherSequenceDuration;
            float range = currentTool.TetherRange;
            float baseSpeed = range / (duration / 2f);
            float throwSpeed = baseSpeed * chargePercent;
            float gravityMultiplier = Mathf.Max(0f, currentTool.TetherGravityMultiplier);
            Vector3 throwDirection = GetThrowDirection(range);
            StartCoroutine(PerformSpearTetherSequence(duration, chargePercent, baseSpeed, throwSpeed, throwDirection, gravityMultiplier));
        }

        public void RegisterHit()
        {
            _shouldRetractEarly = true;
        }

        public void RequestEarlyRetract()
        {
            if (!SequenceExecuting) return;
            _shouldRetractEarly = true;
        }

        public bool TryQueueNpcCatch(ITetherReelableNpc npc, Transform stabAnchor)
        {
            if (npc == null || !CanQueueNpcCatch()) return false;
            if (!npc.CanBeTethered) return false;

            _npcCatchLockedThisSequence = true;
            _pendingNpcCatch = npc;
            Transform fallbackAnchor = (npc as Component)?.transform;
            _pendingStabAnchor = stabAnchor != null ? stabAnchor : fallbackAnchor;
            StickProjectileToAnchorDuringDelay(_pendingStabAnchor);
            StartPendingNpcCatchDelay();
            return true;
        }

        public bool HasQueuedNpcCatch()
        {
            return _npcCatchLockedThisSequence;
        }

        private IEnumerator PerformSpearTetherSequence(float duration, float chargePercent, float returnSpeed, float throwSpeed, Vector3 throwDirection, float gravityMultiplier)
        {
            SequenceExecuting = true;
            _isReturningPhase = false;
            _spearProjectile.SetActive(true);
            Transform projectileTransform = _spearProjectile.transform;
            _npcCatchLockedThisSequence = false;
            ClearPendingNpcCatch();

            float fullHalfDuration = duration / 2f;
            float actualDuration = fullHalfDuration * chargePercent;
            float timer = 0f;
            _shouldRetractEarly = false;
            Vector3 gravity = Physics.gravity * gravityMultiplier;
            Vector3 velocity = throwDirection * throwSpeed;
            
            // Detach so projectile flight is independent from player/camera movement.
            projectileTransform.SetParent(null, true);
            projectileTransform.position = _shootPoint.position;
            projectileTransform.rotation = _shootPoint.rotation;

            while (timer < actualDuration)
            {
                if (_shouldRetractEarly) break;
                if (_isWaitingForNpcReelDelay)
                {
                    yield return null;
                    continue;
                }

                float deltaTime = Time.deltaTime;
                timer += deltaTime;

                velocity += gravity * deltaTime;
                projectileTransform.position += velocity * deltaTime;
                AlignProjectileRotationToVelocity(projectileTransform, velocity, deltaTime);
                yield return null;
            }

            Vector3 reachPosition = projectileTransform.position;
            float returnDistance = Vector3.Distance(reachPosition, _shootPoint.position);
            float returnDuration = (returnSpeed > 0f) ? returnDistance / returnSpeed : 0.25f;
            returnDuration = Mathf.Max(returnDuration, 0.05f);
            float maxReturnTime = returnDuration * 3f;
            float reelPullAcceleration = returnSpeed * 12f;
            float maxReturnSpeed = returnSpeed * 1.6f;
            float completeDistanceSqr = _reelCompleteDistance * _reelCompleteDistance;
            _isReturningPhase = true;

            TetheredSpearProjectile spearProjectileComponent = _spearProjectile.GetComponent<TetheredSpearProjectile>();
            if (spearProjectileComponent != null && !_npcCatchLockedThisSequence)
            {
                // No NPC catch happened during outbound, so disable NPC catches while returning.
                spearProjectileComponent.SetNpcCatchEnabled(false);
            }

            timer = 0f;

            while (timer < maxReturnTime)
            {
                float deltaTime = Time.deltaTime;
                timer += deltaTime;

                ITetherReelableNpc tetheredNpc = FindTetheredNpcOnProjectile();
                if (tetheredNpc != null && Player.Instance != null)
                {
                    Transform tetheredNpcTransform = (tetheredNpc as Component)?.transform;
                    if (tetheredNpcTransform != null)
                    {
                        float distanceToPlayer = Vector3.Distance(tetheredNpcTransform.position, Player.Instance.transform.position);
                        if (distanceToPlayer <= tetheredNpc.TetherReelStopDistanceFromPlayer)
                        {
                            break;
                        }
                    }
                }

                Vector3 toShootPoint = _shootPoint.position - projectileTransform.position;
                if (toShootPoint.sqrMagnitude <= completeDistanceSqr)
                {
                    break;
                }

                Vector3 reelAcceleration = toShootPoint.normalized * reelPullAcceleration;
                velocity += (gravity + reelAcceleration) * deltaTime;

                if (velocity.sqrMagnitude > maxReturnSpeed * maxReturnSpeed)
                {
                    velocity = velocity.normalized * maxReturnSpeed;
                }

                projectileTransform.position += velocity * deltaTime;
                // AlignProjectileRotationToVelocity(projectileTransform, velocity, deltaTime);
                yield return null;
            }

            Npc[] attachedNpcs = _spearProjectile.GetComponentsInChildren<Npc>();
            foreach (Npc npc in attachedNpcs)
            {
                if (npc is ITetherReelableNpc tetheredNpc)
                {
                    tetheredNpc.ReleaseFromTetherAndFlee();
                }
            }

            projectileTransform.position = _shootPoint.position;
            projectileTransform.SetParent(_shootPoint, false);
            projectileTransform.localPosition = Vector3.zero;
            projectileTransform.localRotation = Quaternion.identity;

            _spearProjectile.SetActive(false);
            SequenceExecuting = false;
            _isReturningPhase = false;

            _npcCatchLockedThisSequence = false;
            ClearPendingNpcCatch();
        }

        private void AlignProjectileRotationToVelocity(Transform projectileTransform, Vector3 velocity, float deltaTime)
        {
            if (!_alignRotationToVelocity) return;
            if (velocity.sqrMagnitude < _minVelocityForRotation * _minVelocityForRotation) return;

            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            projectileTransform.rotation = Quaternion.Slerp(
                projectileTransform.rotation,
                targetRotation,
                _velocityAlignSlerpSpeed * deltaTime
            );
        }

        private Vector3 GetThrowDirection(float aimDistance)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return _shootPoint.forward;
            }

            Transform camTransform = mainCamera.transform;

            // Start from camera aim + pitch adjustment.
            Vector3 cameraAimedForward = Quaternion.AngleAxis(_throwPitchUpDegrees, camTransform.right) * camTransform.forward;
            cameraAimedForward.Normalize();

            // Convert camera aim into a target point, then derive direction from shoot point.
            // This compensates for lateral offset between camera and projectile spawn point.
            Vector3 aimedPoint = camTransform.position + cameraAimedForward * Mathf.Max(aimDistance, 0.1f);
            Vector3 correctedDirection = aimedPoint - _shootPoint.position;

            if (correctedDirection.sqrMagnitude <= 0.0001f)
            {
                return cameraAimedForward;
            }

            return correctedDirection.normalized;
        }

        private bool CanQueueNpcCatch()
        {
            return SequenceExecuting && !_isReturningPhase && !_npcCatchLockedThisSequence;
        }

        private void StartPendingNpcCatchDelay()
        {
            if (_pendingNpcCatchCoroutine != null)
            {
                StopCoroutine(_pendingNpcCatchCoroutine);
            }

            _pendingNpcCatchCoroutine = StartCoroutine(DelayThenAttachCaughtNpcAndReel());
        }

        private IEnumerator DelayThenAttachCaughtNpcAndReel()
        {
            yield return new WaitForSeconds(_npcReelDelaySeconds);

            _pendingNpcCatchCoroutine = null;

            if (!SequenceExecuting)
            {
                ClearPendingNpcCatch();
                yield break;
            }

            _isWaitingForNpcReelDelay = false;
            _spearProjectile.transform.SetParent(null, true);

            bool attachedNpc = false;
            if (_pendingNpcCatch != null)
            {
                attachedNpc = _pendingNpcCatch.CatchByTether(_spearProjectile.transform, true);
            }

            _pendingNpcCatch = null;
            _pendingStabAnchor = null;

            if (attachedNpc)
            {
                _shouldRetractEarly = true;
            }
            else
            {
                _npcCatchLockedThisSequence = false;
            }
        }

        private void ClearPendingNpcCatch()
        {
            if (_pendingNpcCatchCoroutine != null)
            {
                StopCoroutine(_pendingNpcCatchCoroutine);
                _pendingNpcCatchCoroutine = null;
            }

            _pendingNpcCatch = null;
            _pendingStabAnchor = null;
            _isWaitingForNpcReelDelay = false;
        }

        private void StickProjectileToAnchorDuringDelay(Transform anchorTransform)
        {
            if (anchorTransform == null) return;

            _isWaitingForNpcReelDelay = true;
            _spearProjectile.transform.SetParent(anchorTransform, true);
        }

        private ITetherReelableNpc FindTetheredNpcOnProjectile()
        {
            Npc[] attachedNpcs = _spearProjectile.GetComponentsInChildren<Npc>();
            foreach (Npc npc in attachedNpcs)
            {
                if (npc is ITetherReelableNpc tetheredNpc)
                {
                    return tetheredNpc;
                }
            }

            return null;
        }
    }
}
