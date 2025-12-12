using System;
using CliffGame;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

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

    private Camera _cam;
    private Tween _fovTween;
    private float _walkingFOV;

    [SerializeField] 
    private float _climbingFOV = 70f;
    
    [SerializeField] 
    private float _fovLerpDuration = 0.25f;

    private void Start()
    {
        _cam = GetComponentInChildren<Camera>();
        _walkingFOV = _cam.fieldOfView;

        _offset = new Vector3(0f, transform.localPosition.y, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        GameInput.Instance.OnLook += GameInput_OnLook;
        Player.Instance.OnMoveStateChanged += OnMoveStateChanged;
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnLook -= GameInput_OnLook;
        Player.Instance.OnMoveStateChanged -= OnMoveStateChanged;
    }

    private void OnMoveStateChanged(PlayerMoveState previous, PlayerMoveState newState)
    {
        _fovTween?.Kill();

        float target = newState == PlayerMoveState.Climbing ? _climbingFOV : _walkingFOV;

        _fovTween = DOTween.To(
            () => _cam.fieldOfView,
            v => _cam.fieldOfView = v,
            target,
            _fovLerpDuration
        ).SetEase(Ease.OutQuad);
    }

    private void Update()
    {
        if(Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead || CraftingManager.Instance.CraftingMenuUIOpened) return;

        // Smooth camera velocity
        Vector2 mouseDelta = new Vector2(_lookInput.x, _lookInput.y);
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * Sensitivity);
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / Smoothing);
        _velocity += _frameVelocity;
        _velocity.y = Mathf.Clamp(_velocity.y, -90, 90);

        if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking)
        {
            // Attach camera back to character
            if (transform.parent != _character)
            {
                transform.SetParent(_character);
                transform.localPosition = _offset;
                transform.SetSiblingIndex(0);
            }

            transform.localRotation = Quaternion.AngleAxis(-_velocity.y, Vector3.right);
            _character.localRotation = Quaternion.AngleAxis(_velocity.x, Vector3.up);
        }
        else if (Player.Instance.CurrentMoveStateType == PlayerMoveState.Climbing)
        {
            // Detach camera
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            transform.position = _character.position + _offset;

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
        _character = GetComponentInParent<WalkingMoveState>().transform;
    }
}