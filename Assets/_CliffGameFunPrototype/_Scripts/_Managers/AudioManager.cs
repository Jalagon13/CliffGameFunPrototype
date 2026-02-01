using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;

namespace CliffGame
{
    public enum Ambience
    {
        ForestAmbience = 0,
        CaveAmbience = 1
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private EventInstance _ambienceEventInstance;
        private Bus _masterBus;

        private void Awake()
        {
            Instance = this;
            
            _masterBus = RuntimeManager.GetBus("bus:/");
        }

        private void Start()
        {
            // Debug.Log($"Amb started");
            InitializeAmbience(FMODEvents.Instance.WindAmb);
            SetWindSeverity(0.1f);
        }

        public void OnDestroy()
        {
            _ambienceEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        public void SetWindSeverity(float value)
        {
            _ambienceEventInstance.setParameterByName("WindSeverity", value);
        }

        public void StopCurrentAmbience()
        {
            _ambienceEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        public void SetAmbience(Ambience ambience)
        {
            _ambienceEventInstance.setParameterByName("Ambience", (float)ambience);
        }

        public void InitializeAmbience(EventReference ambienceEventReference)
        {
            _ambienceEventInstance = CreateInstance(ambienceEventReference);
            _ambienceEventInstance.start();
        }

        // Play a sound one time at a specific world position
        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound, worldPos);
        }

        // Create an event instance
        public EventInstance CreateInstance(EventReference eventReference)
        {
            EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
            return eventInstance;
        }
        
        public void SetMasterVolume(float volume) // connected to pause menu volume slider
        {
            _masterBus.setVolume(volume);
        }
    }
}
