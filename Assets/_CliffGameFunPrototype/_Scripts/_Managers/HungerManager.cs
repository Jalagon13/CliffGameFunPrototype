using System;
using UnityEngine;
using CliffGame;
using System.Collections;
using UnityEngine.InputSystem;

namespace CliffGame
{
    public enum HungerState
    {
        Full,   // When the player has a hunger higher than 0
        Hungry  // When hunger is 0
    }

    public class HungerManager : MonoBehaviour
    {
        public static HungerManager Instance { get; private set; }

        [SerializeField]
        private int _maxHunger = 100;
        [SerializeField]
        private float _hungerDrainPerSecond = 0.1f;

        private PlayerStat _hungerStat;
        public int CurrentHunger => _hungerStat.Current;

        public HungerState CurrentHungerState { get; private set; }

        public event Action<int, int> OnHungerChanged; // current, max

        private void Awake()
        {
            Instance = this;

            _hungerStat = new PlayerStat(_maxHunger, _hungerDrainPerSecond, 0f);
            _hungerStat.OnValueChanged += (current, max) =>
            {
                OnHungerChanged?.Invoke(current, max);
                if (current <= 0)
                    CurrentHungerState = HungerState.Hungry;
                else
                    CurrentHungerState = HungerState.Full;
            };
        }

        private IEnumerator Start()
        {
            Player.Instance.OnPlayerRespawn += OnRespawn;
            GameInput.Instance.OnPrimaryInteract += TryToEat;

            yield return null;
            OnHungerChanged?.Invoke(CurrentHunger, _hungerStat.Max);
        }

        private void OnDestroy()
        {
            Player.Instance.OnPlayerRespawn -= OnRespawn;
            GameInput.Instance.OnPrimaryInteract -= TryToEat;
        }

        private void TryToEat(object sender, InputAction.CallbackContext e)
        {
            if(e.started && InventoryManager.Instance.HasSelectedItem && InventoryManager.Instance.SelectedInventoryItem.Item is ConsumableItemSO consumableItem)
            {
                InventoryManager.Instance.RemoveItem(consumableItem, 1);
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.EatingSFX, Player.Instance.transform.position);
                AddHunger(consumableItem.HealAmount);
            }
        }

        private void Update()
        {
            if(Player.Instance.CurrentMoveStateType == PlayerMoveState.Dead) return;
        
            _hungerStat.UpdateStat(Time.deltaTime, true);
        }

        private void OnRespawn()
        {
            AddHunger(_maxHunger);
        }

        public void AddHunger(int amount)
        {
            _hungerStat.ChangeCurrent(amount);
        }
    }
}
