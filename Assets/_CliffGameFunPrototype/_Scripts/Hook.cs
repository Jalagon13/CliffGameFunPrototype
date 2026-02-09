using UnityEngine;

namespace CliffGame
{
    public class Hook : MonoBehaviour
    {
        [SerializeField] private LayerMask _solidLayerMask;
    
        private LineRenderer _hookLine;
        
        private void Awake()
        {
            _hookLine = GetComponent<LineRenderer>();
            _hookLine.positionCount = 2;
        }
        
        private void Update()
        {
            Transform shootPoint = HookshotManager.Instance.HookshotHolder.ShootPoint;

            if (shootPoint != null)
            {
                _hookLine.SetPosition(0, shootPoint.position);
                _hookLine.SetPosition(1, transform.position);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _solidLayerMask) != 0)
            {
                HookshotManager.Instance.HookshotHolder.RegisterHit();
            }
        
            if (other.TryGetComponent(out BirdResource bird))
            {
                bird.Catch(transform);
                HookshotManager.Instance.HookshotHolder.RegisterHit();
            }
        }
    }
}
