using UnityEngine;

namespace CliffGame
{
    public interface ITetherReelableNpc
    {
        bool CanBeTethered { get; }
        float TetherReelStopDistanceFromPlayer { get; }

        bool CatchByTether(Transform spearTransform, bool ignoreStateCheck = false, bool preserveHitOffset = true);
        void ReleaseFromTetherAndFlee();
        void OnTetherStabbed();
    }
}
