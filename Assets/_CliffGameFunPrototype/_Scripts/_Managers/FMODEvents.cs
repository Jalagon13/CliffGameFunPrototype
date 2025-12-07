using FMODUnity;
using UnityEngine;

namespace CliffGame
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Ambience")]
        [field: SerializeField] public EventReference WindAmb { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
