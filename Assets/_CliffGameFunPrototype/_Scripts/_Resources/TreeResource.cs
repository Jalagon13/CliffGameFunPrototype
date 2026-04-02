using System.Collections.Generic;
using UnityEngine;

namespace CliffGame
{
    public class TreeResource : NaturalResource
    {
        [Header("Tree Resource Settings")]
        [SerializeField] 
        private GameObject _leafClumpHolder;
        
        [SerializeField]
        private float _initialDecayDuration = 0.2f;
        
        [SerializeField]
        private float _decayDurationIncrement = 0.1f;

        public override void DestroyResource(bool dropItems = true)
        {
            if (dropItems)
            {
                foreach (HarvestDrop drop in _harvestDrops)
                {
                    if (drop.DropItem == null)
                        continue;

                    int amount = Random.Range(drop.MinAmount, drop.MaxAmount + 1);

                    if (amount <= 0)
                        continue;

                    InventoryManager.Instance.CreateWorldItem(drop.DropItem, amount, _itemDropPoint.position);
                }

                AudioManager.Instance.PlayOneShot(_destroySFX, transform.position);
            }

            if (_destroyParticles != null && gameObject.scene.isLoaded)
            {
                Instantiate(_destroyParticles.gameObject, transform.position, Quaternion.identity);
            }
            
            List<Transform> clumps = new List<Transform>();
            foreach (Transform child in _leafClumpHolder.transform)
            {
                clumps.Add(child);
            }

            Vector3 origin = transform.position;
            clumps.Sort((a, b) => Vector3.SqrMagnitude(a.position - origin).CompareTo(Vector3.SqrMagnitude(b.position - origin)));

            float decayDuration = _initialDecayDuration;
            foreach (Transform child in clumps)
            {
                child.SetParent(null);
                if (child.TryGetComponent(out LeafClumpResource leafClump)) 
                    leafClump.Decay(decayDuration);

                decayDuration += _decayDurationIncrement;
            }

            Destroy(gameObject);
        }
    }
}
