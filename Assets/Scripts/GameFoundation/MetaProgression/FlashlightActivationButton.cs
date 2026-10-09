using TMPro;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>World-space control placed directly in the Flashlight prefab.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FlashlightActivationButton : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new(1f, .92f, .72f, 1f);

        private WorldFlashlightAvailability availability;
        private Collider2D hitCollider;
        private bool isHovered;
        private bool pointerPressed;
        private Vector2 pointerDown;
        private Camera worldCamera;

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
            ApplyColor(normalColor);
        }

        // Flashlight instances are rotated to aim their beams. The button is a world
        // interface element, so it must remain readable in global coordinates.
        private void LateUpdate() => transform.rotation = Quaternion.identity;

        private void Update()
        {
            if (hitCollider == null || worldCamera == null || availability == null) return;
            Vector2 pointerPosition = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            bool hovered = hitCollider.OverlapPoint(pointerPosition) &&
                !availability.IsPointerOverBlockingUI(Input.mousePosition) && GameSpeedControls.SimulationSpeed > 0;
            if (hovered != isHovered)
            {
                isHovered = hovered;
                ApplyColor(isHovered ? hoverColor : normalColor);
            }
            if (Input.GetMouseButtonDown(0))
            { pointerPressed = hovered; pointerDown = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0))
            {
                if (pointerPressed && hovered && Vector2.Distance(pointerDown, Input.mousePosition) < 8) Activate();
                pointerPressed = false;
            }
        }

        private void OnDisable() { pointerPressed = false; isHovered = false; ApplyColor(normalColor); }

        public void Activate() => availability?.ActivateAvailableLights();

        private void ApplyColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
