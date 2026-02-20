using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class Trellis : Resource
    {
        [SerializeField] private List<PlanterBox> _planterBoxes = new List<PlanterBox>();
    
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
