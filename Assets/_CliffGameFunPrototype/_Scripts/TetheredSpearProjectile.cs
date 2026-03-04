using UnityEngine;

namespace CliffGame
{
    public class TetheredSpearProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask _solidLayerMask;

        private LineRenderer _tetherLine;

        private void Awake()
        {
            _tetherLine = GetComponent<LineRenderer>();
            _tetherLine.positionCount = 2;
        }

        private void Update()
        {
            Transform shootPoint = SpearTetherManager.Instance.SpearTetherHolder.ShootPoint;

            if (shootPoint != null)
            {
                _tetherLine.SetPosition(0, shootPoint.position);
                _tetherLine.SetPosition(1, transform.position);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            SpearTetherHolder holder = SpearTetherManager.Instance.SpearTetherHolder;

            if (((1 << other.gameObject.layer) & _solidLayerMask) != 0)
            {
                if (!holder.HasQueuedNpcCatch())
                {
                    holder.RegisterHit();
                }
            }

            if (other.TryGetComponent(out BirdNpc bird))
            {
                if (holder.TryQueueBirdCatch(bird))
                {
                    bird.OnTetherStabbed();
                }
            }

            GiantBatNpc giantBat = other.GetComponentInParent<GiantBatNpc>();
            if (giantBat != null && holder.TryQueueGiantBatCatch(giantBat))
            {
                giantBat.OnTetherStabbed();
            }
        }
    }
}
