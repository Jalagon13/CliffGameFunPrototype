using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CliffGame
{
    public class ThirstBarUI : MonoBehaviour
    {
        [SerializeField] private MMF_Player _thirstPangFeedback;

        private void Start()
        {
            ThirstManager.Instance.OnThirstPangExecuted += HandleThirstPang;
        }

        private void OnDestroy()
        {
            ThirstManager.Instance.OnThirstPangExecuted -= HandleThirstPang;
        }

        private void HandleThirstPang()
        {
            _thirstPangFeedback?.PlayFeedbacks();
        }
    }
}
