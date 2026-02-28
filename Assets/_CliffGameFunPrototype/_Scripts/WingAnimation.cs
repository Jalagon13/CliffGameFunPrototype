using System;
using UnityEngine;

namespace CliffGame
{
    public class WingAnimation : MonoBehaviour
    {
        public Action OnWingDown;
    
        [SerializeField]
        private float _maxZRotationDegrees = 30f;

        [SerializeField]
        private float _cycleDuration = 1f;

        [SerializeField]
        private bool _startAtPositiveRotation = true;

        private float _timer;
        private float _originalZ;
        private int _lastCycleCount;

        private void Start()
        {
            _originalZ = transform.localEulerAngles.z;
            _timer = _startAtPositiveRotation ? _cycleDuration : 0f;
            _lastCycleCount = Mathf.FloorToInt(_timer / _cycleDuration);
        }

        private void Update()
        {
            if (_cycleDuration <= 0f)
                return;

            _timer += Time.deltaTime;

            int currentCycleCount = Mathf.FloorToInt(_timer / _cycleDuration);
            if (currentCycleCount > _lastCycleCount)
            {
                _lastCycleCount = currentCycleCount;
                if(_startAtPositiveRotation && transform.localRotation.z > 0f)
                {
                    OnWingDown?.Invoke();
                }
            }

            float t = Mathf.PingPong(_timer, _cycleDuration) / _cycleDuration;
            float zRotation = Mathf.Lerp(-_maxZRotationDegrees, _maxZRotationDegrees, t);

            Vector3 currentEuler = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, _originalZ + zRotation);
        }
    }
}
