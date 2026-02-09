using UnityEngine;

namespace CliffGame
{
    public class Hook : MonoBehaviour
    {
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
            Debug.Log($"collided w {other.name}");
        }
    }
}
