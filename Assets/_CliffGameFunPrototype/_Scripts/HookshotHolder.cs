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

            Vector3 targetPoint = Camera.main.transform.position + (Camera.main.transform.forward * range);
            Debug.Log($"Shooting hook with charge percent: {chargePercent}, duration: {duration}, range: {range}");
            StartCoroutine(PerformHookSequence(targetPoint, duration, chargePercent));
        }

        private IEnumerator PerformHookSequence(Vector3 targetPosition, float duration, float chargePercent)
        {
            HookSequenceExecuting = true;
            _hook.SetActive(true);

            float fullHalfDuration = duration / 2f;
            float actualDuration = fullHalfDuration * chargePercent;
            float timer = 0f;
            Vector3 startPosition = _shootPoint.position;

            while (timer < actualDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fullHalfDuration;
                _hook.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            Vector3 reachPosition = _hook.transform.position;
            
            timer = 0f;

            while (timer < actualDuration)
            {
                timer += Time.deltaTime;
                float t = timer / actualDuration;
                _hook.transform.position = Vector3.Lerp(reachPosition, _shootPoint.position, t);
                yield return null;
            }

            _hook.SetActive(false);
            HookSequenceExecuting = false;
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
