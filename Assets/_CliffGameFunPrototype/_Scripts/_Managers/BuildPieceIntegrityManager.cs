using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildPieceIntegrityManager : MonoBehaviour
    {
        public static BuildPieceIntegrityManager Instance { get; private set; }
        
        [SerializeField]
        private float _destructionDelay = 0.1f, _initialDestructionDelay = 0.5f;

        [SerializeField]
        private int _maxSupportedDistance = 5;
        
        private HashSet<BuildPiece> _registeredBuildPieces = new();
        private HashSet<BuildPiece> _supportedBuildPieces = new();
        private Queue<(BuildPiece piece, int distance)> _snapShotQueue = new();
        
        private void Awake()
        {
            Instance = this;
        }
        
        public void RegisterBuildPiece(BuildPiece buildPiece)
        {
            buildPiece.InitializeAnchoredStatus();
            _registeredBuildPieces.Add(buildPiece);

            ExecuteIntegrityCheck();
        }
        
        public void UnregisterBuildPiece(BuildPiece buildPiece, bool executeIntegrityCheck = true)
        {
            _registeredBuildPieces.Remove(buildPiece);

            if (executeIntegrityCheck)
                ExecuteIntegrityCheck();
        }
        
        private void ExecuteIntegrityCheck()
        {
            foreach (BuildPiece buildPiece in _registeredBuildPieces)
            {
                buildPiece.SetDistanceFromAnchor(int.MaxValue);
            }

            // Find all supported build pieces
            _supportedBuildPieces.Clear();
            _snapShotQueue.Clear();

            foreach (BuildPiece buildPiece in _registeredBuildPieces)
            {
                if (buildPiece.IsAnchored)
                {
                    buildPiece.SetDistanceFromAnchor(0);
                    _supportedBuildPieces.Add(buildPiece);
                    _snapShotQueue.Enqueue((buildPiece, 0));
                }
            }
            
            while (_snapShotQueue.Count > 0)
            {
                var (currentPiece, currentDistance) = _snapShotQueue.Dequeue();

                if (currentDistance >= _maxSupportedDistance)
                    continue;

                foreach (BuildPiece neighbor in currentPiece.GetConnectedBuildPieces())
                {
                    int nextDistance = currentDistance + 1;

                    if (nextDistance >= neighbor.DistanceFromAnchor)
                        continue;

                    neighbor.SetDistanceFromAnchor(nextDistance);
                    _supportedBuildPieces.Add(neighbor);
                    _snapShotQueue.Enqueue((neighbor, nextDistance));
                }
            }

            // Find all unsupported build pieces
            List<BuildPiece> unsupported = new();

            foreach (BuildPiece buildPiece in _registeredBuildPieces)
            {
                if (!_supportedBuildPieces.Contains(buildPiece))
                    unsupported.Add(buildPiece);
            }

            if (unsupported.Count == 0)
                return;

            StartCoroutine(DestroyUnsupportedPiecesRoutine(unsupported));
        }

        // NTFS: Might be very buggy if I start another destroy routine while one is already running
        private IEnumerator DestroyUnsupportedPiecesRoutine(List<BuildPiece> unsupported)
        {
            Transform playerTransform = Player.Instance.transform;

            if (playerTransform != null)
            {
                Vector3 playerPosition = playerTransform.position;

                unsupported.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;

                    float da = Vector3.SqrMagnitude(a.transform.position - playerPosition);
                    float db = Vector3.SqrMagnitude(b.transform.position - playerPosition);

                    return da.CompareTo(db);
                });
            }
            
            yield return new WaitForSeconds(_initialDestructionDelay);

            foreach (BuildPiece buildPiece in unsupported)
            {
                yield return new WaitForSeconds(_destructionDelay);
                
                _registeredBuildPieces.Remove(buildPiece);
                
                buildPiece.HandleDestroy();
            }
        }
    }
}
