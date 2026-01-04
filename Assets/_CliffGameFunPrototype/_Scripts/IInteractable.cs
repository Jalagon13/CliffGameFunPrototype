using UnityEngine;

namespace CliffGame
{
    public interface IInteractable
    {
        ToolType BreakToolType { get; }

        void ExecuteInteraction(); // For like opening UI  or something
        
        void OnHitWithTool(); // When it gets hit by a tool
    }
}