using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class ResourceDamagePopup : MonoBehaviour
    {
        private const float LifetimeSeconds = 0.8f;
        private const float FadeOutFraction = 0.2f;
        private const float FloatDistancePixels = 65f;
        private const float DepletedFloatDistancePixels = 42f;
        private const float BaseFontSize = 28f;
        private const float CriticalHitFontSize = 32f;
        private const float SpawnOffsetRangePixels = 24f;
        private static readonly Color DefaultTextColor = Color.white;
        private static readonly Color CriticalHitTextColor = new Color(1f, 0.55f, 0.05f, 1f);
        private static readonly Color DepletedToolTextColor = new Color(0.95f, 0.2f, 0.2f, 1f);

        private static Canvas _overlayCanvas;

        private RectTransform _rectTransform;
        private TextMeshProUGUI _textMeshPro;
        private CanvasGroup _canvasGroup;
        private Vector3 _worldAnchorPosition;
        private Vector2 _screenStartPosition;
        private Vector2 _screenSpaceSpawnOffset;
        private float _floatDistancePixels;
        private float _elapsedTime;

        public static void Create(Vector3 worldPosition, int damageAmount, bool wasCriticalHit, bool usedDepletedTool)
        {
            EnsureOverlayCanvasExists();

            GameObject popupGameObject = new GameObject("ResourceDamagePopup", typeof(RectTransform), typeof(CanvasGroup));
            popupGameObject.transform.SetParent(_overlayCanvas.transform, false);

            ResourceDamagePopup popup = popupGameObject.AddComponent<ResourceDamagePopup>();
            popup.Initialize(worldPosition, damageAmount, wasCriticalHit, usedDepletedTool);
        }

        private static void EnsureOverlayCanvasExists()
        {
            if (_overlayCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("ResourceDamagePopupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            Object.DontDestroyOnLoad(canvasObject);
            _overlayCanvas = canvas;
        }

        private void Initialize(Vector3 worldPosition, int damageAmount, bool wasCriticalHit, bool usedDepletedTool)
        {
            _worldAnchorPosition = worldPosition;
            _screenSpaceSpawnOffset = GetRandomScreenSpaceOffset();
            _floatDistancePixels = usedDepletedTool ? DepletedFloatDistancePixels : FloatDistancePixels;
            _rectTransform = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();

            GameObject textObject = new GameObject("DamageText", typeof(RectTransform));
            textObject.transform.SetParent(transform, false);

            _textMeshPro = textObject.AddComponent<TextMeshProUGUI>();
            _textMeshPro.text = damageAmount.ToString();
            _textMeshPro.fontSize = wasCriticalHit ? CriticalHitFontSize : BaseFontSize;
            _textMeshPro.alignment = TextAlignmentOptions.Center;
            _textMeshPro.color = GetDisplayColor(wasCriticalHit, usedDepletedTool);
            _textMeshPro.fontStyle = wasCriticalHit ? FontStyles.Bold : FontStyles.Normal;
            _textMeshPro.outlineWidth = 0.2f;
            _textMeshPro.outlineColor = Color.black;
            _textMeshPro.raycastTarget = false;

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(120f, 50f);

            Vector2 projectedPosition = ProjectWorldToCanvasPosition();
            _screenStartPosition = projectedPosition;
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.anchoredPosition = projectedPosition + _screenSpaceSpawnOffset;
            _rectTransform.sizeDelta = textRect.sizeDelta;
        }

        private void Update()
        {
            if (Camera.main == null)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            float lifetimeProgress = Mathf.Clamp01(_elapsedTime / LifetimeSeconds);
            Vector2 projectedPosition = ProjectWorldToCanvasPosition();
            _screenStartPosition = projectedPosition;
            _rectTransform.anchoredPosition = projectedPosition + _screenSpaceSpawnOffset + Vector2.up * (_floatDistancePixels * lifetimeProgress);

            UpdateAlpha(lifetimeProgress);

            if (_elapsedTime >= LifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }

        private Vector2 ProjectWorldToCanvasPosition()
        {
            if (Camera.main == null)
            {
                return _screenStartPosition;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(_worldAnchorPosition);
            return screenPosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        private void UpdateAlpha(float lifetimeProgress)
        {
            float fadeStartProgress = 1f - FadeOutFraction;
            _canvasGroup.alpha = lifetimeProgress < fadeStartProgress
                ? 1f
                : Mathf.InverseLerp(1f, fadeStartProgress, lifetimeProgress);
        }

        private static Vector2 GetRandomScreenSpaceOffset()
        {
            return new Vector2(
                Random.Range(-SpawnOffsetRangePixels, SpawnOffsetRangePixels),
                Random.Range(-SpawnOffsetRangePixels, SpawnOffsetRangePixels));
        }

        private static Color GetDisplayColor(bool wasCriticalHit, bool usedDepletedTool)
        {
            if (wasCriticalHit)
            {
                return CriticalHitTextColor;
            }

            if (usedDepletedTool)
            {
                return DepletedToolTextColor;
            }

            return DefaultTextColor;
        }
    }
}
