using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Fits a shelter label and its optional construction icon without changing the authored centre.</summary>
    [ExecuteAlways]
    public sealed class BuildingButtonLayout : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Image constructionIcon;
        [SerializeField, Tooltip("Start the rest of a building name on a second line after its first word.")]
        private bool wrapAfterFirstWord;
        [SerializeField, Min(0)] private float horizontalPadding = 28;
        [SerializeField, Min(0)] private float verticalPadding = 14;
        [SerializeField, Min(0)] private float iconGap = 10;
        [SerializeField, Min(0)] private float minimumHeight = 64;
        private string previousText;
        private int previousFontSize;

        private void OnEnable() => Refresh();
        private void OnValidate() => previousText = null;
        private void LateUpdate()
        {
            if (label != null && (label.text != previousText || label.fontSize != previousFontSize)) Refresh();
        }

        public void Refresh()
        {
            if (label == null) return;
            if (wrapAfterFirstWord && !label.text.Contains("\n"))
            {
                string title = label.text.Trim();
                for (int i = 0; i < title.Length; i++)
                {
                    if (!char.IsWhiteSpace(title[i])) continue;
                    label.text = title.Substring(0, i) + "\n" + title.Substring(i).TrimStart();
                    break;
                }
            }
            previousText = label.text;
            previousFontSize = label.fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = false;
            var spacing = label.GetComponent<LetterSpacing>();
            var settings = label.GetGenerationSettings(Vector2.zero);
            float width = 0;
            foreach (string line in label.text.Split('\n'))
            {
                float extra = 0;
                if (spacing != null && spacing.enabled)
                    for (int i = 1; i < line.Length; i++)
                        if (char.IsLetter(line[i]) && char.IsLetter(line[i - 1])) extra += spacing.Spacing;
                float lineWidth = label.cachedTextGeneratorForLayout.GetPreferredWidth(line, settings) / label.pixelsPerUnit;
                width = Mathf.Max(width, lineWidth + extra);
            }
            width = Mathf.Ceil(width + 4);
            bool icon = constructionIcon != null && constructionIcon.sprite != null && constructionIcon.gameObject.activeSelf;
            Vector2 iconSize = icon ? constructionIcon.sprite.rect.size * 2 : Vector2.zero;
            float textHeight = Mathf.Ceil(Mathf.Max(label.fontSize * 1.5f, label.preferredHeight + 4));
            float height = Mathf.Max(minimumHeight, Mathf.Max(textHeight, iconSize.y) + verticalPadding * 2);
            float total = width + (icon ? iconSize.x + iconGap : 0);
            var root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Ceil(total + horizontalPadding * 2));
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Place(label.rectTransform, new Vector2(width, textHeight), new Vector2((total - width) * .5f, 0));
            if (icon) Place(constructionIcon.rectTransform, iconSize, new Vector2((iconSize.x - total) * .5f, 0));
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
