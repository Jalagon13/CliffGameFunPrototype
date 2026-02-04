using UnityEngine;

namespace CliffGame
{
    public interface IPlayerState
    {
        void EnterState();
        void StateUpdate();
        void ExitState();
    }
}
