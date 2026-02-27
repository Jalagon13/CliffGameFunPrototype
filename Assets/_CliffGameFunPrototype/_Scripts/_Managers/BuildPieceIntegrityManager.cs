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
        public int MaxSupportedDistance => _maxSupportedDistance;
        
        private HashSet<BuildPiece> _registeredBuildPieces = new();
        public IEnumerable<BuildPiece> RegisteredBuildPieces => _registeredBuildPieces;
        private HashSet<BuildPiece> _supportedBuildPieces = new();
        private Queue<(BuildPiece piece, int distance)> _snapShotQueue = new();

        private Coroutine _destructionRoutine;
        private bool _destructionInProgress;
        private bool _pendingIntegrityCheck;
        private readonly HashSet<BuildPiece> _pendingPlacedPieces = new();

        private void Awake()
        {
            Instance = this;
        }
        
        public void RegisterBuildPiece(BuildPiece buildPiece)
        {
            Debug.Log($"Registering build piece: {buildPiece.name}");
            buildPiece.InitializeAnchoredStatus();
            _registeredBuildPieces.Add(buildPiece);

            RequestIntegrityCheck(buildPiece);
        }
        
        public void UnregisterBuildPiece(BuildPiece buildPiece, bool executeIntegrityCheck = true)
        {
            _registeredBuildPieces.Remove(buildPiece);

            if (executeIntegrityCheck)
                RequestIntegrityCheck();
        }
        
        private void RequestIntegrityCheck(BuildPiece newlyPlacedPiece = null)
        {
            if (_destructionInProgress)
            {
                _pendingIntegrityCheck = true;

                if (newlyPlacedPiece != null)
                {
                    _pendingPlacedPieces.Add(newlyPlacedPiece);
                }

                return;
            }

            ExecuteIntegrityCheck(newlyPlacedPiece);
        }

        private void ExecuteIntegrityCheck(BuildPiece newlyPlacedPiece = null)
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

            if (newlyPlacedPiece != null && unsupported.Contains(newlyPlacedPiece))
            {
                newlyPlacedPiece.MarkRefundable();
            }

            foreach (BuildPiece pendingPiece in _pendingPlacedPieces)
            {
                if (pendingPiece != null && _registeredBuildPieces.Contains(pendingPiece) && unsupported.Contains(pendingPiece))
                {
                    pendingPiece.MarkRefundable();
                }
            }

            _destructionRoutine = StartCoroutine(DestroyUnsupportedPiecesRoutine(unsupported));
        }

        // NTFS: Might be very buggy if I start another destroy routine while one is already running
        private IEnumerator DestroyUnsupportedPiecesRoutine(List<BuildPiece> unsupported)
        {
            _destructionInProgress = true;
        
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
                if (buildPiece == null || buildPiece.gameObject == null)
                    continue;

                yield return new WaitForSeconds(_destructionDelay);
                
                if (buildPiece == null) continue;
                
                _registeredBuildPieces.Remove(buildPiece);
                
                buildPiece.HandleDestroy();
            }

            _destructionInProgress = false;

            if (_pendingIntegrityCheck)
            {
                _pendingIntegrityCheck = false;
                Debug.Log($"Executing pending integrity check");
                ExecuteIntegrityCheck();
            }

            _pendingPlacedPieces.Clear();
        }
    }
}
