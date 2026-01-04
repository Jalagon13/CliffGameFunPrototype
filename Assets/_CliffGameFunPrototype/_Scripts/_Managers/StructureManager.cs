using UnityEngine;

namespace CliffGame
{
    public class StructureManager : MonoBehaviour
    {
        public static StructureManager Instance;
        
        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}
