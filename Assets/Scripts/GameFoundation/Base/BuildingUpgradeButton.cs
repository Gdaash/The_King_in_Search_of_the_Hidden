using GameFoundation.Audio;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BuildingUpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string buildingId;
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private GameObject availabilityMarker;
        [SerializeField] private GameFoundation.UI.BuildingButtonHighlight highlight;
        [SerializeField] private Text effectLabel, priceLabel, amountA, amountB, statusLabel;
        [SerializeField] private Image iconA, iconB;
        private bool hovering;
        private LocalizationService localization;
        private void Awake()
        {
            if (visibility == null) visibility = GetComponent<CanvasGroup>();
            if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
            button.onClick.AddListener(Upgrade);
        }
        private void OnEnable()
        {
            BuildingUpgradeService.Changed += Refresh;
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            BuildingUpgradeService.Changed -= Refresh;
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            if (localization != null) localization.LanguageChanged -= Refresh;
            if (hovering) BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
        }
        private void ResourceChanged(ResourceType _, int __) => Refresh();
        private void Upgrade()
        {
            if (BuildingUpgradeService.TryUpgrade(buildingId))
                GameAudioController.PlayUI(GameAudioCue.BuildingComplete, .9f, .98f, 1.02f, .1f);
        }
        private void Refresh()
        {
            int level = BuildingUpgradeService.Level(buildingId);
            int max = BuildingUpgradeService.Catalog?.Find(buildingId)?.levels.Count ?? 0;
            bool visible = BuildingUpgradeService.IsBuilt(buildingId);
            bool affordable = visible && level < max && BuildingUpgradeService.CanUpgrade(buildingId);
            visibility.alpha = visible ? 1 : 0;
            visibility.blocksRaycasts = visible;
            visibility.interactable = visible;
            button.interactable = affordable;
            BaseDisabledButtonHint.Set(button, buildingId == PortalProgression.BuildingId && PortalProgression.DemoRestrictionsEnabled ? BaseDisabledButtonHint.Reason.Demo : level >= max ? BaseDisabledButtonHint.Reason.Maximum : BaseDisabledButtonHint.Reason.Resources);
            if (button.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = true;
            if (availabilityMarker != null) availabilityMarker.SetActive(false);
            if (highlight != null) highlight.SetAvailable(affordable);
            if (!visible && hovering)
            {
                hovering = false;
                BuildingConstructionTooltip.Instance?.Hide();
            }
            label.text = (level < max ? Tr("base.upgrade.action", GameFoundation.Localization.GameText.Get("Улучшить")) : Tr("base.upgrade.max", GameFoundation.Localization.GameText.Get("Максимум"))) + " " + level + "/" + max;
            if (buildingId == PortalProgression.BuildingId && PortalProgression.DemoRestrictionsEnabled)
                label.text = Tr("base.portal.demo_unavailable", "Недоступно в демо");
            RefreshDetails(affordable);
            if (hovering) ShowTooltip();
        }
        private void RefreshDetails(bool affordable)
        {
            if (effectLabel == null) return;
            var panel = (RectTransform)transform.parent;
            if (TryGetComponent<GameFoundation.UI.BuildingButtonLayout>(out var automaticLayout)) automaticLayout.enabled = false;
            ((RectTransform)transform).sizeDelta = new Vector2(300, 112);
            if (button.targetGraphic != null && button.targetGraphic.transform != transform)
            {
                var visual = button.targetGraphic.rectTransform;
                visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one;
                visual.offsetMin = visual.offsetMax = Vector2.zero;
            }
            label.fontSize = 20;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.sizeDelta = new Vector2(260, 48);
            label.rectTransform.anchoredPosition = new Vector2(0, 26);
            ((RectTransform)transform).anchoredPosition = new Vector2(panel.rect.width * .5f - 182, 12);
            float descriptionWidth = panel.rect.width - 396;
            effectLabel.rectTransform.sizeDelta = new Vector2(descriptionWidth, 100);
            effectLabel.rectTransform.anchoredPosition = new Vector2(-panel.rect.width * .5f + 32 + descriptionWidth * .5f, 22);
            statusLabel.rectTransform.sizeDelta = new Vector2(descriptionWidth, 44);
            statusLabel.rectTransform.anchoredPosition = new Vector2(effectLabel.rectTransform.anchoredPosition.x, -52);
            statusLabel.alignment = TextAnchor.MiddleLeft;
            var price = (RectTransform)priceLabel.transform.parent;
            price.SetParent(transform, false);
            price.sizeDelta = new Vector2(260, 48);
            price.anchoredPosition = new Vector2(0, -28);
            price.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            priceLabel.gameObject.SetActive(false);
            var definition = BuildingUpgradeService.Catalog?.Find(buildingId);
            var next = BuildingUpgradeService.Next(buildingId);
            effectLabel.text = definition != null && next != null ? Effect(definition, next) : Tr("base.upgrade.max", "Максимум");
            priceLabel.text = next != null ? Tr("base.upgrade.price", "Цена улучшения") : "";
            statusLabel.text = "";
            statusLabel.gameObject.SetActive(false);
            SetPrice(iconA, amountA, next?.resourceA, next?.costA ?? 0);
            SetPrice(iconB, amountB, next?.resourceB, next?.costB ?? 0);
        }
        private static void SetPrice(Image icon, Text amount, ResourceType resource, int cost)
        {
            bool show = resource != null && cost > 0;
            icon.gameObject.SetActive(show); amount.gameObject.SetActive(show);
            if (!show) return;
            ResourceIconSizing.Apply(icon, resource.resourceIcon);
            if (icon.TryGetComponent<LayoutElement>(out var layout))
            {
                layout.preferredWidth = icon.rectTransform.rect.width;
                layout.preferredHeight = icon.rectTransform.rect.height;
            }
            amount.text = cost.ToString();
            amount.color = GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(resource) >= cost
                ? new Color(.55f, .85f, .54f) : new Color(1f, .38f, .37f);
        }
        private string Effect(BuildingUpgradeCatalog.Building definition, BuildingUpgradeCatalog.Level next)
        {
            int capacity = BuildingUpgradeService.Capacity(buildingId);
            return definition.effect == BuildingUpgradeCatalog.UpgradeEffect.PortalAccess
                ? string.Format(Tr("base.portal.upgrade_effect", GameText.Get("Открывает локацию: {0}\nДоступ сохраняется между походами.")), PortalProgression.DestinationsAt(BuildingUpgradeService.Level(buildingId) + 1))
                : string.Format(Tr("base.upgrade.effect", GameText.Get("Дополнительные места: +{0}\nВместимость: {1} → {2}")), next.additionalCapacity, capacity, capacity + next.additionalCapacity);
        }
        public void OnPointerEnter(PointerEventData _) { hovering = true; ShowTooltip(); }
        public void OnPointerExit(PointerEventData _) { hovering = false; BuildingConstructionTooltip.Instance?.Hide(); }
        private void ShowTooltip()
        {
            if (effectLabel != null) return;
            if (buildingId == PortalProgression.BuildingId && PortalProgression.DemoRestrictionsEnabled) { BuildingConstructionTooltip.Instance?.Hide(); return; }
            if (!BuildingUpgradeService.IsBuilt(buildingId)) return;
            var definition = BuildingUpgradeService.Catalog?.Find(buildingId);
            var next = BuildingUpgradeService.Next(buildingId);
            if (definition == null || next == null) { BuildingConstructionTooltip.Instance?.Hide(); return; }
            int capacity = BuildingUpgradeService.Capacity(buildingId);
            string effect = definition.effect == BuildingUpgradeCatalog.UpgradeEffect.PortalAccess
                ? string.Format(Tr("base.portal.upgrade_effect", GameFoundation.Localization.GameText.Get("Открывает локацию: {0}\nДоступ сохраняется между походами.")), PortalProgression.DestinationsAt(BuildingUpgradeService.Level(buildingId) + 1))
                : string.Format(Tr("base.upgrade.effect", GameFoundation.Localization.GameText.Get("Дополнительные места: +{0}\nВместимость: {1} → {2}")), next.additionalCapacity, capacity, capacity + next.additionalCapacity);
            BuildingConstructionTooltip.Instance?.Show(Tr(definition.nameKey, definition.displayName),
                effect,
                Tr("base.upgrade.price", GameFoundation.Localization.GameText.Get("Цена улучшения")), next.resourceA, next.costA, next.resourceB, next.costB);
        }
        private static string Tr(string key, string fallback)
        {
            string result = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(result) || result == key ? fallback : result;
        }
    }
}
