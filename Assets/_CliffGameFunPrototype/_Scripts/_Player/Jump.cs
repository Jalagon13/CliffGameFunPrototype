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
        _jumpCooldownTimer = new Timer(_jumpCooldown);
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
        _jumpCooldownTimer.Tick(Time.deltaTime);
    }

    private void GameInput_OnJump(object sender, InputAction.CallbackContext e)
    {
        if(e.started && _jumpCooldownTimer.RemainingSeconds <= 0 && _groundCheck.IsGrounded)
        {
            _rigidbody.linearVelocity = new(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            Debug.Log($"Jumped!");
            _jumpCooldownTimer.Reset();
            _rigidbody.AddForce(JumpStrength * Vector3.up, ForceMode.Impulse);
            OnJumped?.Invoke();
        }
    }
}
