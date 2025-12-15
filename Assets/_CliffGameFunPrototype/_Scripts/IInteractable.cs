using UnityEngine;

namespace CliffGame
{
    public interface IInteractable
    {
        // How long it takes to interact with this object
        float InteractionTime { get; }

        // Called when the interaction is executed
        void ExecuteInteraction();
    }
}