using UnityEngine;

namespace CliffGame
{
    public class DeathFloor : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // Check if the object that entered is on the Player layer
            if (other.gameObject.TryGetComponent(out Player player))
            {
                // Kill the player (adjust method name based on your Player class)
                HealthManager.Instance.DamageHealth(1000);
            }
        }
    }
}
