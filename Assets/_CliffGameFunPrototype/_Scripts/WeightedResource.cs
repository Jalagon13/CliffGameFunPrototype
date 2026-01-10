using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    [System.Serializable]
    public class WeightedResource
    {
        [Tooltip("Resource prefab to spawn")]
        public Resource resourcePrefab;

        [Range(0, 100)]
        [Tooltip("Relative spawn weight")]
        public int spawnWeight = 10;
    }

    public static class WeightedResourceSelector
    {
        /// <summary>
        /// Returns a Resource prefab chosen based on spawn weights.
        /// </summary>
        public static Resource GetRandomResource(List<WeightedResource> resources)
        {
            if (resources == null || resources.Count == 0)
                return null;

            int totalWeight = 0;

            foreach (var entry in resources)
            {
                if (entry.resourcePrefab == null)
                    continue;

                totalWeight += Mathf.Max(0, entry.spawnWeight);
            }

            if (totalWeight == 0)
                return null;

            int roll = Random.Range(0, totalWeight);
            int runningTotal = 0;

            foreach (var entry in resources)
            {
                if (entry.resourcePrefab == null)
                    continue;

                runningTotal += Mathf.Max(0, entry.spawnWeight);

                if (roll < runningTotal)
                    return entry.resourcePrefab;
            }

            return null;
        }
    }
}