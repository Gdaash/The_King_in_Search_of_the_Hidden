using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;

namespace GameFoundation.UI
{
    [ExecuteAlways]
    public sealed class VerticalMilitaryPanel : MonoBehaviour
    {
        public RectTransform title;
        public RectTransform content;
        public Text emptyLabel;
        public WorldMilitaryRosterItemView itemTemplate;
        [SerializeField] private Vector2 cellSize = new(72, 90);
        [SerializeField] private Vector2 cellSpacing = new(8, 8);
        [SerializeField] private Vector2 padding = new(16, 16);
        [SerializeField] private float headerHeight = 56;
        [SerializeField] private float minimumWidth = 120;
        public float Width => ((RectTransform)transform).rect.width;

        private ScrollRect scroll;

        private void Awake()
        {
            if (!Application.isPlaying || content == null) return;

            var viewport = NewRect("Roster Viewport", transform);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = padding;
            viewport.offsetMax = new Vector2(-padding.x, -headerHeight);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var bar = NewRect("Roster Scrollbar", transform);
            bar.anchorMin = new Vector2(1, 0);
            bar.anchorMax = Vector2.one;
            bar.offsetMin = new Vector2(-12, padding.y);
            bar.offsetMax = new Vector2(-4, -headerHeight);
            bar.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.14f, .11f, .17f);
            var slidingArea = NewRect("Sliding Area", bar);
            slidingArea.anchorMax = Vector2.one;
            var handle = NewRect("Handle", slidingArea);
            handle.anchorMax = Vector2.one;
            var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = new Color(.50f, .43f, .56f);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;

            scroll = gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.sizeDelta = rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private void LateUpdate()
        {
            if (content == null) return;
            var panel = (RectTransform)transform;
            float step = cellSize.y + cellSpacing.y;
            int count = 0;
            foreach (Transform child in content)
            {
                if (!child.gameObject.activeSelf || !child.TryGetComponent<WorldMilitaryRosterItemView>(out var item) || item == itemTemplate) continue;
                var rect = (RectTransform)child;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = cellSize;
                rect.anchoredPosition = new Vector2(0, -count * step);
                count++;
            }
            float width = count == 0 ? 0 : Mathf.Max(minimumWidth, cellSize.x + padding.x * 2);
            if (!Mathf.Approximately(panel.rect.width, width))
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (TryGetComponent<CanvasGroup>(out var visibility))
            {
                visibility.alpha = count > 0 ? 1 : 0;
                visibility.blocksRaycasts = visibility.interactable = count > 0;
            }
            if (emptyLabel != null && count > 0 && emptyLabel.gameObject.activeSelf)
                emptyLabel.gameObject.SetActive(false);
            if (scroll == null) return;
            float height = Mathf.Max(0, count * step - cellSpacing.y);
            if (!Mathf.Approximately(content.rect.height, height))
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (height <= scroll.viewport.rect.height && content.anchoredPosition != Vector2.zero)
            {
                scroll.StopMovement();
                content.anchoredPosition = Vector2.zero;
            }
        }
    }
}
