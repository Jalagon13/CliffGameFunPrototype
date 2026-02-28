using System;
using CliffGame;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class FirstPersonLook : MonoBehaviour
{
    public Action OnStartSequenceFinished;

    [SerializeField] 
    private bool _executeStartingSequence = true;
    public bool ExecuteStartingSequence => _executeStartingSequence;


    [SerializeField]
    private Transform _character;
    
    [SerializeField] 
    private Slider _sensitivitySlider;

    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5f;

    public float Sensitivity = 2;
    public float Smoothing = 1.5f;

    [Header("Starting Sequence Settings")]
    [SerializeField] private float _blackScreenFadeDuration = 2f;
    [SerializeField] private float _startDelay = 1.0f;
    [SerializeField] private float _lookDownDuration = 1.5f;
    [SerializeField] private float _lookDownDelay = 0.5f;
    [SerializeField] private float _lookLeftDuration = 1.5f;
    [SerializeField] private float _lookLeftDelay = 0.5f;
    [SerializeField] private float _lookRightDuration = 1.5f;
    [SerializeField] private float _lookRightDelay = 0.5f;
    [SerializeField] private float _lookForwardDuration = 1.5f;
    [SerializeField] private float _lookDownAngle = 60f;
    [SerializeField] private float _lookLeftAngle = 80f;
    [SerializeField] private float _lookRightAngle = 80f;

    private Vector2 _velocity;
    private Vector2 _frameVelocity;
    private Vector2 _lookInput;
    private Vector3 _offset;

    [Header("FOV Settings")]
    [SerializeField]
    private float _fovLerpDuration = 0.25f;
    
    [SerializeField] 
    private float _sprintingFOV = 90f;
    
    private Camera _cam;
    private Tween _fovTween;
    private float _walkingFOV;

    private Sequence _startingSequence;
    private CanvasGroup _blackScreen;

    public bool IsSequenceOngoing { get; private set; }

    private IEnumerator Start()
    {
        if(_executeStartingSequence)
        {
            IsSequenceOngoing = true;
        }
    
        _cam = GetComponentInChildren<Camera>();
        _walkingFOV = _cam.fieldOfView;

        _offset = new Vector3(0f, transform.localPosition.y, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        GameInput.Instance.OnLook += GameInput_OnLook;
        GameInput.Instance.OnSprintStarted += OnSprintStarted;
        GameInput.Instance.OnSprintEnded += OnSprintEnded;
        
        yield return null;

        if (_executeStartingSequence)
        {
            StartLookingSequence();
        }
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnSprintStarted -= OnSprintStarted;
        GameInput.Instance.OnSprintEnded -= OnSprintEnded;
        GameInput.Instance.OnLook -= GameInput_OnLook;

        _startingSequence?.Kill();
        _fovTween?.Kill();

        if (_blackScreen != null)
        {
            Destroy(_blackScreen.gameObject);
        }
    }

    private void OnSprintStarted(object sender, InputAction.CallbackContext e)
    {
        _fovTween?.Kill();

        _fovTween = DOTween.To(
            () => _cam.fieldOfView,
            v => _cam.fieldOfView = v,
            _sprintingFOV,
            _fovLerpDuration
        ).SetEase(Ease.OutQuad);
    }

    private void OnSprintEnded(object sender, InputAction.CallbackContext e)
    {
        _fovTween?.Kill();

        _fovTween = DOTween.To(
            () => _cam.fieldOfView,
            v => _cam.fieldOfView = v,
            _walkingFOV,
            _fovLerpDuration
        ).SetEase(Ease.OutQuad);
    }

    private void StartLookingSequence()
    {
        IsSequenceOngoing = true;
        _blackScreen = CreateBlackScreen();
        _startingSequence = DOTween.Sequence();

        // Fade out black screen
        _startingSequence.Append(_blackScreen.DOFade(0f, _blackScreenFadeDuration).SetEase(Ease.Linear));
        _startingSequence.AppendCallback(() => Destroy(_blackScreen.gameObject));

        // Initial delay
        _startingSequence.AppendInterval(_startDelay);

        // 1. Look down 60 degrees
        _startingSequence.Append(DOTween.To(() => _velocity.y, x => _velocity.y = x, -_lookDownAngle, _lookDownDuration).SetEase(Ease.InOutQuad));

        _startingSequence.AppendInterval(_lookDownDelay);

        // 2. Look left 80 degrees
        _startingSequence.Append(DOTween.To(() => _velocity.x, x => _velocity.x = x, -_lookLeftAngle, _lookLeftDuration).SetEase(Ease.InOutQuad));

        // Delay after looking left
        _startingSequence.AppendInterval(_lookLeftDelay);

        // 3. Look right 80 degrees (from -80 to 80)
        _startingSequence.Append(DOTween.To(() => _velocity.x, x => _velocity.x = x, _lookRightAngle, _lookRightDuration).SetEase(Ease.InOutQuad));

        // Delay after looking right
        _startingSequence.AppendInterval(_lookRightDelay);

        // 4. Look forward (0 degrees)
        _startingSequence.Append(DOTween.To(() => _velocity.x, x => _velocity.x = x, 0f, _lookForwardDuration).SetEase(Ease.InOutQuad));

        _startingSequence.OnComplete(() => 
        {
            IsSequenceOngoing = false;
            OnStartSequenceFinished?.Invoke();
        });
    }

    private CanvasGroup CreateBlackScreen()
    {
        GameObject canvasGO = new GameObject("BlackScreenCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasGroup cg = canvasGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;

        GameObject imageGO = new GameObject("BlackImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        Image img = imageGO.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        RectTransform rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return cg;
    }

    private void LateUpdate()
    {
        if(Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead || 
            CraftingManager.Instance.IsCraftingUIOpen ||
            BuildingManager.Instance.BuildWheelUI.BuildWheelUIOpen || 
            Time.timeScale == 0f) return;

        if (!IsSequenceOngoing)
        {
            // Smooth camera velocity
            Vector2 mouseDelta = new Vector2(_lookInput.x, _lookInput.y);
            Sensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, _sensitivitySlider.value);
            Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * Sensitivity);
            _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / Smoothing);
            _velocity += _frameVelocity;
            _velocity.y = Mathf.Clamp(_velocity.y, -90, 90);
        }

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
    }

    // private void OnSensitivityChanged(float value) // Connected to the slider
    // {
    //     Sensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, value);
    // }

    private void GameInput_OnLook(object sender, InputAction.CallbackContext e)
    {
        _lookInput = e.ReadValue<Vector2>();
    }

    private void Reset()
    {
        _character = GetComponentInParent<WalkingMoveState>().transform;
    }
}