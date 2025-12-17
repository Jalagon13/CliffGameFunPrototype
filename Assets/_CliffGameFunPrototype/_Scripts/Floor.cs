using System;
using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CliffGame
{
    public class Floor : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool _isStartingPlatform = false;
        [SerializeField] private int _hitPoints = 100;
        public int MaxHitPoints => _hitPoints;

        [SerializeField] private MMF_Player _rattleVFX;
        [SerializeField] private ParticleSystem _destructionParticles;
        [SerializeField] private ParticleSystem _crackParticles;

        [Header("Decal Settings")]
        [SerializeField] private DecalProjector _crackDecalProjector;
        public DecalProjector DecalProjector => _crackDecalProjector;

        [SerializeField] private GameObject _noCracksModel;

        [SerializeField] private Material[] _crackMaterialsArray; // light, medium, heavy, very heavy
        [SerializeField] private GameObject[] _crackModelsArray;  // light, medium, heavy, very heavy

        [SerializeField] private int _repairAmount = 20;
        [SerializeField] private InventoryItem[] _itemsNeededForRepairing;

        private float _currentHP;
        public int CurrentHitPoints => Mathf.RoundToInt(_currentHP);

        private float _hpPercent;
        private int _lastCrackState = -1;
        public bool IsRattling => _rattleVFX.IsPlaying;

        public GameObject GameObject => gameObject;

        [SerializeField] private float _repairDuration = 1f;
        public float InteractionTime => _repairDuration;

        private void Awake()
        {
            _currentHP = _hitPoints;
        }

        private IEnumerator Start()
        {
            if (!_isStartingPlatform) yield break;

            yield return new WaitForSeconds(3f);

            _rattleVFX?.PlayFeedbacks();
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodRattleSFX, transform.position);
            Instantiate(_crackParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);

            yield return new WaitForSeconds(2f);

            _rattleVFX?.PlayFeedbacks();
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodRattleSFX, transform.position);
            Instantiate(_crackParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);

            yield return new WaitForSeconds(2f);

            _rattleVFX?.PlayFeedbacks();
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodRattleSFX, transform.position);
            Instantiate(_crackParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);
        }

        private void Update()
        {
            if (!enabled) return;

            _hpPercent = Mathf.Clamp01(_currentHP / _hitPoints);

            UpdateCrackDecals();

            if (_currentHP <= 0)
            {
                ManageDestruction();
                Destroy(gameObject);
            }
        }

        public void ExecuteInteraction()
        {
            Debug.Log($"Repairing Floor");
            if (InventoryManager.Instance.InventoryHasItems(_itemsNeededForRepairing))
            {
                AddFloorHp(_repairAmount);
                InventoryManager.Instance.RemoveItems(_itemsNeededForRepairing);
            }
        }

        public void RattleFloor(int damageAmount)
        {
            AddFloorHp(-damageAmount);

            _rattleVFX?.PlayFeedbacks();
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodRattleSFX, transform.position);
            Instantiate(_crackParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);
        }

        private void ManageDestruction()
        {
            Debug.Log($"Managing destruction");

            foreach (Connector connector in transform.GetComponentsInChildren<Connector>())
            {
                // Debug.Log($"disablign connector");
                connector.gameObject.SetActive(false);
                connector.UpdateConnectors(true);
            }

            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodDestroyedSFX, transform.position);
            Instantiate(_destructionParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);
        }

        private void UpdateCrackDecals()
        {
            // Convert HP percent to a crack state (0–4)
            // 0 = no cracks (80–100%)
            // 1 = light      (60–80%)
            // 2 = medium     (40–60%)
            // 3 = heavy      (20–40%)
            // 4 = very heavy (0–20%)
            int crackState = Mathf.Clamp(4 - Mathf.FloorToInt(_hpPercent * 5f), 0, 4);

            // Only run when state changes
            if (crackState == _lastCrackState)
                return;

            _lastCrackState = crackState;

            if (crackState == 0)
            {
                _crackDecalProjector.enabled = false;
                _noCracksModel.SetActive(true);

                // Disable all other crack models
                foreach (var model in _crackModelsArray)
                    model.SetActive(false);
            }
            else
            {
                _crackDecalProjector.enabled = true;
                _noCracksModel.SetActive(false);

                int materialIndex = crackState - 1;
                SetCrackDecal(_crackMaterialsArray[materialIndex]);

                // Enable only the current crack model
                for (int i = 0; i < _crackModelsArray.Length; i++)
                    _crackModelsArray[i].SetActive(i == materialIndex);

                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodDestroyedSFX, transform.position);
                Instantiate(_destructionParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);
            }
        }

        private void SetCrackDecal(Material decal)
        {
            _crackDecalProjector.material = decal;
        }

        public void AddFloorHp(int amount)
        {
            _currentHP += amount;
            if (_currentHP > _hitPoints)
            {
                _currentHP = _hitPoints;
            }
            else if (_currentHP < 0)
            {
                _currentHP = 0;
            }
        }
    }
}
