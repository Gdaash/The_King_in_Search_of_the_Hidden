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
        [SerializeField] private bool animateGraphic = true;
        [Tooltip("Keep the Image's authored opacity when highlighting, pressing or disabling this button.")]
        [SerializeField] private bool preserveGraphicAlpha;
        [Header("Background opacity / Прозрачность подложки")]
        [SerializeField] private bool useHoverGraphicAlpha;
        [SerializeField, Range(0, 255)] private int idleGraphicAlpha;
        [SerializeField, Range(0, 255)] private int hoverGraphicAlpha = 150;
        [SerializeField, Min(0f)] private float hoverScaleOverride;
        [Tooltip("Optional visual container to animate without scaling nested, independently clickable buttons.")]
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private bool useButtonPalette;
        [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1f;

        private Button button;
        private Graphic graphic;
        private Outline outline;
        private Vector3 originalScale;
        private Color originalColor;
        private bool hovered;
        private bool focused;
        private bool pressed;
        private Transform animatedTransform;
        private PointerEventData hoverPointer;

        public float ExternalScaleMultiplier { get; set; } = 1f;

        private void Awake()
        {
            button = GetComponent<Button>();
            graphic = button.targetGraphic;
            if (graphic != null) outline = graphic.GetComponent<Outline>();
            animatedTransform = scaleTarget != null ? scaleTarget : transform;
            originalScale = animatedTransform.localScale;
            if (graphic != null)
            {
                originalColor = graphic.color;
                if (useHoverGraphicAlpha)
                {
                    originalColor.a = idleGraphicAlpha / 255f;
                    graphic.color = originalColor;
                }
            }
        }

        private void Update()
        {
            if (button == null || theme == null) return;
            // uGUI sends enter/exit to ancestors too. The shared parent does not get
            // another enter/exit when the pointer moves between its child buttons.
            // Keep reading the event's current hit so those transitions stay correct.
            bool overChildButton = false;
            if (hoverPointer != null)
            {
                var hit = hoverPointer.pointerCurrentRaycast.gameObject;
                var nearest = hit != null ? hit.GetComponentInParent<Button>() : null;
                hovered = nearest == button;
                overChildButton = nearest != null && nearest != button && nearest.transform.IsChildOf(transform);
            }
            bool enabledButton = button.IsActive() && button.interactable;
            bool highlighted = enabledButton && !overChildButton && (hovered || focused);
            float scale = pressed && enabledButton ? theme.pressedScale :
                highlighted ? (hoverScaleOverride > 0f ? hoverScaleOverride : theme.hoverScale) * hoverScaleMultiplier : 1f;
            float step = 1f - Mathf.Exp(-theme.transitionSpeed * Time.unscaledDeltaTime);

            if (animateScale)
            {
                var targetScale = originalScale * scale * Mathf.Max(0.01f, ExternalScaleMultiplier);
                if ((animatedTransform.localScale - targetScale).sqrMagnitude > 0.000001f)
                    animatedTransform.localScale = Vector3.Lerp(animatedTransform.localScale, targetScale, step);
            }

            if (graphic == null || !animateGraphic) return;
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

            if (preserveGraphicAlpha) target.a = originalColor.a;
            // Unavailable building actions still show their cost tooltip on hover.
            // Only the background fades in; their disabled tint and scale stay intact.
            if (useHoverGraphicAlpha)
                target.a = button.IsActive() && !overChildButton && (hovered || focused)
                    ? hoverGraphicAlpha / 255f : idleGraphicAlpha / 255f;

            if (((Vector4)(graphic.color - target)).sqrMagnitude > 0.000001f)
                graphic.color = Color.Lerp(graphic.color, target, step);
            else if (graphic.color != target)
                graphic.color = target;

            if (outline != null)
            {
                var targetOutline = theme.hoverOutlineColor;
                if (!highlighted) targetOutline.a = 0f;
                else if (pressed) targetOutline.a *= 0.6f;
                if (((Vector4)(outline.effectColor - targetOutline)).sqrMagnitude > 0.000001f)
                    outline.effectColor = Color.Lerp(outline.effectColor, targetOutline, step);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) { hoverPointer = eventData; hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hoverPointer = null; hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            focused = false;
            pressed = button != null && button.interactable;
        }
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        // Mouse clicks select uGUI buttons too, but must not leave a permanent hover
        // highlight. Keyboard/controller selection still needs a visible focus state.
        public void OnSelect(BaseEventData eventData) => focused = eventData is not PointerEventData;
        public void OnDeselect(BaseEventData eventData) => focused = false;

        private void OnDisable()
        {
            hovered = focused = pressed = false;
            hoverPointer = null;
            ExternalScaleMultiplier = 1f;
            if (animateScale && animatedTransform != null) animatedTransform.localScale = originalScale;
            if (graphic != null && animateGraphic) graphic.color = originalColor;
            if (outline != null)
            {
                var color = outline.effectColor;
                color.a = 0f;
                outline.effectColor = color;
            }
        }
    }
}
