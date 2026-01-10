using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    // TODO: Brainstorm how platforms should interact with the world

    public class BirdSpawner : MonoBehaviour
    {
        [SerializeField] private Terrain _terrain;
        [SerializeField] private BirdResource _birdResourcePrefab;
        [SerializeField] private int _maxSparrows = 10;
        [SerializeField] private float _spawningPlaneSideLength = 20f;
        [SerializeField] private float _spawningPlaneDistanceFromPlayer = 15f;
        [SerializeField] private float _obstacleAvoidanceRadius = 2f;
        [SerializeField] private int _ticksPerSecond = 60;
        [SerializeField, Range(0f, 0.25f)] private float _spawnChancePerTick = 0.02f; // 2% chance per tick

        private readonly List<BirdResource> _activeSparrows = new();

        private void Start()
        {
            InvokeRepeating(nameof(Tick), 0f, 1f / _ticksPerSecond);
        }

        private void Tick()
        {
            _activeSparrows.RemoveAll(b => b == null);

            if (_activeSparrows.Count >= _maxSparrows)
                return;

            if (Random.value > _spawnChancePerTick)
                return;

            bool spawnOnLeft = Random.value < 0.5f;
            Vector3 planeCenter = transform.position + (spawnOnLeft ? Vector3.left : Vector3.right) * _spawningPlaneDistanceFromPlayer;

            float halfSide = _spawningPlaneSideLength / 2f;
            Vector3 randomOffset = new Vector3(
                Random.Range(-halfSide, halfSide),
                Random.Range(0f, halfSide),
                Random.Range(-halfSide, halfSide)
            );

            Vector3 spawnPos = planeCenter + randomOffset;

            float terrainHeight = _terrain.SampleHeight(spawnPos) + _terrain.GetPosition().y;
            if (spawnPos.y < terrainHeight)
                return;

            Collider[] colliders = Physics.OverlapSphere(spawnPos, _obstacleAvoidanceRadius);
            if (colliders.Length > 0)
                return;

            BirdResource newSparrow = Instantiate(_birdResourcePrefab, spawnPos, Quaternion.identity);
            newSparrow.Initialize(!spawnOnLeft);
            _activeSparrows.Add(newSparrow);
            Debug.Log($"Spawned Bird ({(spawnOnLeft ? "Left" : "Right")}), Count: {_activeSparrows.Count}/{_maxSparrows}");
        }
    }
}
