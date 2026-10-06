using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GameFoundation.UI
{
    public sealed class NotificationRow : MonoBehaviour
    {
        [System.Serializable]
        private struct IconPadding
        {
            public Sprite sprite;
            public float left;
            public float right;
        }

        [SerializeField] private Text label;
        [SerializeField] private Image icon;
        [SerializeField] private CanvasGroup group;
        [SerializeField, Min(0)] private float iconSpacing = 10;
        [SerializeField, Min(1)] private float minimumHeight = 32;
        [SerializeField, Min(0)] private float partSpacing = 10;
        [SerializeField, Min(0)] private float wrappedLineSpacing = 4;
        [Tooltip("Прозрачные поля иконок в исходных пикселях. Учитываются только при размещении, масштаб спрайта сохраняется.")]
        [SerializeField] private IconPadding[] iconPadding;
        public RectTransform Rect => (RectTransform)transform;
        public float Height => Rect.rect.height;
        public float CreatedAt { get; private set; }

        private sealed class PartView
        {
            public Text text;
            public Image image;
            public float width, height, inset, left;
        }

        public void Configure(string text, Sprite sprite, Color color, float width)
            => Configure(new[] { new NotificationPart(text, NotificationKind.Normal, sprite) }, color, color, color, width);

        public void Configure(IReadOnlyList<NotificationPart> parts, Color normal, Color positive, Color negative, float width)
        {
            CreatedAt = Time.unscaledTime;
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            label.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
            var line = new List<PartView>();
            float lineWidth = 0, lineHeight = minimumHeight, y = 0;
            foreach (var part in parts)
            {
                var view = new PartView();
                view.text = Instantiate(label, transform, false);
                view.text.gameObject.SetActive(true);
                view.text.text = part.Text;
                view.text.color = part.Kind == NotificationKind.Positive ? positive : part.Kind == NotificationKind.Negative ? negative : normal;
                float right = 0;
                Vector2 size = part.Icon != null ? part.Icon.rect.size * 2 : Vector2.zero;
                if (part.Icon != null)
                {
                    if (iconPadding != null)
                        foreach (var padding in iconPadding)
                            if (padding.sprite == part.Icon) { view.left = padding.left * 2; right = padding.right * 2; break; }
                    view.image = Instantiate(icon, transform, false);
                    view.image.sprite = part.Icon;
                    view.image.gameObject.SetActive(true);
                    view.image.rectTransform.sizeDelta = size;
                    view.inset = size.x - view.left - right + iconSpacing;
                }
                float textWidth = Mathf.Min(Mathf.Max(1, width - view.inset), Mathf.Ceil(view.text.preferredWidth));
                view.text.rectTransform.anchorMin = view.text.rectTransform.anchorMax = view.text.rectTransform.pivot = new Vector2(0, 1);
                view.text.rectTransform.sizeDelta = new Vector2(textWidth, minimumHeight);
                view.width = textWidth + view.inset;
                view.height = Mathf.Max(minimumHeight, size.y, view.text.preferredHeight);
                if (line.Count > 0 && lineWidth + partSpacing + view.width > width)
                {
                    PlaceLine(line, lineWidth, lineHeight, y, width);
                    y += lineHeight + wrappedLineSpacing;
                    line.Clear(); lineWidth = 0; lineHeight = minimumHeight;
                }
                lineWidth += (line.Count > 0 ? partSpacing : 0) + view.width;
                lineHeight = Mathf.Max(lineHeight, view.height);
                line.Add(view);
            }
            PlaceLine(line, lineWidth, lineHeight, y, width);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y + lineHeight);
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void PlaceLine(List<PartView> parts, float usedWidth, float height, float y, float width)
        {
            float x = (width - usedWidth) * (((int)label.alignment % 3) * .5f);
            foreach (var part in parts)
            {
                var textRect = part.text.rectTransform;
                textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0, 1);
                textRect.sizeDelta = new Vector2(part.width - part.inset, height);
                textRect.anchoredPosition = new Vector2(x + part.inset, -y);
                if (part.image != null)
                {
                    var imageRect = part.image.rectTransform;
                    imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(0, 1);
                    imageRect.anchoredPosition = new Vector2(x - part.left, -y - (height - imageRect.sizeDelta.y) * .5f);
                }
                x += part.width + partSpacing;
            }
        }

        public void Animate(float y, float lifetime, float fade, float movement)
        {
            float age = Time.unscaledTime - CreatedAt;
            group.alpha = Mathf.Min(Mathf.Clamp01(age / fade), Mathf.Clamp01((lifetime + fade - age) / fade));
            Rect.anchoredPosition = Vector2.Lerp(Rect.anchoredPosition, new Vector2(0, y),
                1 - Mathf.Exp(-Time.unscaledDeltaTime * 5 / movement));
        }
    }
}
