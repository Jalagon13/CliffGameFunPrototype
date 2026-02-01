using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class BuildPieceIntegrityManager : MonoBehaviour
    {
        public static BuildPieceIntegrityManager Instance { get; private set; }
        
        private HashSet<BuildPiece> _registeredBuildPieces = new();
        private HashSet<BuildPiece> _anchoredBuildPieces = new();
        
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
        
        public void UnregisterBuildPiece(BuildPiece buildPiece)
        {
            _registeredBuildPieces.Remove(buildPiece);

            ExecuteIntegrityCheck();
        }
        
        private void ExecuteIntegrityCheck()
        {
            _anchoredBuildPieces.Clear();
            
            foreach (BuildPiece buildPiece in _registeredBuildPieces)
            {
                if(buildPiece.IsAnchored)
                {
                    _anchoredBuildPieces.Add(buildPiece);
                }
            }
            
            foreach (BuildPiece anchoredPiece in _anchoredBuildPieces)
            {
                
            }
        }
    }
}
