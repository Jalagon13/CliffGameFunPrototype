using FMODUnity;
using FMOD.Studio;
using UnityEngine;

namespace CliffGame
{
    public class FirstPersonAudio : MonoBehaviour
    {
        public WalkingMoveState Character;
        public GroundCheck GroundCheck;
        public Jump Jump;

        [Header("Movement")]
        [Tooltip("Minimum velocity for moving audio to play")]
        public float VelocityThreshold = 0.01f;

        private Vector2 _lastCharacterPosition;
        private Vector2 _currentCharacterPosition => new Vector2(Character.transform.position.x, Character.transform.position.z);

        // FMOD EventInstances
        private EventInstance _stepsInstance;
        private EventInstance _runningInstance;

        private bool _isMoving = false;
        private bool _isRunning = false;

        private void Start()
        {
            SubscribeToEvents();

            // Create looping FMOD instances
            _stepsInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.StepsSFX);
            _stepsInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

            _runningInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.StepsSFX); // Optional: separate running SFX
            _runningInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        private void OnDestroy()
        {
            UnsubscribeToEvents();

            // Stop and release FMOD instances
            _stepsInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _stepsInstance.release();

            _runningInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _runningInstance.release();
        }

        private void FixedUpdate()
        {
            float velocity = Vector3.Distance(_currentCharacterPosition, _lastCharacterPosition);
            bool moving = velocity >= VelocityThreshold && GroundCheck && GroundCheck.IsGrounded;

            // Update 3D position each frame
            if (_isMoving)
            {
                (_isRunning ? _runningInstance : _stepsInstance)
                    .set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            }

            if (moving && !_isMoving)
            {
                // First time starting movement
                EventInstance instanceToUse = Character.IsRunning ? _runningInstance : _stepsInstance;

                PLAYBACK_STATE state;
                instanceToUse.getPlaybackState(out state);

                if (state == PLAYBACK_STATE.STOPPED)
                {
                    // Start if never started
                    instanceToUse.start();
                }
                else
                {
                    // Resume if paused
                    instanceToUse.setPaused(false);
                }

                _isRunning = Character.IsRunning;
                _isMoving = true;
            }
            else if (!moving && _isMoving)
            {
                // Pause current instance
                (_isRunning ? _runningInstance : _stepsInstance).setPaused(true);
                _isMoving = false;
            }
            else if (moving && _isMoving)
            {
                // Switch between step and running if speed changes
                if (Character.IsRunning != _isRunning)
                {
                    EventInstance previous = _isRunning ? _runningInstance : _stepsInstance;
                    EventInstance next = Character.IsRunning ? _runningInstance : _stepsInstance;

                    previous.setPaused(true);

                    PLAYBACK_STATE state;
                    next.getPlaybackState(out state);

                    if (state == PLAYBACK_STATE.STOPPED)
                        next.start();
                    else
                        next.setPaused(false);

                    _isRunning = Character.IsRunning;
                }
            }

            _lastCharacterPosition = _currentCharacterPosition;
        }

        private void PlayLandingAudio()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.LandingSFX, transform.position);
        }

        private void PlayJumpAudio()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.JumpSFX, transform.position);
        }
        
        private void SubscribeToEvents()
        {
            GroundCheck.Grounded += PlayLandingAudio;

            if (Jump)
                Jump.OnJumped += PlayJumpAudio;
        }

        private void UnsubscribeToEvents()
        {
            GroundCheck.Grounded -= PlayLandingAudio;

            if (Jump)
                Jump.OnJumped -= PlayJumpAudio;
        }

        private void Reset()
        {
            Character = GetComponentInParent<WalkingMoveState>();
            GroundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        }
    }
}