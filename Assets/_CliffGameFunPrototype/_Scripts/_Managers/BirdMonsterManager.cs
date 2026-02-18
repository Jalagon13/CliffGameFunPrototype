using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CliffGame
{
    public class BirdMonsterManager : MonoBehaviour
    {
        public static BirdMonsterManager Instance;

        [Header("Settings")]
        [SerializeField] private bool _attackRountineEnabled = true;
        [SerializeField] private float _timeBetweenAttacks = 300f; // 5 minutes
        [SerializeField] private int _numberOfClosestPlatformsToConsider = 4;
        [SerializeField] private BirdController _birdController; // Reference to the actual bird object

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            if(!_attackRountineEnabled) yield break;
            
            Debug.Log($"Monster attack routine enabled");
        
            // Wait for game initialization
            yield return null;

            // Start the infinite game loop
            while (true)
            {
                // 1. Wait for the peaceful interval
                Debug.Log("Bird is patrolling...");
                _birdController.SetState(BirdState.Patrolling);
                yield return new WaitForSeconds(_timeBetweenAttacks);

                // 2. Find a target
                BuildPieceDurability targetPlatform = GetRandomOuterPlatform();

                if (targetPlatform != null)
                {
                    Debug.Log($"Bird attacking platform: {targetPlatform.name}");

                    // 3. Command the bird to attack
                    // We wait until the bird finishes the attack (either flees or destroys it)
                    yield return _birdController.ExecuteAttackSequence(targetPlatform);
                }
                else
                {
                    Debug.LogWarning("Bird wanted to attack, but no platforms found!");
                }
            }
        }
        
        [Button("Test Attack Sequence")]
        public void TestAttackSequence()
        {
            StartCoroutine(TestRoutine());
        }
        
        private IEnumerator TestRoutine()
        {
            if (_birdController.IsAttacking) yield break;

            _birdController.SetState(BirdState.Patrolling);

            BuildPieceDurability targetPlatform = GetRandomOuterPlatform();
            yield return _birdController.ExecuteAttackSequence(targetPlatform);

            _birdController.SetState(BirdState.Patrolling);
        }

        private BuildPieceDurability GetRandomOuterPlatform()
        {
            if (BuildPieceIntegrityManager.Instance == null || _birdController == null) return null;

            List<BuildPiece> allPieces = BuildPieceIntegrityManager.Instance.RegisteredBuildPieces.ToList();
            List<BuildPieceDurability> outerPieces = new();

            foreach (BuildPiece piece in allPieces)
            {
                // Check if the piece has fewer than 4 neighbors (assuming a grid, < 4 means at least one side is open)
                int neighborCount = piece.GetConnectedBuildPieces().Count();
                
                if (neighborCount < 4 && !piece.IsAnchored && piece.TryGetComponent(out BuildPieceDurability durability))
                {
                    outerPieces.Add(durability);
                }
            }

            if (outerPieces.Count == 0) return null;

            // Sort the outer pieces by distance to the bird and take the closest ones
            var closestPieces = outerPieces.OrderBy(p => Vector3.Distance(p.transform.position, _birdController.transform.position)).Take(_numberOfClosestPlatformsToConsider).ToList();

            if (closestPieces.Count == 0) return null;

            return closestPieces[Random.Range(0, closestPieces.Count)];
        }
    }
}
