using System;
using UnityEngine;

namespace CliffGame
{
    public class Placeable : Resource
    {
        public Platform SupportedBy { get; private set; }

        public void SetSupportedBy(Platform platform)
        {
            SupportedBy = platform;
            SupportedBy.OnPlatformDestroyed += OnSupportedPlatformDestroyed;
        }

        private void OnSupportedPlatformDestroyed()
        {
            SupportedBy.OnPlatformDestroyed -= OnSupportedPlatformDestroyed;
            Destroy(gameObject);
        }
    }
}
