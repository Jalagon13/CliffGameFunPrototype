using UnityEngine;

namespace CliffGame
{
    public class WindParticles : MonoBehaviour
    {
        private Vector3 _offset;
        private bool _initialized = false;

        private void Start()
        {
            if (Player.Instance != null)
            {
                _offset = transform.position - Player.Instance.transform.position;
                _initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || Player.Instance == null)
                return;

            transform.position = Player.Instance.transform.position + _offset;
        }
    }
}
