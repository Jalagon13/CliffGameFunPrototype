using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance;

        [Header("Sampling")]
        [SerializeField]
        private GameObject _resourceSamplingQuad;

        [SerializeField]
        private LayerMask _cliffLayerMask;

        [Header("Spawn Tuning")]
        [Tooltip("Minimum distance allowed between resources")]
        [SerializeField]
        private float _minSpawnDistance = 2f;

        [Tooltip("Minimum distance allowed between resources and player objects")]
        [SerializeField]
        private float _minSpawnDistanceFromPlayerObjects = 0.15f;

        [Header("Initial Spawn")]
        [Tooltip("How many resources to try to spawn at game start")]
        [SerializeField]
        private int _initialSpawnCount = 15;

        [Tooltip("Minimum remaining seconds for initially spawned resources")]
        [SerializeField]
        private float _initialSpawnMinRemainingTime = 5f;

        [Tooltip("Maximum number of active resources allowed")]
        [SerializeField]
        private int _maxResources = 30;

        [Tooltip("Seconds between spawn attempts")]
        [SerializeField]
        private float _spawnAttemptInterval = 3f;

        [Tooltip("How many random samples to try per spawn attempt")]
        [SerializeField]
        private int _attemptsPerTick = 5;

        [SerializeField] 
        private List<WeightedResource> _resources;

        private readonly List<NaturalResource> _activeResources = new List<NaturalResource>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitialSpawn();
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_spawnAttemptInterval);

                if (_activeResources.Count >= _maxResources)
                    continue;

                TrySpawnResource(false);
            }
        }

        private void InitialSpawn()
        {
            int spawned = 0;
            int safetyIterations = _initialSpawnCount * 5;

            while (spawned < _initialSpawnCount && safetyIterations > 0)
            {
                int beforeCount = _activeResources.Count;
                TrySpawnResource(true);

                if (_activeResources.Count > beforeCount)
                    spawned++;

                safetyIterations--;
            }
        }

        private void TrySpawnResource(bool isInitialSpawn = false)
        {
            Vector3 bestPoint = Vector3.zero;
            Vector3 bestNormal = Vector3.zero;
            float bestDistance = -1f;
            bool candidateFound = false;

            for (int i = 0; i < _attemptsPerTick; i++)
            {
                Vector3 worldSamplePoint = GetRandomPointOnQuad();
                Ray ray = new Ray(worldSamplePoint, -_resourceSamplingQuad.transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _cliffLayerMask))
                {
                    if (IsOverlappingWithPlaceable(hit.point))
                        continue;

                    float dist = GetDistanceToNearestResource(hit.point);

                    if (dist < _minSpawnDistance)
                        continue;

                    if (dist > bestDistance)
                    {
                        bestDistance = dist;
                        bestPoint = hit.point;
                        bestNormal = hit.normal;
                        candidateFound = true;
                    }
                }
            }

            if (candidateFound)
            {
                SpawnResourceAt(bestPoint, bestNormal, isInitialSpawn);
            }
        }

        private Vector3 GetRandomPointOnQuad()
        {
            MeshFilter meshFilter = _resourceSamplingQuad.GetComponent<MeshFilter>();
            Vector3 size = meshFilter.sharedMesh.bounds.size;

            float x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float y = Random.Range(-size.y * 0.5f, size.y * 0.5f);

            Vector3 localPoint = new Vector3(x, y, 0f);
            return _resourceSamplingQuad.transform.TransformPoint(localPoint);
        }

        private float GetDistanceToNearestResource(Vector3 point)
        {
            float minDistance = float.MaxValue;

            foreach (Resource resource in _activeResources)
            {
                if (resource == null)
                    continue;

                float dist = Vector3.Distance(point, resource.transform.position);
                if (dist < minDistance)
                    minDistance = dist;
            }

            return minDistance;
        }

        private bool IsOverlappingWithPlaceable(Vector3 point)
        {
            Collider[] colliders = Physics.OverlapSphere(point, _minSpawnDistanceFromPlayerObjects);
            foreach (Collider collider in colliders)
            {
                if (collider.transform.root.CompareTag("Placeable") || collider.transform.root.CompareTag("BuildPiece"))
                {
                    return true;
                }
            }
            return false;
        }

        private void SpawnResourceAt(Vector3 position, Vector3 normal, bool isInitialSpawn = false)
        {
            NaturalResource resource = Instantiate(WeightedResourceSelector.GetRandomResource(_resources), position, Quaternion.LookRotation(normal), transform);
            // Debug.Log($"{_activeResources.Count}/{_maxResources}: Spawned resource {resource.name} at {position}");
            
            if (isInitialSpawn)
            {
                resource.SetRandomInitialTime(_initialSpawnMinRemainingTime);
            }
            
            _activeResources.Add(resource);
        }

        public void UnregisterResource(NaturalResource resource)
        {
            if (_activeResources.Contains(resource))
            {
                _activeResources.Remove(resource);
                // Debug.Log($"{_activeResources.Count}/{_maxResources}: Unregistered resource {resource.name}");
            }
        }
    }
}
