using UnityEngine;

namespace CliffGame
{
    public class CraftingTable : Placeable
    {

    
        public override void OnInteractWith()
        {
            CraftingManager.Instance.TryToToggleCraftingMenu(true);
        }
    }
}
