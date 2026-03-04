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
        private bool _shouldRetractEarly;
        private bool _npcCatchLockedThisSequence;
        private BirdNpc _pendingBirdCatch;
        private GiantBatNpc _pendingBatCatch;
        private Coroutine _pendingNpcCatchCoroutine;
        private bool _isWaitingForNpcReelDelay;

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

        public bool TryQueueBirdCatch(BirdNpc bird)
        {
            if (bird == null || !CanQueueNpcCatch()) return false;

            _npcCatchLockedThisSequence = true;
            _pendingBirdCatch = bird;
            StickProjectileToNpcDuringDelay(bird.transform);
            StartPendingNpcCatchDelay();
            return true;
        }

        public bool TryQueueGiantBatCatch(GiantBatNpc bat)
        {
            if (bat == null || !CanQueueNpcCatch()) return false;
            if (!bat.CanBeTethered) return false;

            _npcCatchLockedThisSequence = true;
            _pendingBatCatch = bat;
            StickProjectileToNpcDuringDelay(bat.transform);
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

            timer = 0f;

            while (timer < maxReturnTime)
            {
                float deltaTime = Time.deltaTime;
                timer += deltaTime;

                GiantBatNpc tetheredBat = _spearProjectile.GetComponentInChildren<GiantBatNpc>();
                if (tetheredBat != null && Player.Instance != null)
                {
                    float distanceToPlayer = Vector3.Distance(tetheredBat.transform.position, Player.Instance.transform.position);
                    if (distanceToPlayer <= tetheredBat.TetherReelStopDistanceFromPlayer)
                    {
                        break;
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

            GiantBatNpc[] tetheredBats = _spearProjectile.GetComponentsInChildren<GiantBatNpc>();
            foreach (GiantBatNpc bat in tetheredBats)
            {
                bat.ReleaseFromTetherAndFlee();
            }

            projectileTransform.position = _shootPoint.position;
            projectileTransform.SetParent(_shootPoint, false);
            projectileTransform.localPosition = Vector3.zero;
            projectileTransform.localRotation = Quaternion.identity;

            _spearProjectile.SetActive(false);
            SequenceExecuting = false;

            BirdNpc[] caughtBirds = _spearProjectile.GetComponentsInChildren<BirdNpc>();
            foreach (BirdNpc bird in caughtBirds)
            {
                bird.Collect();
            }

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
            return SequenceExecuting && !_npcCatchLockedThisSequence;
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

            if (_pendingBirdCatch != null)
            {
                _pendingBirdCatch.Catch(_spearProjectile.transform);
                attachedNpc = true;
            }
            else if (_pendingBatCatch != null)
            {
                attachedNpc = _pendingBatCatch.CatchByTether(_spearProjectile.transform, true);
            }

            _pendingBirdCatch = null;
            _pendingBatCatch = null;

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

            _pendingBirdCatch = null;
            _pendingBatCatch = null;
            _isWaitingForNpcReelDelay = false;
        }

        private void StickProjectileToNpcDuringDelay(Transform npcTransform)
        {
            if (npcTransform == null) return;

            _isWaitingForNpcReelDelay = true;
            _spearProjectile.transform.SetParent(npcTransform, true);
        }
    }
}
