using System;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance;
        
        public event Action OnMorningRise;
        public event Action OnNightRise;
        
        [Serializable]
        public class NpcSpawnEntry
        {
            public GameObject Prefab;
            public float SlotCost = 1f;
            [Range(0, 100)] public int SpawnWeight = 10;
            public float SpawnDistanceFromCliff = 20f;
            public float MinSpawnDistance = 20f;
            public float MaxSpawnDistance = 40f;
            public float SpawnZVarianceMin = -2f;
            public float SpawnZVarianceMax = 2f;
        }

        [SerializeField] 
        private List<NpcSpawnEntry> _npcPool = new List<NpcSpawnEntry>();

        [SerializeField]
        private bool _canSpawnNpcs = true;
        
        [SerializeField]
        private int _maxNpcSlotAmount = 6;
        
        [SerializeField, Range(0f, 60f)] 
        private float _spawnsPerMinute = 5;

        [Header("Spawn Area")]
        [SerializeField] private LayerMask _cliffLayerMask;
        
        private float _currentNpcCapacity = 0;

        private readonly float _tickTime = 1f / 60f; // 60 ticks per second
        private readonly int _maxSpawnAttempts = 50;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InvokeRepeating(nameof(TryToSpawnNpc), _tickTime, _tickTime);
        }
        
        public void OnMorningStarted() // Connected to SkyCore event time system
        {
            Debug.Log($"Morning has risen!");
            OnMorningRise?.Invoke();
        }
        
        public void OnNightStarted() // Connected to SkyCore event time system
        {
            Debug.Log($"Night has risen!");
            OnNightRise?.Invoke();
        }

        private void TryToSpawnNpc()
        {
            if(!_canSpawnNpcs || Player.Instance == null || Player.Instance.FirstPersonLook.IsSequenceOngoing) return;

            // Check if we're at max capacity
            if (_currentNpcCapacity >= _maxNpcSlotAmount) return;

            // Calculate spawn probability per tick (Terraria-style)
            float spawnModifier = GetSpawnModifier();

            // Convert spawns per minute to probability per tick
            // If we want X spawns per minute and we tick 60 times per second (3600 times per minute)
            // Then probability per tick = X / 3600 * modifier
            float spawnProbability = (_spawnsPerMinute / 3600f) * spawnModifier;

            if (UnityEngine.Random.value < spawnProbability)
            {
                NpcSpawnEntry npcEntry = SelectRandomNpc();
                if (npcEntry == null) return;

                float remainingNpcSlotSpace = _maxNpcSlotAmount - _currentNpcCapacity;
                if (npcEntry.SlotCost > remainingNpcSlotSpace) return;

                float cliffZ = 0;
                if (Physics.Raycast(Player.Instance.transform.position, Vector3.back, out RaycastHit hit, 500f, _cliffLayerMask))
                {
                    cliffZ = hit.point.z;
                }

                for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
                {
                    Vector3 potentialSpawnPoint = GetRandomSpawnPoint(npcEntry, cliffZ);

                    if (SpawnSpotIsValid(potentialSpawnPoint, npcEntry.MinSpawnDistance))
                    {
                        SpawnNpc(potentialSpawnPoint, npcEntry);
                        return;
                    }
                }
            }
        }

        private Vector3 GetRandomSpawnPoint(NpcSpawnEntry npcEntry, float cliffZ)
        {
            Vector3 playerPos = Player.Instance.transform.position;
            
            // 1. Determine the Z plane for this mob (Cliff Face + Desired Distance + small variance)
            float targetZ = cliffZ + npcEntry.SpawnDistanceFromCliff + UnityEngine.Random.Range(npcEntry.SpawnZVarianceMin, npcEntry.SpawnZVarianceMax);

            // 2. Calculate the maximum XY radius we can use while staying within _maxSpawnDistance of the player
            // Distance^2 = XY_Dist^2 + Z_Dist^2  =>  XY_Dist = Sqrt(MaxDist^2 - Z_Dist^2)
            float zDifference = targetZ - playerPos.z;
            float maxDistSq = npcEntry.MaxSpawnDistance * npcEntry.MaxSpawnDistance;
            float zDiffSq = zDifference * zDifference;
            
            float maxXYRadius = 0f;

            if (maxDistSq > zDiffSq)
            {
                maxXYRadius = Mathf.Sqrt(maxDistSq - zDiffSq);
            }
            else
            {
                // If the player is too far from the spawn plane (Z-wise), we can't satisfy the max distance.
                // Fallback: Spawn in a small radius on the plane anyway so mobs still appear.
                maxXYRadius = 20f; 
            }

            // 3. Pick a random point in that circle on the XY plane
            Vector2 randomXY = UnityEngine.Random.insideUnitCircle * maxXYRadius;

            return new Vector3(playerPos.x + randomXY.x, playerPos.y + randomXY.y, targetZ);
        }

        private bool SpawnSpotIsValid(Vector3 potentialSpawnPoint, float minSpawnDistance)
        {
            // Check if the immediate area is clear of obstacles
            if (Physics.CheckSphere(potentialSpawnPoint, 1f)) return false;

            // Ensure we aren't spawning too close to the player (respecting min distance)
            if (Vector3.Distance(potentialSpawnPoint, Player.Instance.transform.position) < minSpawnDistance) return false;

            return true;
        }

        private NpcSpawnEntry SelectRandomNpc()
        {
            if (_npcPool.Count == 0) return null;

            int totalWeight = 0;
            foreach (var entry in _npcPool)
            {
                if (IsSpawnAllowed(entry))
                {
                    totalWeight += entry.SpawnWeight;
                }
            }

            if (totalWeight == 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in _npcPool)
            {
                if (IsSpawnAllowed(entry))
                {
                    currentWeight += entry.SpawnWeight;
                    if (randomValue < currentWeight) return entry;
                }
            }

            return null;
        }

        private bool IsSpawnAllowed(NpcSpawnEntry entry)
        {
            // GameTime currentTime = SkyCore.Instance.CurrentGameTime;
            // return entry.AllowedSpawnTimes != null && entry.AllowedSpawnTimes.Contains(currentTime);
            return true;
        }

        private void SpawnNpc(Vector3 potentialSpawnPoint, NpcSpawnEntry npcEntry)
        {
            GameObject newNpc = Instantiate(npcEntry.Prefab, potentialSpawnPoint, Quaternion.identity);
            _currentNpcCapacity += npcEntry.SlotCost;
            
            Debug.Log($"Spawned NPC: {newNpc.name}, Capacity: {_currentNpcCapacity}/{_maxNpcSlotAmount}");

            // Attach a tracker to the NPC so we know when it dies/despawns
            NpcTracker tracker = newNpc.AddComponent<NpcTracker>();
            tracker.Initialize(this, npcEntry.SlotCost);
        }

        public void UnregisterNpc(float cost)
        {
            _currentNpcCapacity = Mathf.Max(0, _currentNpcCapacity - cost);
            Debug.Log($"Unregistered NPC, new Capacity: {_currentNpcCapacity}/{_maxNpcSlotAmount}");
        }

        private float GetSpawnModifier()
        {
            float activeRatio = _currentNpcCapacity / _maxNpcSlotAmount;

            // Terraria-style: More mobs = lower spawn rate, fewer mobs = higher spawn rate
            if (activeRatio < 0.2f)
            {
                return 1.5f; // 50% faster when area is mostly empty
            }
            else if (activeRatio < 0.4f)
            {
                return 1.3f; // 30% faster when area is 20-40% full
            }
            else if (activeRatio < 0.6f)
            {
                return 1.1f; // 10% faster when area is 40-60% full
            }
            else if (activeRatio < 0.8f)
            {
                return 0.9f; // 10% slower when area is 60-80% full
            }
            else if (activeRatio < 0.95f)
            {
                return 0.5f; // 50% slower when area is 80-95% full
            }

            return 0.1f; // 90% slower when area is nearly full
        }

        // Helper component to track NPC lifecycle automatically
        private class NpcTracker : MonoBehaviour
        {
            private NpcManager _manager;
            private float _cost;

            public void Initialize(NpcManager manager, float cost)
            {
                _manager = manager;
                _cost = cost;
            }

            private void OnDestroy()
            {
                if (_manager != null)
                {
                    _manager.UnregisterNpc(_cost);
                }
            }
        }
    }
}
