using FMODUnity;
using FMOD.Studio;
using UnityEngine;

namespace CliffGame
{
    public class FirstPersonAudio : MonoBehaviour
    {
        public WalkingMoveState WalkingMoveState;
        public GroundCheck GroundCheck;
        public Jump Jump;

        [Header("Movement")]
        public float VelocityThreshold = 0.01f;

        private Vector3 _lastCharacterPosition;
        private Vector3 _currentCharacterPosition => WalkingMoveState.transform.position;

        private EventInstance _stepsInstance;
        private EventInstance _climbingInstance;

        private bool _isMoving = false;
        private bool _isClimbing = false;

        private void Start()
        {
            SubscribeToEvents();

            _stepsInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.StepsSFX);
            _stepsInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

            _climbingInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.ClimbingSFX);
            _climbingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        private void OnDestroy()
        {
            UnsubscribeToEvents();

            _stepsInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _stepsInstance.release();

            _climbingInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _climbingInstance.release();
        }

        private void FixedUpdate()
        {
            float velocity = Vector3.Distance(_currentCharacterPosition, _lastCharacterPosition);
            bool moving = velocity >= VelocityThreshold;

            bool grounded = GroundCheck.IsGrounded;
            bool walking = Player.Instance.CurrentMoveStateType == PlayerMoveState.Walking;
            bool climbing = Player.Instance.CurrentMoveStateType == PlayerMoveState.Climbing;

            bool shouldPlaySteps = walking && grounded && moving;

            // Update active sound position
            if (_isMoving)
            {
                (_isClimbing ? _climbingInstance : _stepsInstance)
                    .set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            }

            // ----------------------------------------------------------
            // START MOVEMENT
            // ----------------------------------------------------------
            if (moving && !_isMoving)
            {
                if (shouldPlaySteps)
                {
                    StartSteps();
                }
                else if (climbing)
                {
                    StartClimbing();
                }
                else
                {
                    PauseSteps();
                }

                _isMoving = true;
            }

            // ----------------------------------------------------------
            // STOP MOVEMENT
            // ----------------------------------------------------------
            else if (!moving && _isMoving)
            {
                PauseSteps();
                PauseClimbing();
                _isMoving = false;
            }

            // ----------------------------------------------------------
            // SWITCHING MOVEMENT WHILE MOVING
            // ----------------------------------------------------------
            else if (moving && _isMoving)
            {
                bool climbingNow = climbing;

                if (climbingNow != _isClimbing)
                {
                    PauseSteps();
                    PauseClimbing();

                    // ---- TRANSITION ONE-SHOTS ----
                    if (climbingNow && !_isClimbing)
                    {
                        // Walking -> Climbing
                        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WalkToClimbSFX, transform.position);
                    }
                    else if (!climbingNow && _isClimbing)
                    {
                        // Climbing -> Walking
                        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ClimbToWalkSFX, transform.position);
                    }

                    // ---- Start New Loop ----
                    if (climbingNow)
                    {
                        StartClimbing();
                    }
                    else if (shouldPlaySteps)
                    {
                        StartSteps();
                    }

                    _isClimbing = climbingNow;
                }
                else
                {
                    // ENFORCE footsteps rule EVERY SINGLE FRAME
                    if (shouldPlaySteps)
                        StartSteps();
                    else
                        PauseSteps(); // absolutely prevents airborne steps
                }
            }

            _lastCharacterPosition = _currentCharacterPosition;
        }

        // ----------------------------------------------------------
        // SFX HELPERS
        // ----------------------------------------------------------

        private void StartSteps()
        {
            PLAYBACK_STATE state;
            _stepsInstance.getPlaybackState(out state);

            if (state == PLAYBACK_STATE.STOPPED)
                _stepsInstance.start();
            else
                _stepsInstance.setPaused(false);

            _isClimbing = false;
        }

        private void PauseSteps()
        {
            _stepsInstance.setPaused(true);
        }

        private void StartClimbing()
        {
            PLAYBACK_STATE state;
            _climbingInstance.getPlaybackState(out state);

            if (state == PLAYBACK_STATE.STOPPED)
                _climbingInstance.start();
            else
                _climbingInstance.setPaused(false);

            _isClimbing = true;
        }

        private void PauseClimbing()
        {
            _climbingInstance.setPaused(true);
        }

        // ----------------------------------------------------------
        // GLOBAL ONE-SHOT EVENTS
        // ----------------------------------------------------------

        private void SubscribeToEvents()
        {
            Player.Instance.OnMoveStateChanged += OnMoveStateChanged;
            GroundCheck.Grounded += PlayLandingAudio;

            if (Jump)
                Jump.OnJumped += PlayJumpAudio;
        }

        private void UnsubscribeToEvents()
        {
            Player.Instance.OnMoveStateChanged -= OnMoveStateChanged;
            GroundCheck.Grounded -= PlayLandingAudio;

            if (Jump)
                Jump.OnJumped -= PlayJumpAudio;
        }

        private void OnMoveStateChanged(PlayerMoveState prev, PlayerMoveState next)
        {
            if (prev == PlayerMoveState.Climbing && next == PlayerMoveState.Walking)
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ClimbToWalkSFX, transform.position);

            else if (prev == PlayerMoveState.Walking && next == PlayerMoveState.Climbing)
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WalkToClimbSFX, transform.position);
        }

        private void PlayLandingAudio()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.LandingSFX, transform.position);
        }

        private void PlayJumpAudio()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.JumpSFX, transform.position);
        }

        private void Reset()
        {
            WalkingMoveState = GetComponentInParent<WalkingMoveState>();
            GroundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        }
    }
}