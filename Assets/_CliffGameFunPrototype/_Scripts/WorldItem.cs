using System;
using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class WorldItem : MonoBehaviour
    {
        [SerializeField]
        float _lerpSpeed = 15f;

        [SerializeField] 
        private float _canCollectTimer = 0.5f;
        
        [SerializeField] private float _initialUpwardForce = 2f;
        [SerializeField] private float _initialHorizontalForce = 0.5f;
        [SerializeField] private float _maxFallDistance = 90f;
        [SerializeField] private float _despawnDuration = 300f;
        
        private ItemSO _item;
        private int _amount;
        private bool _canCollect = false;
        private bool _isCollecting = false;
        private Rigidbody _rb;
        private float _fallStartY;
        private bool _isFalling;
        private Timer _despawnTimer;
        private MeshRenderer _meshRenderer;


        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _despawnTimer = new Timer(_despawnDuration);
            _despawnTimer.OnTimerEnd += HandleDespawn;
        }
        
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(_canCollectTimer);
            _canCollect = true;
        }

        private void OnDestroy()
        {
            if (_despawnTimer != null)
            {
                _despawnTimer.OnTimerEnd -= HandleDespawn;
            }
        }

        private void Update()
        {
            if (_isCollecting) return;

            _despawnTimer.Tick(Time.deltaTime);
            HandleFallTracking();
        }

        private void HandleDespawn(object sender, EventArgs e)
        {
            Debug.Log($"{gameObject.name} despawned after {_despawnDuration} seconds.");
            Destroy(gameObject);
        }
        
        private void HandleFallTracking()
        {
            // Detect the start of a fall (moving downward)
            if (_rb.linearVelocity.y < -0.1f && !_isFalling)
            {
                _isFalling = true;
                _fallStartY = transform.position.y;
            }

            if (_isFalling)
            {
                if (_fallStartY - transform.position.y >= _maxFallDistance)
                {
                    Debug.Log($"{gameObject.name} fell too far! and is now destroyed");
                    Destroy(gameObject);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Reset fall tracking whenever the item touches a surface
            _isFalling = false;
        }
    
        public void Initialize(ItemSO item, int amount)
        {
            _item = item;
            _amount = amount;
            
            gameObject.name = $"{item.InGameName} x{amount}";
            
            if (item.WorldItemModel != null)
            {
                _meshRenderer.enabled = false;
                GameObject spawnedModel = Instantiate(item.WorldItemModel, transform);
                spawnedModel.transform.localPosition = Vector3.zero;

                MonoBehaviour[] scripts = spawnedModel.GetComponentsInChildren<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    Destroy(script);
                }
            }

            SetInitialUpwardForce();
        }

        private void SetInitialUpwardForce()
        {
            Vector3 force = new Vector3(
                UnityEngine.Random.Range(-_initialHorizontalForce, _initialHorizontalForce),
                _initialUpwardForce,
                UnityEngine.Random.Range(-_initialHorizontalForce, _initialHorizontalForce));

            _rb.AddForce(force, ForceMode.Impulse);
        }

        public void CollectItem(ItemCollectCollider itemCollectCollider)
        {
            if (!_canCollect || _isCollecting) return;

            DisablePhysics();
            StartCoroutine(CollectRoutine(itemCollectCollider));
        }
        
        private void DisablePhysics()
        {
            _isCollecting = true;

            // Disable all physics control and gravity
            if (TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            // Disable all colliders so it passes through everything
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
        }

        private IEnumerator CollectRoutine(ItemCollectCollider collector)
        {
            while (collector != null && Vector3.Distance(transform.position, collector.transform.position) > collector.CollectRadius)
            {
                // Smoothly lerp towards the collection point
                transform.position = Vector3.Lerp(transform.position, collector.transform.position, _lerpSpeed * Time.deltaTime);
                yield return null;
            }

            InventoryManager.Instance.AddItem(_item, _amount);
            Destroy(gameObject);
        }
    }
}
