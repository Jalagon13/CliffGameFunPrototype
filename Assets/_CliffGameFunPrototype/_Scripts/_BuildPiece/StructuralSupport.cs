using UnityEngine;

namespace CliffGame
{
    public class StructuralSupport : MonoBehaviour
    {
        [SerializeField] 
        private int _maxSupportScore = 100;
    
        private int _supportScore;
        public int SupportScore => _supportScore;
        
        public void SetSupportScore(int newScore)
        {
            _supportScore = Mathf.Clamp(newScore, 0, _maxSupportScore);
        }
    }
}
