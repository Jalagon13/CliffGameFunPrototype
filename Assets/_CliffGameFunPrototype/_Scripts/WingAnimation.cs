using UnityEngine;

namespace CliffGame
{
    public class WingAnimation : MonoBehaviour
    {
        [SerializeField]
        private float _maxZRotationDegrees = 30f;

        [SerializeField]
        private float _cycleDuration = 1f;

        [SerializeField]
        private bool _startAtPositiveRotation = true;

        private float _timer;
        private float _originalZ;

        private void Start()
        {
            _originalZ = transform.localEulerAngles.z;
            _timer = _startAtPositiveRotation ? _cycleDuration : 0f;
        }

        private void Update()
        {
            if (_cycleDuration <= 0f)
                return;

            _timer += Time.deltaTime;

            float t = Mathf.PingPong(_timer, _cycleDuration) / _cycleDuration;
            float zRotation = Mathf.Lerp(-_maxZRotationDegrees, _maxZRotationDegrees, t);

            Vector3 currentEuler = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, _originalZ + zRotation);
        }
    }
}
