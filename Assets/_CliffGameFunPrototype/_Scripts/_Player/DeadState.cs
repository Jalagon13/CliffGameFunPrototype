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
            Debug.Log($"Entered Dead State");
            
            _context.WalkingMoveState.DesiredMoveDirection = Vector2.zero;
            _context.ClimbMoveState.DesiredMoveDirection = Vector2.zero;
            _context.RigidBody.constraints = RigidbodyConstraints.FreezeAll;
            _context.RigidBody.useGravity = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StateFixedUpdate()
        {

        }

        public void ExitState()
        {
            Debug.Log($"Exited Dead State");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
