using FMODUnity;
using UnityEngine;

namespace CliffGame
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Walking SFX")]
        [field: SerializeField] public EventReference JumpSFX { get; private set; }
        [field: SerializeField] public EventReference LandingSFX { get; private set; }
        [field: SerializeField] public EventReference StepsSFX { get; private set; }

        [field: Header("Climbing SFX")]
        [field: SerializeField] public EventReference ClimbingSFX { get; private set; }
        [field: SerializeField] public EventReference SlidingSFX { get; private set; }
        [field: SerializeField] public EventReference LedgeClimbSFX { get; private set; }
        [field: SerializeField] public EventReference ClimbToWalkSFX { get; private set; }
        [field: SerializeField] public EventReference WalkToClimbSFX { get; private set; }

        [field: Header("Ambience")]
        [field: SerializeField] public EventReference WindAmb { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
