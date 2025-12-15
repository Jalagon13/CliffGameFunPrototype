using MoreMountains.Tools;
using UnityEngine;

namespace CliffGame
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private MMProgressBar _interactRadialBar;

        private bool _wasHarvesting = false; // Track previous state

        private void Update()
        {
            bool isHarvesting = InteractionManager.Instance.IsHarvesting;

            if (isHarvesting)
            {
                if (!_wasHarvesting)
                {
                    OnHarvestStarted();
                }

                _interactRadialBar.gameObject.SetActive(true);
                _interactRadialBar.UpdateBar(InteractionManager.Instance.HarvestTimer.PercentRemaining, 0, 1);
            }
            else
            {
                _interactRadialBar.gameObject.SetActive(false);
            }

            _wasHarvesting = isHarvesting; // Update previous state
        }

        private void OnHarvestStarted()
        {
            _interactRadialBar.UpdateBar(0, 0, 1);
        }
    }
}