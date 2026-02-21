using UnityEngine;

namespace CliffGame
{
    public class FlyingAI : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _reachThreshold = 1.0f;

        [Header("Rotation Constraints")]
        [SerializeField] private bool _freezeRotationX;
        [SerializeField] private bool _freezeRotationY;
        [SerializeField] private bool _freezeRotationZ;

        private Vector3? _destination;
        public bool HasReachedDestination { get; private set; } = true;
        public bool IsMoving => _destination.HasValue;

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            HasReachedDestination = false;
        }

        public void SetSpeed(float speed)
        {
            _moveSpeed = speed;
        }

        public void Stop()
        {
            _destination = null;
            HasReachedDestination = true;
        }

        private void Update()
        {
            if (_destination.HasValue)
            {
                Vector3 direction = _destination.Value - transform.position;
                float distance = direction.magnitude;

                if (distance <= _reachThreshold)
                {
                    HasReachedDestination = true;
                    _destination = null;
                    return;
                }

                Vector3 moveDirection = direction.normalized;
                transform.position += moveDirection * _moveSpeed * Time.deltaTime;

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    Quaternion nextRotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

                    if (_freezeRotationX || _freezeRotationY || _freezeRotationZ)
                    {
                        Vector3 nextEuler = nextRotation.eulerAngles;
                        Vector3 currentEuler = transform.rotation.eulerAngles;

                        if (_freezeRotationX) nextEuler.x = currentEuler.x;
                        if (_freezeRotationY) nextEuler.y = currentEuler.y;
                        if (_freezeRotationZ) nextEuler.z = currentEuler.z;

                        transform.rotation = Quaternion.Euler(nextEuler);
                    }
                    else
                    {
                        transform.rotation = nextRotation;
                    }
                }
            }
        }
    }
}
