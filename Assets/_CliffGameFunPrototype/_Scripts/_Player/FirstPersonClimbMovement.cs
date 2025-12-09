using System;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class FirstPersonClimbMovement : MonoBehaviour, IPlayerState
    {
        [SerializeField]
        private float _fromWallOffsetDistance = 1f;

        [SerializeField]
        private LayerMask _climbableLayer;

        [SerializeField] 
        private float _climbSpeed = 2.5f;

        [SerializeField]
        private float _rayDistance = 3f;

        [SerializeField]
        private float _horizontalSpreadAngle = 25f;

        [SerializeField]
        private float _verticalSpreadAngle = 25f;

        private Vector2 _desiredDirection;
        private Player _context;
        private Rigidbody _rb;
        
        private Vector3 _wallNormal;
        private Vector3 _rightTangent;
        private Vector3 _upTangent;
        private Vector3 _lastWallHitPoint;

        private void Awake()
        {
            _context = GetComponent<Player>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            GameInput.Instance.OnMove += GameInput_OnMove;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnMove -= GameInput_OnMove;
        }

        public void EnterState()
        {
            Debug.Log($"Entered Climb State");

            _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            _rb.useGravity = false;
            _wallNormal = transform.forward;
        }

        public void StateFixedUpdate()
        {
            _wallNormal = CalculateWallNormal();

            // === 2. Compute movement tangents ===
            _rightTangent = Vector3.Cross(Vector3.up, _wallNormal).normalized;
            _upTangent = Vector3.Cross(_wallNormal, _rightTangent).normalized;

            // === 3. Snap player to wall ===
            Vector3 snapPosition = _lastWallHitPoint + (_wallNormal * _fromWallOffsetDistance);
            _rb.MovePosition(snapPosition);

            // === 4. Rotate player to face wall ===
            transform.forward = -_wallNormal;

            // === 5. Move player along the wall based on input ===
            Vector3 move = (_rightTangent * -_desiredDirection.x) + (_upTangent * _desiredDirection.y);
            _rb.MovePosition(_rb.position + move * _climbSpeed * Time.fixedDeltaTime);
        }
        
        private Vector3 CalculateWallNormal()
        {
            // === 1. Perform wall detection using multi‑angle raycasts ===
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            Quaternion leftRot = Quaternion.AngleAxis(-_horizontalSpreadAngle, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(_horizontalSpreadAngle, Vector3.up);
            Quaternion upRot = Quaternion.AngleAxis(-_verticalSpreadAngle, transform.right);
            Quaternion downRot = Quaternion.AngleAxis(_verticalSpreadAngle, transform.right);

            Vector3[] dirs = new Vector3[]
            {
                forward,
                leftRot * forward,
                rightRot * forward,
                upRot * forward,
                downRot * forward
            };

            Vector3 normalSum = Vector3.zero;
            int normalCount = 0;

            foreach (var dir in dirs)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, _rayDistance, _climbableLayer))
                {
                    normalSum += hit.normal;
                    normalCount++;

                    if (normalCount == 1 || hit.distance < Vector3.Distance(origin, _lastWallHitPoint))
                        _lastWallHitPoint = hit.point;
                }
            }
            if (normalCount == 0)
            {
                // No wall detected — prevent zero forward vector
                return _wallNormal;
            }

            return (normalSum / normalCount).normalized;
        }

        public void ExitState()
        {
            Debug.Log($"Exited Climb State");

            _rb.useGravity = true;
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext e)
        {
            _desiredDirection = e.ReadValue<Vector2>();
        }
    }
}
