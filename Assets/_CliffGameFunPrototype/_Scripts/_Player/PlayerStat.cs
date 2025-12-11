using System;
using UnityEngine;

namespace CliffGame
{
    public class PlayerStat
    {
        public int Max { get; private set; }
        public int Current { get; private set; }

        public float DrainPerSecond { get; set; }
        public float RegenPerSecond { get; set; }

        private float _drainCounter = 0f;
        private float _regenCounter = 0f;

        public event Action<int, int> OnValueChanged; // current, max

        public PlayerStat(int max, float drainPerSecond = 0f, float regenPerSecond = 0f)
        {
            Max = max;
            Current = max;
            DrainPerSecond = drainPerSecond;
            RegenPerSecond = regenPerSecond;
        }

        public void SetDrainPerSecond(float drainPerSecond)
        {
            DrainPerSecond = drainPerSecond;
        }

        public void UpdateStat(float deltaTime, bool isActive = true)
        {
            if (isActive && DrainPerSecond > 0f)
            {
                _drainCounter += DrainPerSecond * deltaTime;
                if (_drainCounter >= 1f)
                {
                    int drainAmount = Mathf.FloorToInt(_drainCounter);
                    ChangeCurrent(-drainAmount);
                    _drainCounter -= drainAmount;
                    _regenCounter = 0f; // Reset regen when draining
                }
            }
            else if (RegenPerSecond > 0f)
            {
                _regenCounter += RegenPerSecond * deltaTime;
                if (_regenCounter >= 1f)
                {
                    int regenAmount = Mathf.FloorToInt(_regenCounter);
                    ChangeCurrent(regenAmount);
                    _regenCounter -= regenAmount;
                    _drainCounter = 0f; // Reset drain when regenerating
                }
            }
        }

        public void ChangeCurrent(int amount)
        {
            int old = Current;
            Current = Mathf.Clamp(Current + amount, 0, Max);
            if (Current != old)
                OnValueChanged?.Invoke(Current, Max);
        }
    }
}