using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public class JournalMenuUI : MonoBehaviour
    {
        [SerializeField] 
        private GameObject _journalMenuUI;
        
        private bool _journalMenuOpen;
        public bool IsJournalMenuOpen => _journalMenuOpen;
    
        private void Start()
        {
            GameInput.Instance.OnToggleJournalMenu += HandleToggleJournalMenu;

            Hide();
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnToggleJournalMenu -= HandleToggleJournalMenu;
        }

        private void HandleToggleJournalMenu(object sender, InputAction.CallbackContext e)
        {
            _journalMenuOpen = !_journalMenuOpen;

            if (_journalMenuOpen)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        private void Show()
        {
            _journalMenuUI.SetActive(true);
        }
        
        private void Hide()
        {
            _journalMenuUI.SetActive(false);
        }
    }
}
