using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CliffGame
{
    public class HungerBarUI : MonoBehaviour
    {
        [SerializeField] private MMF_Player _hungerPangFeedback;
    
        private void Start()
        {
            HungerManager.Instance.OnHungerPangExecuted += HandleHungerPang;
        }
        
        private void OnDestroy()
        {
            HungerManager.Instance.OnHungerPangExecuted -= HandleHungerPang;
        }

        private void HandleHungerPang()
        {
            _hungerPangFeedback?.PlayFeedbacks();
        }
    }
}
