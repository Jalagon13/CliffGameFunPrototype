using UnityEngine;

namespace CliffGame
{
    public class TetheredSpearProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask _solidLayerMask;
        [SerializeField] private Collider _projectileCollider;

        private LineRenderer _tetherLine;

        private void Awake()
        {
            _tetherLine = GetComponent<LineRenderer>();
            _tetherLine.positionCount = 2;

            if (_projectileCollider == null)
            {
                _projectileCollider = GetComponent<Collider>();
            }
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
            if (!holder.IsOutboundPhase) return;

            if (((1 << other.gameObject.layer) & _solidLayerMask) != 0)
            {
                if (!holder.HasQueuedNpcCatch())
                {
                    holder.RegisterHit();
                }
            }

            Npc hitNpc = other.GetComponentInParent<Npc>();
            if (hitNpc is ITetherReelableNpc tetherableNpc)
            {
                if (holder.TryQueueNpcCatch(tetherableNpc, other.transform))
                {
                    tetherableNpc.OnTetherStabbed();
                }
            }
        }

        private void OnEnable()
        {
            SetNpcCatchEnabled(true);
        }

        public void SetNpcCatchEnabled(bool enabled)
        {
            if (_projectileCollider == null) return;
            _projectileCollider.enabled = enabled;
        }
    }
}
