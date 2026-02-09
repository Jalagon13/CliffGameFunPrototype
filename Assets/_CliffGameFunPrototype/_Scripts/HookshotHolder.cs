using System;
using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class HookshotHolder : MonoBehaviour
    {
        [SerializeField] private GameObject _hook;
        [SerializeField] private Transform _shootPoint;
        public Transform ShootPoint => _shootPoint;
    
        private GameObject _hookshotModel;
        
        public bool HookSequenceExecuting { get; private set; }
        private bool _shouldRetractEarly;
        
        private void Awake()
        {
            _hookshotModel = transform.GetChild(0).gameObject;
            _hookshotModel.SetActive(false);
            _hook.SetActive(false);
        }
    
        private void Start()
        {
            HookshotManager.Instance.OnHookshotEquipped += OnHookshotEquipped;
            HookshotManager.Instance.OnHookshotUnequipped += OnHookshotUnequipped;
            HookshotManager.Instance.OnHookshotRelease += OnHookshotRelease;
        }
        
        private void OnDestroy()
        {
            HookshotManager.Instance.OnHookshotEquipped -= OnHookshotEquipped;
            HookshotManager.Instance.OnHookshotUnequipped -= OnHookshotUnequipped;
            HookshotManager.Instance.OnHookshotRelease -= OnHookshotRelease;
        }

        private void OnHookshotRelease(float chargePercent)
        {
            if (HookSequenceExecuting || chargePercent <= 0f) return;

            HookshotItemSO currentHook = HookshotManager.Instance.CurrentHookshot;
            if (currentHook == null) return;

            float duration = currentHook.HookSequenceDuration;
            float range = currentHook.HookRange;
            float speed = range / (duration / 2f);

            Vector3 targetPoint = Camera.main.transform.position + (Camera.main.transform.forward * range);
            Debug.Log($"Shooting hook with charge percent: {chargePercent}, duration: {duration}, range: {range}");
            StartCoroutine(PerformHookSequence(targetPoint, duration, chargePercent, speed));
        }

        public void RegisterHit()
        {
            _shouldRetractEarly = true;
        }

        private IEnumerator PerformHookSequence(Vector3 targetPosition, float duration, float chargePercent, float speed)
        {
            HookSequenceExecuting = true;
            _hook.SetActive(true);

            float fullHalfDuration = duration / 2f;
            float actualDuration = fullHalfDuration * chargePercent;
            float timer = 0f;
            _shouldRetractEarly = false;
            Vector3 startPosition = _shootPoint.position;

            while (timer < actualDuration)
            {
                if (_shouldRetractEarly) break;

                timer += Time.deltaTime;
                float t = timer / fullHalfDuration;
                _hook.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            Vector3 reachPosition = _hook.transform.position;
            float returnDistance = Vector3.Distance(reachPosition, _shootPoint.position);
            float returnDuration = (speed > 0f) ? returnDistance / speed : 0.25f;
            returnDuration = Mathf.Max(returnDuration, 0.05f);
            
            timer = 0f;

            while (timer < returnDuration)
            {
                timer += Time.deltaTime;
                float t = timer / returnDuration;
                _hook.transform.position = Vector3.Lerp(reachPosition, _shootPoint.position, t);
                yield return null;
            }

            _hook.SetActive(false);
            HookSequenceExecuting = false;

            // Check if we caught a bird
            BirdResource[] caughtBirds = _hook.GetComponentsInChildren<BirdResource>();
            foreach (BirdResource bird in caughtBirds)
            {
                bird.Collect();
            }
        }

        private void OnHookshotEquipped()
        {
            _hookshotModel.SetActive(true);
        }

        private void OnHookshotUnequipped()
        {
            _hookshotModel.SetActive(false);
        }
    }
}
