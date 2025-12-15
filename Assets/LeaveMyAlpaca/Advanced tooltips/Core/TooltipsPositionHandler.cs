using UnityEngine;
using UnityEngine.InputSystem; // Needed for the new Input System

namespace AdvancedTooltips.Core
{
    public class TooltipsPositionHandler : MonoBehaviour
    {
        [SerializeField] internal RectTransform Canvas;
        [SerializeField, Tooltip("should be the same one as in the TooltipReferenceHolder")] internal RectTransform Layout;

        void Update()
        {
            if (Mouse.current == null) return; // Safety check in case no mouse is connected

            // Get mouse position using the new Input System
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            // Adjust for canvas scale
            Vector2 anchorPoint = mousePosition / Canvas.localScale.x;

            // Clamp to canvas bounds
            if (anchorPoint.x + Layout.rect.width > Canvas.rect.width)
                anchorPoint.x = Canvas.rect.width - Layout.rect.width;

            if (anchorPoint.y + Layout.rect.height > Canvas.rect.height)
                anchorPoint.y = Canvas.rect.height - Layout.rect.height;

            Layout.anchoredPosition = anchorPoint;
        }
    }
}