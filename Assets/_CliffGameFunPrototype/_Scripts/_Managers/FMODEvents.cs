using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

namespace CliffGame
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Player SFX")]
        [field: SerializeField] public EventReference PlayerHurtSFX { get; private set; }
        [field: SerializeField] public EventReference ToolSwingSFX { get; private set; }
        [field: FormerlySerializedAs("HookshotReleaseSFX")]
        [field: SerializeField] public EventReference SpearTetherReleaseSFX { get; private set; }
        [field: SerializeField] public EventReference EatingSFX { get; private set; }
        [field: SerializeField] public EventReference GulpSFX { get; private set; }
        [field: SerializeField] public EventReference StomachGrowlSFX { get; private set; }
        [field: SerializeField] public EventReference ThirstPangSFX { get; private set; }

        [field: Header("Walking SFX")]
        [field: SerializeField] public EventReference JumpSFX { get; private set; }
        [field: SerializeField] public EventReference LandingSFX { get; private set; }
        [field: SerializeField] public EventReference StepsSFX { get; private set; }

        [field: Header("UI SFX")]
        [field: SerializeField] public EventReference ItemPickupSFX { get; private set; }
        [field: SerializeField] public EventReference SlotClickedSFX { get; private set; }

        [field: Header("Ambience")]
        [field: SerializeField] public EventReference WindAmb { get; private set; }
        [field: SerializeField] public EventReference TitleMusic { get; private set; }

        [field: Header("Building SFX")]
        [field: SerializeField] public EventReference WoodRattleSFX { get; private set; }
        [field: SerializeField] public EventReference WoodDestroyedSFX { get; private set; }
        [field: SerializeField] public EventReference BuildingRepairedSFX { get; private set; }
        
        [field: Header("Structure SFX")]
        [field: SerializeField] public EventReference StructureBuiltSFX { get; private set; }
        [field: SerializeField] public EventReference StructureDestroyedSFX { get; private set; }
        [field: SerializeField] public EventReference CampfireCooking { get; private set; }
        [field: SerializeField] public EventReference SplashSFX { get; private set; }
        
        [field: Header("Resource SFX")]
        [field: SerializeField] public EventReference BirdCaughtSFX { get; private set; }
        [field: SerializeField] public EventReference StoneHitSFX { get; private set; }
        [field: SerializeField] public EventReference StoneDestroyedSFX { get; private set; }
        [field: SerializeField] public EventReference LeafHitSFX { get; private set; }



        private void Awake()
        {
            Instance = this;
        }
    }
}
