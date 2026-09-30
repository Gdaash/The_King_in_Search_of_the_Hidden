using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UnitDescriptionTooltip : MonoBehaviour
    {
        [SerializeField] private UnitDescriptionView card;
        [SerializeField] private Vector2 offset = new(18f, 18f);
        [SerializeField, Min(0f)] private float screenMargin = 16f;
        [SerializeField] private Vector2 preferredSize = new(600f, 820f);
        private CanvasGroup group;
        private RectTransform rect;
        private Canvas rootCanvas;
        private Object owner;
        private void Awake()
        {
            group = GetComponent<CanvasGroup>(); rect = (RectTransform)transform;
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            Hide(null);
        }
        public void Show(Object source, UnitDescriptionDefinition definition, MilitaryProfile profile, GameObject unit)
        {
            if (definition == null || card == null) return;
            if (group == null) Awake();
            owner = source;
            card.enabled = true;
            card.Show(definition, profile, unit);
            group.alpha = 1f; group.blocksRaycasts = false; group.interactable = false;
            transform.SetAsLastSibling();
            Position();
        }
        public void Hide(Object source)
        {
            if (source != null && owner != source) return;
            owner = null;
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (card != null) card.enabled = false;
        }
        public void Scroll(Object source, float amount) { if (owner == source && card != null) card.Scroll(amount); }
        private void OnDisable() => Hide(null);
        private void LateUpdate()
        {
            if (group == null || group.alpha <= 0f) return;
            if (owner == null) { Hide(null); return; }
            Position();
        }
        private void Position()
        {
            if (rootCanvas == null || rect == null) return;
            RectTransform canvasRect = (RectTransform)rootCanvas.transform;
            Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            float scale = Mathf.Max(.01f, rootCanvas.scaleFactor);
            float fit = Mathf.Clamp((Screen.width - screenMargin * 2f) / (preferredSize.x * scale), .01f, 1f);
            rect.localScale = new Vector3(fit, fit, 1f);
            scale *= fit;
            rect.sizeDelta = new Vector2(preferredSize.x,
                Mathf.Min(preferredSize.y, (Screen.height - screenMargin * 2f) / scale));
            Vector2 pointer = Input.mousePosition;
            // Prefer above the troop row; flip to the left near the right screen edge.
            float x = pointer.x + offset.x * scale;
            if (x + rect.sizeDelta.x * scale > Screen.width - screenMargin) x = pointer.x - (rect.sizeDelta.x + offset.x) * scale;
            float y = pointer.y + offset.y * scale;
            x = Mathf.Clamp(x, screenMargin, Mathf.Max(screenMargin, Screen.width - screenMargin - rect.sizeDelta.x * scale));
            y = Mathf.Clamp(y, screenMargin, Mathf.Max(screenMargin, Screen.height - screenMargin - rect.sizeDelta.y * scale));
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, new Vector2(x, y), camera, out Vector3 world)) rect.position = world;
        }
    }
}
