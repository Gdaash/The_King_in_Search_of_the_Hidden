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
            if (hitCollider == null || Camera.main == null) return;
            Vector2 pointerPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool hovered = hitCollider.OverlapPoint(pointerPosition);
            if (hovered != isHovered)
            {
                isHovered = hovered;
                ApplyColor(isHovered ? hoverColor : normalColor);
            }
            if (Input.GetMouseButtonUp(0) && hovered) Activate();
        }

        private void OnMouseUp() => Activate();

        private void Activate()
        {
            if (availability == null)
                availability = UnityEngine.Object.FindFirstObjectByType<WorldFlashlightAvailability>(FindObjectsInactive.Include);
            availability?.ActivateAvailableLights();
        }

        private void ApplyColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
