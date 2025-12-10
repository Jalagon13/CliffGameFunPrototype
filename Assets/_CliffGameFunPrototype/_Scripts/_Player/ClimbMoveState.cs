using System;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class ClimbMoveState : MonoBehaviour, IPlayerState
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

        [SerializeField]
        private float _momentumDamping = 6f;

        [Header("Ledge Climb Settings")]
        [SerializeField] private float _ledgeCheckDistance = 0.7f;
        [SerializeField] private float _ledgeClimbDuration = 0.5f;
        [SerializeField] private float _minLedgeFlatAngle = 45f;

        private Vector2 _desiredDirection;
        private Player _context;
        private Rigidbody _rb;
        
        private Vector3 _wallNormal, _rightTangent, _upTangent, _lastWallHitPoint;
        private Vector3 _climbMomentum, _climbStartPosition, _ledgeTopPosition;
        
        private bool _isLerpingToLedge;
        public bool IsLerpingToLedge => _isLerpingToLedge;
        
        private Timer _ledgeTimer;
        private Transform _bodyVisualsTransform;

        private void Awake()
        {
            _context = GetComponent<Player>();
            _rb = GetComponent<Rigidbody>();
            _bodyVisualsTransform = transform.GetChild(1).transform;
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
            Debug.Log($"Entered Climb State with inherited velocity: {_context.FirstPersonMovement.CaptureExitVelocity}");

            _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            _rb.useGravity = false;
            _climbMomentum = _context.FirstPersonMovement.CaptureExitVelocity;
            _wallNormal = transform.forward;
        }

        public void StateFixedUpdate()
        {
            if (_isLerpingToLedge)
            {
                LerpToLedge();
                return;
            }

            if (_desiredDirection.y > 0.1f && TryFindLedge())
            {
                StartLedgeLerp();
                return;
            }

            _wallNormal = CalculateWallNormal();

            _rightTangent = Vector3.Cross(Vector3.up, _wallNormal).normalized;
            _upTangent = Vector3.Cross(_wallNormal, _rightTangent).normalized;

            transform.forward = -_wallNormal;

            ApplyClimbMomentum();

            (float xInput, float yInput) = ComputeDirectionalInputStop();

            Vector3 move = (_rightTangent * -xInput) + (_upTangent * yInput);

            EnforceWallOffset(ref move);
            
            _rb.MovePosition(_rb.position + move * _climbSpeed * Time.fixedDeltaTime);
        }

        private void EnforceWallOffset(ref Vector3 move)
        {
            Vector3 origin = _bodyVisualsTransform.position;
            Vector3 dir = _bodyVisualsTransform.forward;

            // Ray to find current distance to wall
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 3, _climbableLayer))
            {
                float distanceFromWall = Vector3.Distance(hit.point, origin);
                
                float difference = distanceFromWall - _fromWallOffsetDistance;

                if (Mathf.Abs(difference) > 0.01f)
                {
                    // Direction from player to wall
                    Vector3 directionToWall = (hit.point - origin).normalized;

                    // If difference > 0 → player is too far → move toward wall
                    // If difference < 0 → player is too close → move away from wall

                    Vector3 correction = directionToWall * difference;

                    move += correction;
                }
            }
        }

        private (float, float) ComputeDirectionalInputStop()
        {
            bool leftHit = false, rightHit = false, upHit = false, downHit = false;
            float detectDistance = _rayDistance;
            Vector3 origin = _bodyVisualsTransform.position;
            Vector3 forward = _bodyVisualsTransform.forward;

            Quaternion leftRot = Quaternion.AngleAxis(-_horizontalSpreadAngle, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(_horizontalSpreadAngle, Vector3.up);
            Quaternion upRot = Quaternion.AngleAxis(-_verticalSpreadAngle, _bodyVisualsTransform.right);
            Quaternion downRot = Quaternion.AngleAxis(_verticalSpreadAngle, _bodyVisualsTransform.right);

            (Vector3 dir, string tag)[] dirs = new (Vector3, string)[]
            {
                (forward, "center"),
                (leftRot * forward, "left"),
                (rightRot * forward, "right"),
                (upRot * forward, "up"),
                (downRot * forward, "down"),
            };

            foreach ((Vector3 dir, string tag) in dirs)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, detectDistance, _climbableLayer))
                {
                    if (tag == "left") leftHit = true;
                    if (tag == "right") rightHit = true;
                    if (tag == "up") upHit = true;
                    if (tag == "down") downHit = true;
                }
            }

            float xInput = _desiredDirection.x;
            float yInput = _desiredDirection.y;

            if (xInput < 0 && !leftHit) xInput = 0f;
            if (xInput > 0 && !rightHit) xInput = 0f;
            if (yInput > 0 && !upHit) yInput = 0f;
            if (yInput < 0 && !downHit) yInput = 0f;

            return (xInput, yInput);
        }

        private Vector3 CalculateWallNormal()
        {
            // === 1. Perform wall detection using multi‑angle raycasts ===
            Vector3 origin = _bodyVisualsTransform.position;
            Vector3 forward = _bodyVisualsTransform.forward;

            Quaternion leftRot = Quaternion.AngleAxis(-_horizontalSpreadAngle, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(_horizontalSpreadAngle, Vector3.up);
            Quaternion upRot = Quaternion.AngleAxis(-_verticalSpreadAngle, _bodyVisualsTransform.right);
            Quaternion downRot = Quaternion.AngleAxis(_verticalSpreadAngle, _bodyVisualsTransform.right);

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

        private void ApplyClimbMomentum()
        {
            // Project inherited momentum onto wall tangents
            float rightComponent = Vector3.Dot(_climbMomentum, _rightTangent);
            float upComponent = Vector3.Dot(_climbMomentum, _upTangent);

            Vector3 tangentialMomentum = (rightComponent * _rightTangent) + (upComponent * _upTangent);

            // Apply movement along the wall
            _rb.MovePosition(_rb.position + tangentialMomentum * Time.fixedDeltaTime);

            // Exponential damping
            float decay = Mathf.Exp(-_momentumDamping * Time.fixedDeltaTime);
            _climbMomentum *= decay;
            if (_climbMomentum.magnitude < 0.1f)
            {
                _climbMomentum = Vector3.zero;
            }
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

        private bool TryFindLedge()
        {
            Vector3 cameraPos = Camera.main.transform.position - Vector3.up; // small offset so the camera does not clip through the mesh when climbing the ledge
            Vector3 rayOrigin = cameraPos + Vector3.up * 1f + transform.forward * _ledgeCheckDistance;
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit ledgeHit, 1.5f, _climbableLayer, QueryTriggerInteraction.Ignore))
            {
                float minDot = Mathf.Cos(_minLedgeFlatAngle * Mathf.Deg2Rad);
                bool flatSurface = Vector3.Dot(Vector3.up, ledgeHit.normal) > minDot;

                if (flatSurface)
                {
                    _ledgeTopPosition = ledgeHit.point;
                    _climbStartPosition = transform.position;
                    return true;
                }
            }
            return false;
        }

        private void StartLedgeLerp()
        {
            _isLerpingToLedge = true;
            _climbStartPosition = transform.position;
            _ledgeTimer = new Timer(_ledgeClimbDuration);
            _ledgeTimer.Reset();

            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        private void LerpToLedge()
        {
            _ledgeTimer.Tick(Time.fixedDeltaTime);
            float t = _ledgeTimer.GetPercentComplete();
            transform.position = Vector3.Lerp(_climbStartPosition, _ledgeTopPosition, t);

            if (_ledgeTimer.RemainingSeconds <= 0f)
            {
                _isLerpingToLedge = false;
                _rb.isKinematic = false;
                _rb.detectCollisions = true;
                
                _context.TransitionState(PlayerMoveState.Walking);
            }
        }
    }
}
