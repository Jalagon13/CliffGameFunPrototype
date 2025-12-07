using UnityEngine;
using CliffGame;
using UnityEngine.InputSystem;
using System;

public class Jump : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float jumpStrength = 2;
    public event Action Jumped;

    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    private GroundCheck _groundCheck;


    private void Reset()
    {
        // Try to get groundCheck.
        _groundCheck = GetComponentInChildren<GroundCheck>();
    }

    private void Awake()
    {
        // Get rigidbody.
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        GameInput.Instance.OnJump += GameInput_OnJump;
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnJump -= GameInput_OnJump;
    }

    private void GameInput_OnJump(object sender, InputAction.CallbackContext e)
    {
        if(!_groundCheck || _groundCheck.IsGrounded)
        {
            _rigidbody.AddForce(100 * jumpStrength * Vector3.up);
            Jumped?.Invoke();
        }
    }
}
