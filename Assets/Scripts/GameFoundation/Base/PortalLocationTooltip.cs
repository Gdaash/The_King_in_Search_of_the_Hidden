using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalLocationTooltip : MonoBehaviour
    {
        [System.Serializable]
        private sealed class ResourceRow
        {
            public GameObject root;
            public Image icon;
            public Text abundance;
        }

        public static PortalLocationTooltip Instance { get; private set; }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text dangerLabel;
        [SerializeField] private Image[] dangerSkulls;
        [SerializeField] private ResourceRow[] resourceRows;
        [SerializeField] private Vector2 offset = new(230f, 0f);
        [SerializeField] private Vector2 edgePadding = new(18f, 18f);

        private PortalLocationDefinition current;
        private RectTransform currentAnchor;
        private bool languageSubscribed;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        private void OnEnable() => SubscribeLanguage();

        private void OnDisable()
        {
            if (languageSubscribed && LocalizationService.Instance != null)
                LocalizationService.Instance.LanguageChanged -= Refresh;
            languageSubscribed = false;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(PortalLocationDefinition definition, RectTransform anchor)
        {
            if (definition == null || anchor == null) return;
            current = definition;
            currentAnchor = anchor;
            Refresh();
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public void Hide()
        {
            current = null;
            currentAnchor = null;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        private void Refresh()
        {
            if (current == null) return;
            if (title != null) title.text = Tr(current.NameKey, current.FallbackName);
            if (description != null) description.text = Tr(current.DescriptionKey, current.FallbackDescription);
            if (dangerLabel != null) dangerLabel.text = Tr("base.portal.tooltip.danger", "Опасность");

            for (int i = 0; dangerSkulls != null && i < dangerSkulls.Length; i++)
                if (dangerSkulls[i] != null)
                    dangerSkulls[i].gameObject.SetActive(i < current.Difficulty);

            for (int i = 0; resourceRows != null && i < resourceRows.Length; i++)
            {
                ResourceRow row = resourceRows[i];
                bool visible = i < current.Resources.Count && current.Resources[i]?.resource != null;
                if (row?.root != null) row.root.SetActive(visible);
                if (!visible) continue;
                PortalLocationDefinition.ResourceHint hint = current.Resources[i];
                ResourceIconSizing.Apply(row.icon, hint.resource.resourceIcon);
                if (row.abundance != null)
                    row.abundance.text = Tr(hint.abundanceKey, hint.fallbackAbundance);
            }

            PositionNearAnchor();
        }

        private void PositionNearAnchor()
        {
            if (panel == null || currentAnchor == null || panel.parent is not RectTransform parent) return;
            Vector2 local = parent.InverseTransformPoint(currentAnchor.position);
            local += offset;
            Rect bounds = parent.rect;
            Vector2 half = panel.rect.size * 0.5f;
            if (local.x + half.x > bounds.xMax - edgePadding.x) local.x -= offset.x * 2f;
            local.x = Mathf.Clamp(local.x, bounds.xMin + half.x + edgePadding.x, bounds.xMax - half.x - edgePadding.x);
            local.y = Mathf.Clamp(local.y, bounds.yMin + half.y + edgePadding.y, bounds.yMax - half.y - edgePadding.y);
            panel.anchoredPosition = local;
        }

        private void SubscribeLanguage()
        {
            if (languageSubscribed || LocalizationService.Instance == null) return;
            LocalizationService.Instance.LanguageChanged += Refresh;
            languageSubscribed = true;
        }

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
