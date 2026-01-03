using UnityEngine;

namespace CliffGame
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance;
        
        private void Awake()
        {
            Instance = this;
        }
    }
}
