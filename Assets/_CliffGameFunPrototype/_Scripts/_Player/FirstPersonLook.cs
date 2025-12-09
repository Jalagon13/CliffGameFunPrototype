using System;
using CliffGame;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    private Transform _character;

    public float Sensitivity = 2;
    public float Smoothing = 1.5f;

    private Vector2 _velocity;
    private Vector2 _frameVelocity;
    private Vector2 _lookInput;
    private Vector3 _offset;

    private void Start()
    {
        _offset = new Vector3(0f, transform.localPosition.y, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        GameInput.Instance.OnLook += GameInput_OnLook;
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnLook -= GameInput_OnLook;
    }

    private void Update()
    {
        // Smooth camera velocity
        Vector2 mouseDelta = new Vector2(_lookInput.x, _lookInput.y);
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * Sensitivity);
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / Smoothing);
        _velocity += _frameVelocity;
        _velocity.y = Mathf.Clamp(_velocity.y, -90, 90);

        if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking)
        {
            // Attach camera back to character as child 0
            if (transform.parent != _character)
            {
                transform.SetParent(_character);
                transform.localPosition = _offset;
                transform.SetSiblingIndex(0);
            }

            // Standard first-person rotation
            transform.localRotation = Quaternion.AngleAxis(-_velocity.y, Vector3.right);
            _character.localRotation = Quaternion.AngleAxis(_velocity.x, Vector3.up);
        }
        else if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Climbing)
        {
            // Detach camera from character
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            // Follow character position
            transform.position = _character.position + _offset;

            // Climbing rotation logic
            Quaternion camRot = Quaternion.Euler(-_velocity.y, _velocity.x, 0f);
            transform.localRotation = camRot;
        }
    }

    private void GameInput_OnLook(object sender, InputAction.CallbackContext e)
    {
        _lookInput = e.ReadValue<Vector2>();
    }

    private void Reset()
    {
        _character = GetComponentInParent<FirstPersonMovement>().transform;
    }
}