using System;
using UnityEngine;
using DG.Tweening;

namespace CliffGame
{
    public class GiantBatAnimations : MonoBehaviour
    {
        [SerializeField] private WingAnimation _wingAnimation;
        [SerializeField] private float _bobAmount = 0.5f;
        [SerializeField] private Transform _targetTransform;
        
        private GiantBatNpc _npc;
        

        private void Awake()
        {
            _npc = transform.root.GetComponent<GiantBatNpc>();

            if (_targetTransform == null)
            {
                _targetTransform = transform;
            }
        }
    
        private void Start()
        {
            _wingAnimation.OnWingDown += OnWingDown;
        }
        
        private void OnDestroy()
        {
            _wingAnimation.OnWingDown -= OnWingDown;
        }

        private void OnWingDown()
        {
            if(_npc.State == GiantBatNpc.BatState.Attacking && _npc.TargetPlatform != null)
            {
                _npc.TargetPlatform.PlayCrackingFX();
            }

            Sequence bobSequence = DOTween.Sequence();
            bobSequence.Append(_targetTransform.DOLocalMoveY(_bobAmount, 0.4f).SetRelative(true).SetEase(Ease.OutQuad));
            bobSequence.Append(_targetTransform.DOLocalMoveY(-_bobAmount, 0.6f).SetRelative(true).SetEase(Ease.InQuad));
        }
    }
}
