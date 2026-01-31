using UnityEngine;

namespace CliffGame
{
    public class BuildPieceIntegrityManager : MonoBehaviour
    {
        public static BuildPieceIntegrityManager Instance { get; private set; }
        
        
        
        private void Awake()
        {
            Instance = this;
        }
        
        public void RegisterBuildPiece()
        {
            
        }
        
        public void UnregisterBuildPiece()
        {
            
        }
    }
}
