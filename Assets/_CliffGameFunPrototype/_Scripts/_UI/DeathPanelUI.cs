using System;
using UnityEngine;

namespace CliffGame
{
    public class DeathPanelUI : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _deathPanel;

        private void Awake()
        {
            Hide();
        }

        private void Start()
        {
            HealthManager.Instance.OnPlayerDeath += HandleOnDeath;
            Player.Instance.OnPlayerRespawn += HandleOnRespawn;
        }

        private void OnDestroy()
        {
            HealthManager.Instance.OnPlayerDeath -= HandleOnDeath;
            Player.Instance.OnPlayerRespawn -= HandleOnRespawn;
        }

        private void HandleOnRespawn()
        {
            Hide();
        }

        private void HandleOnDeath()
        {
            Show();
        }

        private void Show()
        {
            _deathPanel.gameObject.SetActive(true);
        }

        private void Hide()
        {
            _deathPanel.gameObject.SetActive(false);
        }
    }
}
