using UnityEngine;
using CliffGame;
using UnityEngine.InputSystem;
using System;

public class Jump : MonoBehaviour
{
    [SerializeField] private float _jumpCooldown = 0.2f;
    public float JumpStrength = 2;
    public event Action OnJumped;

    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    private GroundCheck _groundCheck;
    private Rigidbody _rigidbody;
    private Timer _jumpCooldownTimer;
    
    private void Reset()
    {
        // Try to get groundCheck.
        _groundCheck = GetComponentInChildren<GroundCheck>();
    }

    private void Awake()
    {
        // Get rigidbody.
        _rigidbody = GetComponent<Rigidbody>();
        // _jumpCooldownTimer = new Timer(_jumpCooldown);
    }
    
    private void Start()
    {
        GameInput.Instance.OnJump += GameInput_OnJump;
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnJump -= GameInput_OnJump;
    }
    
    private void Update()
    {
        // _jumpCooldownTimer.Tick(Time.deltaTime);
    }

    // private void GameInput_OnJump(object sender, InputAction.CallbackContext e)
    // {
    //     if(e.started && _groundCheck.IsGrounded)
    //     {
    //         // _rigidbody.linearVelocity = new(0, 0, 0);
    //         Vector3 vel = _rigidbody.linearVelocity;
    //         vel.y = 0f;
    //         _rigidbody.linearVelocity = vel;

    //         _rigidbody.AddForce(Vector3.up * JumpStrength, ForceMode.Impulse);
    //         OnJumped?.Invoke();
    //     }
    // }

    private bool _jumpRequested;

    private void GameInput_OnJump(object sender, InputAction.CallbackContext e)
    {
        if (e.started && _groundCheck.IsGrounded)
        {
            _jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (!_jumpRequested) return;

        Vector3 vel = _rigidbody.linearVelocity;
        vel.y = 0f;
        _rigidbody.linearVelocity = vel;

        _rigidbody.AddForce(Vector3.up * JumpStrength, ForceMode.Impulse);
        OnJumped?.Invoke();

        _jumpRequested = false;
    }
}
