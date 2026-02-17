using UnityEngine;

namespace CliffGame
{
    public class BirdMonsterManager : MonoBehaviour
    {
        public BirdMonsterManager Instance;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            
        }
        
        
    }
}
