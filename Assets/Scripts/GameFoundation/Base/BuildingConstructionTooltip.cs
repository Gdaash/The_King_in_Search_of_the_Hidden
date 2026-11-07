using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BuildingConstructionTooltip : MonoBehaviour
    {
        public static BuildingConstructionTooltip Instance { get; private set; }

        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text priceLabel;
        [SerializeField] private Image woodIcon;
        [SerializeField] private Text woodAmount;
        [SerializeField] private Image stoneIcon;
        [SerializeField] private Text stoneAmount;
        [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);
        [SerializeField] private float screenMargin = 12f;
        [SerializeField, Min(0f)] private float bottomPadding = 24f;
        [SerializeField, Min(0f)] private float resourceRowGap = 12f;
        [SerializeField, Min(0f)] private float titleTopPadding = 22f;
        [SerializeField, Min(0f)] private float titleDescriptionGap = 24f;
        [SerializeField, Min(0f)] private float titleHorizontalPadding = 48f;
        [SerializeField, Min(0f)] private float descriptionHorizontalPadding = 68f;

        private CanvasGroup group;
        private RectTransform rect;
        private ResourceType firstResource, secondResource;
        private int firstCost, secondCost;
        private GameFoundation.UI.ButtonVisualTheme theme;
        private void OnEnable() => GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void OnResourceChanged(ResourceType resource, int amount)
        {
            if (group != null && group.alpha > 0 && (resource == firstResource || resource == secondResource)) RefreshPriceColors();
        }
        private void RefreshPriceColors()
        {
            void Tint(Text label, ResourceType resource, int cost)
            {
                if (label == null || resource == null || cost <= 0) return;
                bool enough = (GlobalResourceManager.Instance != null ? GlobalResourceManager.Instance.GetResourceAmount(resource) : 0) >= cost;
                label.color = enough ? (theme != null ? theme.positiveLabelColor : new Color(.38f,.9f,.48f))
                    : (theme != null ? theme.negativeLabelColor : new Color(1f,.38f,.38f));
            }
            Tint(woodAmount,firstResource,firstCost); Tint(stoneAmount,secondResource,secondCost);
        }

        private void Awake()
        {
            Instance = this;
            theme = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme");
            group = GetComponent<CanvasGroup>();
            rect = transform as RectTransform;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string buildingName, string function, string price,
            ResourceType wood, int woodCost, ResourceType stone, int stoneCost)
        {
            if (title != null) title.text = buildingName;
            if (description != null) description.text = function;
            bool first = wood != null && woodCost > 0;
            bool second = stone != null && stoneCost > 0;
            if (priceLabel != null) priceLabel.text = first || second ? price
                : GameFoundation.Localization.LocalizationService.Instance?.Language == "en" ? "Free" : GameFoundation.Localization.GameText.Get("Бесплатно");
            firstResource = wood; firstCost = woodCost; secondResource = stone; secondCost = stoneCost;
            SetResource(woodIcon, woodAmount, wood, woodCost);
            SetResource(stoneIcon, stoneAmount, stone, stoneCost);
            RefreshPriceColors();
            CenterPrice(first, second);
            FitContent(first, second);
            group.alpha = 1f;
            group.blocksRaycasts = false;
            transform.SetAsLastSibling();
            UpdatePosition();
        }

        public void Hide()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (group != null && group.alpha > 0f) UpdatePosition();
        }

        private static void SetResource(Image image, Text amount, ResourceType resource, int cost)
        {
            if (image != null)
            {
                ResourceIconSizing.Apply(image, resource != null ? resource.resourceIcon : null);
                image.enabled = image.sprite != null && cost > 0;
            }
            if (amount != null) { amount.text = cost.ToString(); amount.enabled = resource != null && cost > 0; }
        }

        private void CenterPrice(bool first, bool second)
        {
            float Width(Image icon, Text amount) => icon != null && amount != null ? icon.rectTransform.rect.width + 12f + Mathf.Max(32f, amount.preferredWidth) : 0f;
            float a = first ? Width(woodIcon, woodAmount) : 0f;
            float b = second ? Width(stoneIcon, stoneAmount) : 0f;
            float x = -(a + b + (first && second ? 36f : 0f)) * .5f;
            void Place(Image icon, Text amount, float width)
            {
                if (icon == null || amount == null) return;
                // Older tooltip prefabs anchor these fields to the left edge.
                // Centre the whole price group relative to the panel, including single-resource prices.
                foreach (var item in new[] { icon.rectTransform, amount.rectTransform })
                {
                    var anchor = item.anchorMin; anchor.x = .5f; item.anchorMin = anchor;
                    anchor = item.anchorMax; anchor.x = .5f; item.anchorMax = anchor;
                    var pivot = item.pivot; pivot.x = .5f; item.pivot = pivot;
                }
                amount.alignment = TextAnchor.MiddleCenter;
                var p = icon.rectTransform.anchoredPosition; p.x = x + icon.rectTransform.rect.width * .5f; icon.rectTransform.anchoredPosition = p;
                amount.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(32f, amount.preferredWidth));
                p = amount.rectTransform.anchoredPosition; p.x = x + width - amount.rectTransform.rect.width * .5f; amount.rectTransform.anchoredPosition = p;
                x += width + 36f;
            }
            if (first) Place(woodIcon, woodAmount, a);
            if (second) Place(stoneIcon, stoneAmount, b);
        }

        private void FitContent(bool first, bool second)
        {
            if (rect == null) rect = transform as RectTransform;
            if (rect == null || description == null || priceLabel == null) return;

            float panelWidth = rect.rect.width;
            SetTextWidth(title, Mathf.Max(1f, panelWidth - titleHorizontalPadding));
            SetTextWidth(description, Mathf.Max(1f, panelWidth - descriptionHorizontalPadding));
            SetTextWidth(priceLabel, Mathf.Max(1f, panelWidth - descriptionHorizontalPadding));
            Canvas.ForceUpdateCanvases();

            // Wrapped title and description grow downward while resource icons keep their native size.
            float titleHeight = title != null ? Mathf.Max(title.fontSize, title.preferredHeight) : 54f;
            if (title != null) title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, titleHeight);
            float descriptionTop = titleTopPadding + titleHeight + titleDescriptionGap;
            if (description != null)
            {
                var descriptionPosition = description.rectTransform.anchoredPosition;
                descriptionPosition.y = -descriptionTop;
                description.rectTransform.anchoredPosition = descriptionPosition;
            }
            float descriptionHeight = string.IsNullOrWhiteSpace(description.text) ? 0f : description.preferredHeight;
            description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionHeight);
            float priceTop = descriptionTop + descriptionHeight + 10f;
            var pricePosition = priceLabel.rectTransform.anchoredPosition;
            pricePosition.y = -priceTop;
            priceLabel.rectTransform.anchoredPosition = pricePosition;
            float priceHeight = Mathf.Max(priceLabel.fontSize, priceLabel.preferredHeight);
            priceLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, priceHeight);
            float rowHeight = 0f;
            void Measure(Image icon, Text amount, bool visible)
            {
                if (!visible) return;
                if (icon != null) rowHeight = Mathf.Max(rowHeight, icon.rectTransform.rect.height);
                if (amount != null) rowHeight = Mathf.Max(rowHeight, amount.rectTransform.rect.height, amount.preferredHeight);
            }
            Measure(woodIcon, woodAmount, first);
            Measure(stoneIcon, stoneAmount, second);
            float rowTop = priceTop + priceHeight + (first || second ? resourceRowGap : 0f);
            void Align(RectTransform item)
            {
                if (item == null) return;
                item.anchorMin = item.anchorMax = new Vector2(.5f, 1f);
                item.pivot = new Vector2(.5f, .5f);
                var position = item.anchoredPosition;
                position.y = -(rowTop + rowHeight * .5f);
                item.anchoredPosition = position;
            }
            if (first) { Align(woodIcon != null ? woodIcon.rectTransform : null); Align(woodAmount != null ? woodAmount.rectTransform : null); }
            if (second) { Align(stoneIcon != null ? stoneIcon.rectTransform : null); Align(stoneAmount != null ? stoneAmount.rectTransform : null); }
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowTop + rowHeight + bottomPadding);
            // Keep the existing divider between the header and the content even when the title wraps.
            var divider = transform.Find("Header Divider") as RectTransform;
            if (divider != null)
            {
                var position = divider.anchoredPosition;
                position.y = -(titleTopPadding + titleHeight + titleDescriptionGap * .5f);
                divider.anchoredPosition = position;
            }
        }

        private static void SetTextWidth(Text text, float width)
        {
            if (text == null) return;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            var textRect = text.rectTransform;
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private void UpdatePosition()
        {
            if (rect == null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                (Vector2)Input.mousePosition + cursorOffset, camera, out Vector2 local);
            rect.anchoredPosition = local;
            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 correction = Vector2.zero;
            if (corners[0].x < screenMargin) correction.x += screenMargin - corners[0].x;
            if (corners[2].x > Screen.width - screenMargin) correction.x -= corners[2].x - (Screen.width - screenMargin);
            if (corners[0].y < screenMargin) correction.y += screenMargin - corners[0].y;
            if (corners[2].y > Screen.height - screenMargin) correction.y -= corners[2].y - (Screen.height - screenMargin);
            rect.position += (Vector3)correction;
        }
    }
}
