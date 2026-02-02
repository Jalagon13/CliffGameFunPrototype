using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildPieceIntegrityManager : MonoBehaviour
    {
        [SerializeField]
        private float destructionDelay = 0.1f;

        public static BuildPieceIntegrityManager Instance { get; private set; }
        
        private HashSet<BuildPiece> _registeredBuildPieces = new();
        private HashSet<BuildPiece> _supportedBuildPieces = new();
        private Queue<BuildPiece> _snapShotQueue = new();
        
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
            // Find all supported build pieces
            _supportedBuildPieces.Clear();
            _snapShotQueue.Clear();

            foreach (BuildPiece buildPiece in _registeredBuildPieces)
            {
                if(buildPiece.IsAnchored)
                {
                    _supportedBuildPieces.Add(buildPiece);
                    _snapShotQueue.Enqueue(buildPiece);
                }
            }
            
            while(_snapShotQueue.Count > 0)
            {
                BuildPiece currentPiece = _snapShotQueue.Dequeue();
                
                foreach (BuildPiece neighbor in currentPiece.GetConnectedBuildPieces())
                {
                    if(_supportedBuildPieces.Add(neighbor))
                    {
                        _snapShotQueue.Enqueue(neighbor);
                    }
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

            foreach (BuildPiece buildPiece in unsupported)
            {
                yield return new WaitForSeconds(destructionDelay);
                
                _registeredBuildPieces.Remove(buildPiece);
                
                buildPiece.HandleDestroy();
            }
        }
    }
}
