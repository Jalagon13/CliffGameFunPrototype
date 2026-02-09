using UnityEngine;

namespace CliffGame
{
    public class BirdResource : MonoBehaviour
    {
        [SerializeField] private float _minSpeed = 1.25f;
        [SerializeField] private float _maxSpeed = 2.25f;
        [SerializeField] private float _travelDuration = 30f;
        [SerializeField] private float _maxAngleDeviation = 15f;
        [SerializeField] private float _detectionDistance = 2f;
        [SerializeField] private LayerMask _obstacleLayerMask;

        [SerializeField] private ItemSO _itemToDrop;

        private float _speed;
        private float _timer;
        private Vector3 _direction;
        private bool _hasBeenInitialized;

        private void Update()
        {
            if (!_hasBeenInitialized) return;

            if (Physics.Raycast(transform.position, _direction, _detectionDistance, _obstacleLayerMask))
            {
                _direction = -_direction;
                Vector3 localScale = transform.GetChild(0).transform.localScale;
                localScale.z *= -1;
                transform.GetChild(0).transform.localScale = localScale;
            }

            _timer += Time.deltaTime;

            if (_timer < _travelDuration)
            {
                transform.position += _direction * _speed * Time.deltaTime;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(bool moveLeft)
        {
            _speed = Random.Range(_minSpeed, _maxSpeed);

            Vector3 baseDirection = moveLeft ? Vector3.left : Vector3.right;

            float randomAngleZ = Random.Range(-_maxAngleDeviation, _maxAngleDeviation);
            Quaternion randomRotation = Quaternion.Euler(0f, 0f, randomAngleZ);
            _direction = (randomRotation * baseDirection).normalized;

            _hasBeenInitialized = true;

            transform.GetChild(0).transform.localScale = new(1, 1, moveLeft ? -1 : 1);
        }

        public void Catch(Transform hookTransform)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.BirdCaughtSFX, transform.position);
        
            _hasBeenInitialized = false; // Stop movement logic
            GetComponent<Collider>().enabled = false; // Prevent further collisions
            
            transform.SetParent(hookTransform);
            transform.localPosition = Vector3.zero;
        }

        public void Collect()
        {
            if (_itemToDrop != null)
            {
                InventoryManager.Instance.AddItem(_itemToDrop, 1);
            }
            Destroy(gameObject);
        }
    }
}
