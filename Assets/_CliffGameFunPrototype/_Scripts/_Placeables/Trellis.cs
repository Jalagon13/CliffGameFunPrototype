using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CliffGame
{
    public class Trellis : Placeable
    {
        [Header("Trellis Settings")]
        [SerializeField] private DecalProjector _dirtDecalProjecter;
        [SerializeField] private Transform _dirtMounts;
        [SerializeField] private List<PlanterBox> _planterBoxes = new List<PlanterBox>();
    
        public override void OnSpawnAsGhost()
        {
            _dirtDecalProjecter.enabled = false;
            _dirtMounts.gameObject.SetActive(false);
        }

        public override void DestroyResource(bool giveItems = true)
        {
            foreach (PlanterBox pb in _planterBoxes)
            {
                if(pb.CurrentState == PlanterBoxState.Growing)
                {
                    pb.CancelGrowth();
                }
                else if(pb.CurrentState == PlanterBoxState.Grown)
                {
                    pb.CurrentGrowableInstance.DestroyResource(true);
                }
            }
        
            base.DestroyResource(giveItems);
        }
                
        public override void OnInteractWith()
        {
            Debug.Log($"Interacting with trellis");
        }
    }
}
