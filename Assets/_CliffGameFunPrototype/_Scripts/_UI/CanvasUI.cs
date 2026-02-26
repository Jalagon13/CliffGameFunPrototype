using System;
using DG.Tweening;
using UnityEngine;

namespace CliffGame
{
    public class CanvasUI : MonoBehaviour
    {
        [SerializeField] 
        private float _fadeInDuration = 2f;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        private bool _hasInitialized = false;
        
        private void Start()
        {
            Player.Instance.FirstPersonLook.OnStartSequenceFinished += OnStartSequenceFinished;
        }
        
        private void OnDestroy()
        {
            Player.Instance.FirstPersonLook.OnStartSequenceFinished -= OnStartSequenceFinished;
        }

        void Update()
        {
            if (!_hasInitialized)
            {
                DoLateInitialization();
                _hasInitialized = true;
            }
        }

        private void DoLateInitialization()
        {
            _canvasGroup.alpha = Player.Instance.FirstPersonLook.ExecuteStartingSequence ? 0f : 1f;
        }

        private void OnStartSequenceFinished()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, _fadeInDuration);
        }
    }
}
