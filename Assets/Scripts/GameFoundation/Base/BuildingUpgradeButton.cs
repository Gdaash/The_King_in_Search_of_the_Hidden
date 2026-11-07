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
        private bool IsHousingAnnex => buildingId == ExpeditionTasks.Coop || buildingId == ExpeditionTasks.Tannery;
        internal void SetHousingAnnex(string id)
        {
            buildingId = id;
            if (isActiveAndEnabled) Refresh();
        }

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
            if (IsHousingAnnex ? BuildingUpgradeService.TryBuildAnnex(buildingId) : BuildingUpgradeService.TryUpgrade(buildingId))
                GameAudioController.PlayUI(GameAudioCue.BuildingComplete, .9f, .98f, 1.02f, .1f);
        }
        private void Refresh()
        {
            int level = BuildingUpgradeService.Level(buildingId);
            int max = BuildingUpgradeService.Catalog?.Find(buildingId)?.levels.Count ?? 0;
            bool annex = IsHousingAnnex;
            bool built = BuildingUpgradeService.IsBuilt(buildingId);
            bool visible = annex ? BuildingUpgradeService.IsBuilt("housing") :
                built && (buildingId != "housing" || effectLabel != null);
            bool complete = annex ? built : level >= max;
            bool affordable = visible && (annex ? BuildingUpgradeService.CanBuildAnnex(buildingId) :
                !complete && BuildingUpgradeService.CanUpgrade(buildingId));
            visibility.alpha = visible ? 1 : 0;
            visibility.blocksRaycasts = visible;
            visibility.interactable = visible;
            button.interactable = affordable;
            BaseDisabledButtonHint.Set(button, buildingId == PortalProgression.BuildingId && PortalProgression.DemoRestrictionsEnabled ? BaseDisabledButtonHint.Reason.Demo : complete ? BaseDisabledButtonHint.Reason.Maximum : BaseDisabledButtonHint.Reason.Resources);
            if (button.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = !annex || !complete;
            if (availabilityMarker != null) availabilityMarker.SetActive(false);
            if (highlight != null) highlight.SetAvailable(affordable);
            if (!visible && hovering)
            {
                hovering = false;
                BuildingConstructionTooltip.Instance?.Hide();
            }
            label.text = (level < max ? Tr("base.upgrade.action", GameFoundation.Localization.GameText.Get("Улучшить")) : Tr("base.upgrade.max", GameFoundation.Localization.GameText.Get("Максимум"))) + " " + level + "/" + max;
            if (buildingId == "housing" && !complete)
                label.text = Tr("base.housing.build_house", "Построить дом") + " " + level + "/" + max;
            if (annex)
                label.text = complete ? Tr("base.housing.built", "Построено") : buildingId == ExpeditionTasks.Coop
                    ? Tr("base.housing.build_coop", "Построить курятник") : Tr("base.housing.build_tannery", "Построить дубильню");
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
            var buttonRect = (RectTransform)transform;
            const float padding = 24f, gap = 24f;
            float buttonWidth = Mathf.Min(520, panel.rect.width * .57f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, 76);
            buttonRect.anchoredPosition = new Vector2(panel.rect.width * .5f - padding - buttonWidth * .5f, 0);
            var visual = button.targetGraphic != null ? button.targetGraphic.rectTransform : buttonRect;
            if (visual != buttonRect)
            {
                visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one;
                visual.offsetMin = visual.offsetMax = Vector2.zero;
            }
            float descriptionWidth = panel.rect.width - padding * 2 - gap - buttonWidth;
            effectLabel.rectTransform.sizeDelta = new Vector2(descriptionWidth, panel.rect.height - padding * 2);
            effectLabel.rectTransform.anchoredPosition = new Vector2(-panel.rect.width * .5f + padding + descriptionWidth * .5f, 0);
            effectLabel.alignment = TextAnchor.MiddleLeft;
            effectLabel.fontSize = 22;
            var price = (RectTransform)priceLabel.transform.parent;
            if (price.TryGetComponent<HorizontalLayoutGroup>(out var group)) group.enabled = false;
            label.transform.SetParent(visual, false);
            var items = new[] { iconA.rectTransform, amountA.rectTransform, iconB.rectTransform, amountB.rectTransform };
            foreach (var item in items) item.SetParent(visual, false);
            var layout = GetComponent<GameFoundation.UI.PriceButtonLayout>() ?? gameObject.AddComponent<GameFoundation.UI.PriceButtonLayout>();
            layout.visual = visual; layout.label = label; layout.prices = items;
            priceLabel.gameObject.SetActive(false);
            var definition = BuildingUpgradeService.Catalog?.Find(buildingId);
            var next = IsHousingAnnex
                ? (BuildingUpgradeService.IsBuilt(buildingId) ? null : definition?.constructionCost)
                : BuildingUpgradeService.Next(buildingId);
            if (IsHousingAnnex)
            {
                effectLabel.text = buildingId == ExpeditionTasks.Coop
                    ? Tr("base.housing.coop_effect", "Позволяет ловить кур во время вылазок.")
                    : Tr("base.housing.tannery_effect", "Позволяет собирать шкуры кабанов во время вылазок.");
            }
            else effectLabel.text = definition != null && next != null ? Effect(definition, next) : Tr("base.upgrade.max", "Максимум");
            priceLabel.text = next != null ? Tr("base.upgrade.price", "Цена улучшения") : "";
            statusLabel.text = "";
            statusLabel.gameObject.SetActive(false);
            SetPrice(iconA, amountA, next?.resourceA, next?.costA ?? 0);
            SetPrice(iconB, amountB, next?.resourceB, next?.costB ?? 0);
            // Keep long translations on one line without shrinking their typography.
            var readingLabel = label.GetComponentInChildren<TMPro.TMP_Text>(true);
            float captionWidth = readingLabel != null ? readingLabel.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x : label.preferredWidth;
            float priceWidth = 0;
            foreach (var item in items) if (item.gameObject.activeSelf) priceWidth += item.rect.width + 14;
            float requiredWidth = captionWidth + priceWidth + 64;
            buttonWidth = Mathf.Min(Mathf.Max(buttonWidth, requiredWidth), panel.rect.width - 352);
            // Housing construction rows share a column, including completed annexes.
            if (buildingId == "housing" || IsHousingAnnex) buttonWidth = panel.rect.width - 352;
            buttonRect.sizeDelta = new Vector2(buttonWidth, 76);
            buttonRect.anchoredPosition = new Vector2(panel.rect.width * .5f - padding - buttonWidth * .5f, 0);
            descriptionWidth = panel.rect.width - padding * 2 - gap - buttonWidth;
            effectLabel.rectTransform.sizeDelta = new Vector2(descriptionWidth, panel.rect.height - padding * 2);
            effectLabel.rectTransform.anchoredPosition = new Vector2(-panel.rect.width * .5f + padding + descriptionWidth * .5f, 0);
            layout.Refresh();
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
