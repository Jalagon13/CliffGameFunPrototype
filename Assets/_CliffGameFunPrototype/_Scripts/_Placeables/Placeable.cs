using System;
using UnityEngine;

namespace CliffGame
{
    public class Placeable : Resource
    {
        public BuildPieceDurability SupportedBy { get; private set; }

        public void SetSupportedBy(BuildPieceDurability platform)
        {
            SupportedBy = platform;
            SupportedBy.OnBuildPieceDestoyed += OnSupportedPlatformDestroyed;
        }

        private void OnSupportedPlatformDestroyed()
        {
            SupportedBy.OnBuildPieceDestoyed -= OnSupportedPlatformDestroyed;
            Destroy(gameObject);
        }
        
        public virtual void OnSpawnAsGhost()
        {
            
        }
    }
}
