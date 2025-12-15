using System;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance;

        [SerializeField]
        private float _interactSearchDistance = 2f;

        [SerializeField]
        private LayerMask _interactLayer;

        private IInteractable _currentFoundInteractable;
        private IInteractable _previousFoundInteractable;
        private Timer _interactTimer;
        public Timer HarvestTimer => _interactTimer;
        public bool IsHarvesting => _interactTimer != null;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            GameInput.Instance.OnInteractStarted += OnInteractStarted;
            GameInput.Instance.OnInteractCanceled += OnInteractCanceled;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnInteractStarted -= OnInteractStarted;
            GameInput.Instance.OnInteractCanceled -= OnInteractCanceled;
        }

        private void Update()
        {
            if (GameInput.Instance == null) return;

            // Always update current interactable under the crosshair
            SearchForResource();

            if (GameInput.Instance.IsHoldingDownInteract)
            {
                if (_currentFoundInteractable != null)
                {
                    // Start or restart the harvest timer if it's a new interactable
                    if (_currentFoundInteractable != _previousFoundInteractable || _interactTimer == null)
                    {
                        StartInteractTimer(_currentFoundInteractable);
                    }

                    // Tick the timer
                    _interactTimer?.Tick(Time.deltaTime);
                }
                else
                {
                    // Not looking at anything harvestable, cancel timer
                    CancelInteractTimer();
                    _previousFoundInteractable = null;
                }
            }
            else
            {
                // Player is not holding, cancel any ongoing timer
                CancelInteractTimer();
                _previousFoundInteractable = null;
            }
        }

        private void StartInteractTimer(IInteractable interactable)
        {
            CancelInteractTimer(); // Ensure no old timer is active

            _interactTimer = new Timer(interactable.InteractionTime);
            _interactTimer.OnTimerEnd += OnTimerEnd;
            _previousFoundInteractable = interactable;
        }
        
        private void CancelInteractTimer()
        {
            if(_interactTimer != null)
            {
                _interactTimer.OnTimerEnd -= OnTimerEnd;
                _interactTimer = null;
            }
        }

        private void OnTimerEnd(object sender, EventArgs e)
        {
            _currentFoundInteractable.ExecuteInteraction();
        }

        private bool SearchForResource()
        {
            RaycastHit hit;
            if (Physics.Raycast(Player.Instance.PlayerCamera.transform.position, Player.Instance.PlayerCamera.transform.forward, out hit, _interactSearchDistance, _interactLayer))
            {
                // Try to get the IInteractable component from the hit collider
                _currentFoundInteractable = hit.collider.GetComponent<IInteractable>();
                return _currentFoundInteractable != null;
            }

            _currentFoundInteractable = null;
            return false;
        }

        private void OnInteractStarted(object sender, InputAction.CallbackContext e)
        {
            // Not needed to do anything here; Update handles starting harvest
        }

        private void OnInteractCanceled(object sender, InputAction.CallbackContext e)
        {
            CancelInteractTimer();
            _previousFoundInteractable = null;
        }
    }
}
