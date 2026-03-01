using System;
using UnityEngine;
using DG.Tweening;
using FMODUnity;
using System.Collections;

namespace CliffGame
{
    public class GiantBatAnimations : MonoBehaviour
    {
        [SerializeField] private WingAnimation _wingAnimation;
        [SerializeField] private float _bobAmount = 0.5f;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private EventReference _wingFlapSfx;
        
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
                if(UnityEngine.Random.value < 0.9)
                    StartCoroutine(Delay());
                    
                AudioManager.Instance.PlayOneShot(_wingFlapSfx, transform.position);
            }

            Sequence bobSequence = DOTween.Sequence();
            bobSequence.Append(_targetTransform.DOLocalMoveY(_bobAmount, 0.25f).SetRelative(true).SetEase(Ease.OutQuad));
            bobSequence.Append(_targetTransform.DOLocalMoveY(-_bobAmount, 0.5f).SetRelative(true).SetEase(Ease.InQuad));
        }
        
        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.125f);
            if(_npc.TargetPlatform != null)
            {
                _npc.TargetPlatform.PlayCrackingFX();
            }
        }
    }
}
