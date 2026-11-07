using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>One line: action at the left, resource icons and amounts at the right. All belong to the animated visual.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(700)]
    public sealed class PriceButtonLayout : MonoBehaviour
    {
        public RectTransform visual;
        public Text label;
        public RectTransform[] prices;
        public float minimumAmountWidth = 24;
        public float iconAmountGap = 6;
        private int previousHash;
        private void OnEnable()
        {
            // Nested prefab overrides cannot serialize a different parent for inherited objects.
            // Resolve those references once so label and prices share the same hover transform.
            if (visual != null)
            {
                if (label != null && label.transform.parent != visual) label.transform.SetParent(visual, false);
                if (prices != null) foreach (var item in prices)
                    if (item != null && item.parent != visual) item.SetParent(visual, false);
            }
            previousHash = 0; Refresh();
        }
        private void LateUpdate() => Refresh();
        public void Invalidate() { previousHash = 0; }
        private static bool Visible(RectTransform item) => item != null && item.gameObject.activeSelf &&
            (!item.TryGetComponent<Graphic>(out var graphic) || graphic.enabled);
        public void Refresh()
        {
            if (visual == null || label == null || prices == null) return;
            int hash = visual.rect.size.GetHashCode() ^ label.text.GetHashCode();
            foreach (var item in prices)
                if (item != null) hash = hash * 31 + (Visible(item) ? 1 : 0) +
                    (item.TryGetComponent<Text>(out var t) ? t.text.GetHashCode() : item.sizeDelta.GetHashCode());
            if (hash == previousHash) return;
            previousHash = hash;
            float right = visual.rect.width * .5f - 20;
            for (int i = prices.Length - 1; i >= 0; i--)
            {
                var item = prices[i];
                if (!Visible(item)) continue;
                float width = item.rect.width;
                if (item.TryGetComponent<Text>(out var amount))
                {
                    amount.fontSize = 22;
                    amount.resizeTextForBestFit = false;
                    amount.alignment = TextAnchor.MiddleCenter;
                    width = Mathf.Max(minimumAmountWidth, Mathf.Ceil(amount.preferredWidth) + 4);
                }
                float height = Mathf.Max(40, item.rect.height);
                if (item.TryGetComponent<Image>(out var icon) && icon.sprite != null)
                {
                    width = icon.sprite.rect.width * 2;
                    height = icon.sprite.rect.height * 2;
                    icon.preserveAspect = true;
                }
                Place(item, width, height, right - width * .5f);
                right -= width + (i % 2 == 0 ? 14 : iconAmountGap);
            }
            float left = -visual.rect.width * .5f + 20;
            float labelWidth = Mathf.Max(40, right - 16 - left);
            Place(label.rectTransform, labelWidth, visual.rect.height - 12, left + labelWidth * .5f);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = 22;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
        private static void Place(RectTransform r, float w, float h, float x)
        {
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(x, 0);
        }
    }
}
