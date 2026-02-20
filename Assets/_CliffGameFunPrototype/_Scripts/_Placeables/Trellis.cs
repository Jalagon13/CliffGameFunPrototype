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
    
        public override void OnHitWithTool()
        {
            bool areAllPlanterBoxesEmpty = true;
            foreach (PlanterBox planterBox in _planterBoxes)
            {
                if(planterBox.CurrentState != PlanterBoxState.Empty)
                {
                    areAllPlanterBoxesEmpty = false;
                    break;
                }
            }

            if(areAllPlanterBoxesEmpty)
            {
                Debug.Log($"All planter boxes are empty");
                base.OnHitWithTool();
            }
        }
        
        public override void OnInteractWith()
        {
            Debug.Log($"Interacting with trellis");
        }
    }
}
