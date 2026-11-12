using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    internal sealed class PopupChoicePrice
    {
        private readonly Image icon;
        private readonly TMP_Text amount;

        public PopupChoicePrice(RectTransform row, TMP_FontAsset font, float rightInset, float y = 0)
        {
            var root = new GameObject("Choice price", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(row, false);
            root.anchorMin = root.anchorMax = new Vector2(1, .5f);
            root.pivot = new Vector2(1, .5f);
            root.anchoredPosition = new Vector2(-rightInset, y);
            root.sizeDelta = new Vector2(72, 48);
            icon = new GameObject("Resource icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.rectTransform.SetParent(root, false);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, .5f);
            icon.rectTransform.pivot = new Vector2(0, .5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.raycastTarget = false;
            amount = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            amount.rectTransform.SetParent(root, false);
            amount.rectTransform.anchorMin = new Vector2(0, 0);
            amount.rectTransform.anchorMax = Vector2.one;
            amount.rectTransform.offsetMin = new Vector2(44, 0);
            amount.rectTransform.offsetMax = Vector2.zero;
            amount.font = font;
            amount.fontSize = 22;
            amount.alignment = TextAlignmentOptions.MidlineRight;
            amount.raycastTarget = false;
        }

        public void Set(ResourceType resource, int cost)
        {
            bool visible = resource != null && cost > 0;
            icon.transform.parent.gameObject.SetActive(visible);
            if (!visible) return;
            ResourceIconSizing.Apply(icon, resource.resourceIcon);
            amount.text = cost.ToString();
            amount.color = GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(resource) >= cost
                ? new Color(.55f, .85f, .48f) : new Color(.99f, .49f, .50f);
        }
    }
}
