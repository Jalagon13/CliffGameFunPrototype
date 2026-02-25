using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace CliffGame
{
    public class WaterStill : Placeable
    {
        public enum WaterStillState
        {
            Idle,
            CollectingFiber,
            ProcessingWater,
            WaterReady
        }

        [Header("Water Still Settings")]
        [SerializeField] private float _waterUnitGenerationTimeInSec = 30f;
        
        [SerializeField] private int _fiberNeededPerWaterUnit = 5;
        public int FiberNeededPerWaterUnit => _fiberNeededPerWaterUnit;
        
        [SerializeField] private int _thirstReplenished = 35;
        [SerializeField] private ItemSO _fiberItemSO;
        [SerializeField] private GameObject _fiberModel;
        [SerializeField] private GameObject _waterModel;
        [SerializeField] private float _fiberScatterRange = 0.2f;

        [Header("Processing Water Visuals")]
        [SerializeField] private Transform _fiberVisualParent;
        [SerializeField] private float _processingScaleMin = 0.9f;
        [SerializeField] private float _processingScaleMax = 1.1f;
        [SerializeField] private float _processingScaleDuration = 2f;

        private Vector3 _fiberModelOriginalLocalPosition;
        private readonly List<GameObject> _fiberModelDuplicates = new List<GameObject>();
        private int _currentFiberStorage;
        public int CurrentFiberStorage => _currentFiberStorage;
        
        private Timer _waterExtractionTimer;
        
        private WaterStillState _currentState = WaterStillState.Idle;
        public WaterStillState CurrentState => _currentState;

        private Tween _processingScaleTween;

        protected override void Awake()
        {
            base.Awake();
        
            if (_fiberModel != null)
            {
                _fiberModelOriginalLocalPosition = _fiberModel.transform.localPosition;
                _fiberModel.SetActive(false);
            }

            _waterModel.SetActive(false);
        }

        protected override void OnDestroy()
        {
            StopProcessingVisuals();
            base.OnDestroy();
        }

        private void Update()
        {
            if (_waterExtractionTimer != null && !Player.Instance.PauseMenuUI.IsPauseMenuOpen)
            {
                _waterExtractionTimer.Tick(Time.deltaTime);
            }
        }

        public override void OnHitWithTool()
        {
            // Same behavior as CookingStation: block hits while busy or ready
            if (_currentState == WaterStillState.ProcessingWater ||
                _currentState == WaterStillState.WaterReady)
            {
                Debug.Log("[WaterStill] Hit blocked — current state: " + _currentState);
                return;
            }

            base.OnHitWithTool();
        }

        public override void OnInteractWith()
        {
            base.OnInteractWith();

            Debug.Log("[WaterStill] Interacted — current state: " + _currentState);

            switch (_currentState)
            {
                case WaterStillState.Idle:
                case WaterStillState.CollectingFiber:
                    TryAddFiber();
                    break;

                case WaterStillState.WaterReady:
                    DrinkWater();
                    break;
            }
        }

        private void TryAddFiber()
        {
            Debug.Log("[WaterStill] Attempting to add fiber");

            if(!InventoryManager.Instance.InventoryHasItem(_fiberItemSO, 1))
            {
                Debug.Log($"[WaterStill] No {_fiberItemSO.InGameName} in inventory");
                return;
            }

            InventoryManager.Instance.RemoveItem(_fiberItemSO, 1);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.LeafHitSFX, transform.position);
            
            _currentFiberStorage++;
            AddFiberVisual();
            Debug.Log($"[WaterStill] Fiber added. Stored: {_currentFiberStorage}/{_fiberNeededPerWaterUnit}");

            _currentState = WaterStillState.CollectingFiber;

            if (_currentFiberStorage >= _fiberNeededPerWaterUnit)
            {
                StartWaterExtraction();
            }
        }

        private void StartWaterExtraction()
        {
            Debug.Log("[WaterStill] Fiber quota reached — starting water extraction timer");

            _currentState = WaterStillState.ProcessingWater;
            StartProcessingVisuals();

            _waterExtractionTimer = new Timer(_waterUnitGenerationTimeInSec);
            _waterExtractionTimer.OnTimerEnd += OnWaterExtractionFinished;
        }

        private void OnWaterExtractionFinished(object sender, System.EventArgs e)
        {
            Debug.Log("[WaterStill] Water extraction finished — water ready to drink");
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.SplashSFX, transform.position);

            _waterExtractionTimer.OnTimerEnd -= OnWaterExtractionFinished;
            _waterExtractionTimer = null;
            StopProcessingVisuals();
            ResetFiberVisuals();
            _waterModel.SetActive(true);
            _currentFiberStorage = 0;
            _currentState = WaterStillState.WaterReady;
        }

        private void DrinkWater()
        {
            Debug.Log("[WaterStill] Water consumed — thirst replenished");

            // This assumes your thirst system listens to this interaction
            // or that Resource handles drinking elsewhere
            ThirstManager.Instance.AddThirst(_thirstReplenished);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.GulpSFX, transform.position);
            StopProcessingVisuals();
            ResetFiberVisuals();
            _waterModel.SetActive(false);
            _currentState = WaterStillState.Idle;
        }

        private void AddFiberVisual()
        {
            // First fiber: enable the base model
            if (_currentFiberStorage == 1)
            {
                _fiberModel.SetActive(true);
                _fiberModel.transform.localPosition = _fiberModelOriginalLocalPosition;
                return;
            }

            // Additional fibers: spawn duplicates
            SpawnFiberDuplicate();
        }

        private void SpawnFiberDuplicate()
        {
            if (_fiberModel == null) return;

            GameObject duplicate = Instantiate(_fiberModel, _fiberModel.transform.parent);
            duplicate.SetActive(true);

            Vector3 randomOffset = new Vector3(
                Random.Range(-_fiberScatterRange, _fiberScatterRange),
                0f,
                Random.Range(-_fiberScatterRange, _fiberScatterRange)
            );

            duplicate.transform.localPosition = _fiberModelOriginalLocalPosition + randomOffset;

            float randomYRotation = Random.Range(0f, 360f);
            duplicate.transform.localRotation = Quaternion.Euler(0f, randomYRotation, 0f);

            _fiberModelDuplicates.Add(duplicate);
        }

        private void ResetFiberVisuals()
        {
            foreach (GameObject duplicate in _fiberModelDuplicates)
            {
                if (duplicate != null)
                {
                    Destroy(duplicate);
                }
            }

            _fiberModelDuplicates.Clear();

            if (_fiberModel != null)
            {
                _fiberModel.SetActive(false);
                _fiberModel.transform.localPosition = _fiberModelOriginalLocalPosition;
            }
        }

        private void StartProcessingVisuals()
        {
            if (_fiberVisualParent == null)
                _fiberVisualParent = _fiberModel.transform.parent;

            StopProcessingVisuals();

            _fiberVisualParent.localScale = Vector3.one * _processingScaleMin;

            _processingScaleTween = _fiberVisualParent
                .DOScale(_processingScaleMax, _processingScaleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopProcessingVisuals()
        {
            if (_processingScaleTween != null && _processingScaleTween.IsActive())
            {
                _processingScaleTween.Kill();
                _processingScaleTween = null;
            }

            if (_fiberVisualParent != null)
            {
                _fiberVisualParent.localScale = Vector3.one;
            }
        }
    }
}
