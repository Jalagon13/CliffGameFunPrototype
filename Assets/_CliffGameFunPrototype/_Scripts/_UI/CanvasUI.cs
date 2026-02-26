using System;
using DG.Tweening;
using UnityEngine;

namespace CliffGame
{
    public class CanvasUI : MonoBehaviour
    {
        [SerializeField] 
        private GameObject _gameUI;
        [SerializeField] 
        private float _fadeInDuration = 2f;
        
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
            _gameUI.SetActive(!Player.Instance.FirstPersonLook.ExecuteStartingSequence);
                
        }

        private void OnStartSequenceFinished()
        {
            _gameUI.SetActive(true);
            CanvasGroup canvasGroup = _gameUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = _gameUI.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, _fadeInDuration);
        }
    }
}
