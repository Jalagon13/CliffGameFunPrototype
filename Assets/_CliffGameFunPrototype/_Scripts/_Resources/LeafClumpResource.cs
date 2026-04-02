using System.Collections;
using UnityEngine;

namespace CliffGame
{
    public class LeafClumpResource : NaturalResource
    {
        // [Header("Leaf Clump Settings")]
        // [SerializeField] private float _minDecayDuration = 0.75f;
        // [SerializeField] private float _maxDecayDuration = 3f;
    
        public void Decay(float duration )
        {
            StartCoroutine(DecayLeafClump(duration));
        }
        
        private IEnumerator DecayLeafClump(float duration)
        {
            yield return new WaitForSeconds(duration);
            DestroyResource(true);
        }
    }
}
