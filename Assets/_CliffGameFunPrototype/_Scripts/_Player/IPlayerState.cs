using UnityEngine;

namespace CliffGame
{
    public interface IPlayerState
    {
        void EnterState();
        void StateFixedUpdate();
        void ExitState();
    }
}
