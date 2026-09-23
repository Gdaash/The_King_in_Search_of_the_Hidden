using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UnifiedButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private ButtonVisualTheme theme;
        [SerializeField] private bool animateScale = true;
        [SerializeField] private bool useButtonPalette;

        private Button button;
        private Graphic graphic;
        private Outline outline;
        private Vector3 originalScale;
        private Color originalColor;
        private bool hovered;
        private bool focused;
        private bool pressed;

        public float ExternalScaleMultiplier { get; set; } = 1f;

        private void Awake()
        {
            button = GetComponent<Button>();
            graphic = button.targetGraphic;
            if (graphic != null) outline = graphic.GetComponent<Outline>();
            originalScale = transform.localScale;
            if (graphic != null) originalColor = graphic.color;
        }

        private void Update()
        {
            if (button == null || theme == null) return;
            bool enabledButton = button.IsActive() && button.interactable;
            bool highlighted = enabledButton && (hovered || focused);
            float scale = pressed && enabledButton ? theme.pressedScale :
                highlighted ? theme.hoverScale : 1f;
            float step = 1f - Mathf.Exp(-theme.transitionSpeed * Time.unscaledDeltaTime);

            if (animateScale)
            {
                var targetScale = originalScale * scale * Mathf.Max(0.01f, ExternalScaleMultiplier);
                if ((transform.localScale - targetScale).sqrMagnitude > 0.000001f)
                    transform.localScale = Vector3.Lerp(transform.localScale, targetScale, step);
            }

            if (graphic == null) return;
            var palette = button.colors;
            Color target;
            if (!enabledButton)
                target = Color.Lerp(originalColor,
                    useButtonPalette ? palette.disabledColor : theme.disabledColor, 0.85f);
            else if (pressed)
                target = Color.Lerp(originalColor,
                    useButtonPalette ? palette.pressedColor : theme.pressedColor, 0.8f);
            else if (highlighted)
                target = Color.Lerp(originalColor,
                    useButtonPalette ? palette.highlightedColor : theme.hoverColor,
                    theme.hoverColorStrength);
            else
                target = originalColor;

            if (((Vector4)(graphic.color - target)).sqrMagnitude > 0.000001f)
                graphic.color = Color.Lerp(graphic.color, target, step);

            if (outline != null)
            {
                var targetOutline = theme.hoverOutlineColor;
                if (!highlighted) targetOutline.a = 0f;
                else if (pressed) targetOutline.a *= 0.6f;
                if (((Vector4)(outline.effectColor - targetOutline)).sqrMagnitude > 0.000001f)
                    outline.effectColor = Color.Lerp(outline.effectColor, targetOutline, step);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => pressed = button != null && button.interactable;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => focused = true;
        public void OnDeselect(BaseEventData eventData) => focused = false;

        private void OnDisable()
        {
            hovered = focused = pressed = false;
            ExternalScaleMultiplier = 1f;
            if (animateScale) transform.localScale = originalScale;
            if (graphic != null) graphic.color = originalColor;
            if (outline != null)
            {
                var color = outline.effectColor;
                color.a = 0f;
                outline.effectColor = color;
            }
        }
    }
}
