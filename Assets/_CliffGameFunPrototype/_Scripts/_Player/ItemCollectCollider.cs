using UnityEngine;

namespace CliffGame
{
    public class ItemCollectCollider : MonoBehaviour
    {
        [SerializeField] private float _collectRadius = 0.25f;
        public float CollectRadius => _collectRadius;
    
        private void OnTriggerStay(Collider other)
        {
            if(other.TryGetComponent(out WorldItem item))
            {
                item.CollectItem(this);
            }
        }
    }
}
