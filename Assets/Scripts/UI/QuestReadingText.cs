using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Retains legacy UI bindings and layout while rendering reading text with a scalable font.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(600)]
    public sealed class QuestReadingText : MonoBehaviour
    {
        [SerializeField] private Text source;
        [SerializeField] private TextMeshProUGUI display;
        private static TMP_FontAsset readingFont;
        public static void Ensure(Text label)
        {
            if (label == null || label.GetComponent<QuestReadingText>() != null) return;
            if (readingFont == null) readingFont = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            if (readingFont == null) return;
            var child = new GameObject("Reading Text", typeof(RectTransform));
            child.transform.SetParent(label.transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = child.AddComponent<TextMeshProUGUI>();
            text.font = readingFont; text.fontSharedMaterial = readingFont.material; text.raycastTarget = false;
            child.AddComponent<LayoutElement>().ignoreLayout = true;
            var view = label.gameObject.AddComponent<QuestReadingText>();
            view.source = label; view.display = text; view.Refresh();
        }
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void OnDisable()
        {
            if (display != null) display.enabled = false;
            if (source != null) source.canvasRenderer.SetAlpha(1);
        }
        private void Refresh()
        {
            if (source == null || display == null) return;
            display.enabled = source.enabled;
            if (display.text != source.text) display.text = source.text;
            display.fontSize = source.fontSize;
            display.fontStyle = source.fontStyle switch { FontStyle.Bold => FontStyles.Bold, FontStyle.Italic => FontStyles.Italic, FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic, _ => FontStyles.Normal };
            display.textWrappingMode = source.horizontalOverflow == HorizontalWrapMode.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            display.overflowMode = source.verticalOverflow == VerticalWrapMode.Overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            display.enableAutoSizing = source.resizeTextForBestFit;
            if (source.resizeTextForBestFit) { display.fontSizeMin = source.resizeTextMinSize; display.fontSizeMax = source.resizeTextMaxSize; }
            display.color = source.color;
            display.raycastTarget = false;
            display.richText = source.supportRichText;
            display.alignment = source.alignment switch
            {
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.TopLeft
            };
            // CanvasRenderer opacity hides only the old glyphs, preserving source color and layout.
            if (source.canvasRenderer.GetAlpha() != 0) source.canvasRenderer.SetAlpha(0);
        }
    }
}
