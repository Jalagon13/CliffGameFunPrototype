using UnityEngine;

namespace CliffGame
{
    public interface IInteractable
    {
        ToolType BreakToolType { get; }

        void OnInteractWith(); // For like opening UI  or something
        
        void OnHitWithTool(int damage); // When it gets hit by a tool
    }
}