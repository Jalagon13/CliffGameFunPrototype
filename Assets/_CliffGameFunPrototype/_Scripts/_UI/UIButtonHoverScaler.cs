using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace CliffGame
{
    public class UIButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.2f;
        [SerializeField] private float speed = 8f;
        private Vector3 originalScale;
        private TextMeshProUGUI text;

        private bool isHovered;

        private void Awake()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                originalScale = text.rectTransform.localScale;
            }
        }

        private void Update()
        {
            if (text == null) return;

            Vector3 targetScale = isHovered ? originalScale * hoverScale : originalScale;
            text.rectTransform.localScale = Vector3.Lerp(text.rectTransform.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }
    }
}
