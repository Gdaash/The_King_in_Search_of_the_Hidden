using TMPro;
using UnityEngine;
using GameFoundation.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>World-space control placed directly in the Flashlight prefab.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FlashlightActivationButton : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private ButtonVisualTheme theme;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new(1f, .92f, .72f, 1f);

        private WorldFlashlightAvailability availability;
        private Collider2D hitCollider;
        private bool isHovered;
        private bool pointerPressed;
        private Vector2 pointerDown;
        private Camera worldCamera;
        private Vector3 originalScale;
        private Color originalLabelColor;
        private Color displayedBackgroundColor;
        private Color displayedLabelColor;

        public void Bind(WorldFlashlightAvailability owner)
        {
            availability = owner;
            worldCamera = Camera.main;
        }

        private void Awake()
        {
            if (background == null) background = GetComponent<SpriteRenderer>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            hitCollider = GetComponent<Collider2D>();
            transform.rotation = Quaternion.identity;
            originalScale = transform.localScale;
            originalLabelColor = label != null ? label.color : Color.white;
            displayedBackgroundColor = background != null ? background.color : normalColor;
            displayedLabelColor = originalLabelColor;
            ApplyColor(displayedBackgroundColor);
        }

        // Flashlight instances are rotated to aim their beams. The button is a world
        // interface element, so it must remain readable in global coordinates.
        private void LateUpdate() => transform.rotation = Quaternion.identity;

        private void Update()
        {
            if (hitCollider == null || worldCamera == null || availability == null) return;
            Vector2 pointerPosition = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            bool hovered = hitCollider.OverlapPoint(pointerPosition) &&
                !availability.IsPointerOverBlockingUI(Input.mousePosition);
            if (hovered != isHovered)
                isHovered = hovered;
            bool canActivate = availability.CanActivateLights;
            float transitionSpeed = theme != null ? theme.transitionSpeed : 16f;
            float step = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);

            Color targetBackground = !canActivate
                ? (theme != null ? theme.disabledColor : normalColor * .55f)
                : pointerPressed && isHovered
                    ? (theme != null ? theme.pressedColor : hoverColor)
                    : isHovered
                        ? (theme != null ? theme.hoverColor : hoverColor)
                        : (theme != null ? theme.normalColor : normalColor);
            Color targetLabel = canActivate
                ? (theme != null ? theme.positiveLabelColor : originalLabelColor)
                : (theme != null ? theme.unavailableLabelColor : originalLabelColor * .65f);
            displayedBackgroundColor = Color.Lerp(displayedBackgroundColor, targetBackground, step);
            displayedLabelColor = Color.Lerp(displayedLabelColor, targetLabel, step);
            ApplyColor(displayedBackgroundColor);
            if (label != null) label.color = displayedLabelColor;

            float scale = canActivate && isHovered
                ? (pointerPressed ? (theme != null ? theme.pressedScale : .95f)
                    : (theme != null ? theme.hoverScale : 1.08f))
                : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * scale, step);

            if (Input.GetMouseButtonDown(0))
            { pointerPressed = hovered && canActivate; pointerDown = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0))
            {
                if (pointerPressed && hovered && availability.CanActivateLights &&
                    Vector2.Distance(pointerDown, Input.mousePosition) < 8) Activate();
                pointerPressed = false;
            }
        }

        private void OnDisable()
        {
            pointerPressed = false;
            isHovered = false;
            if (transform != null) transform.localScale = originalScale;
            displayedBackgroundColor = theme != null ? theme.normalColor : normalColor;
            displayedLabelColor = originalLabelColor;
            ApplyColor(displayedBackgroundColor);
            if (label != null) label.color = displayedLabelColor;
        }

        public void Activate() => availability?.ActivateAvailableLights();

        private void ApplyColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
