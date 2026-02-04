using UnityEngine;

namespace CliffGame
{
    public class DeadState : MonoBehaviour, IPlayerState
    {
        private Player _context;

        private void Awake()
        {
            _context = GetComponent<Player>();
        }

        public void EnterState()
        {
            // Debug.Log($"Entered Dead State");
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StateFixedUpdate()
        {

        }

        public void ExitState()
        {
            // Debug.Log($"Exited Dead State");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
