using System;
using CliffGame;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float Sensitivity = 2;
    public float Smoothing = 1.5f;

    private Vector2 _velocity;
    private Vector2 _frameVelocity;
    private Vector2 _lookInput;


    private void Start()
    {
        // Lock the mouse cursor to the game screen.
        Cursor.lockState = CursorLockMode.Locked;
        
        GameInput.Instance.OnLook += GameInput_OnLook;
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnLook -= GameInput_OnLook;
    }

    private void Update()
    {
        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(_lookInput.x, _lookInput.y);
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * Sensitivity);
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / Smoothing);
        _velocity += _frameVelocity;
        _velocity.y = Mathf.Clamp(_velocity.y, -90, 90);

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-_velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(_velocity.x, Vector3.up);
    }

    private void GameInput_OnLook(object sender, InputAction.CallbackContext e)
    {
        _lookInput = e.ReadValue<Vector2>();
    }

    private void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }
}
