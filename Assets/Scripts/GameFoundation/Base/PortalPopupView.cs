using System;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Scene-authored destination list and details share the same location data as the former tooltip.</summary>
    public sealed class PortalPopupView : MonoBehaviour
    {
        [Serializable] public sealed class ResourceRow
        {
            public GameObject root;
            public Image icon;
            public Text abundance;
        }
        public PortalLocationRow[] locations;
        public Button closeButton, travelButton;
        public Text portalLevel, title, description, status, travelLabel;
        public Image[] dangerSkulls;
        public ResourceRow[] resourceRows;
        public Color readyColor = new(.58f, .77f, .52f);
        public Color lockedColor = new(.84f, .68f, .45f);
        public PortalLocationDefinition Selected { get; private set; }
        public bool expeditionLayout;
        private PortalExpeditionPanel expedition;
        private Image accessOverlay;
        private Button accessButton;
        private PriceButtonLayout accessPrice;
        private Image accessIconA, accessIconB;
        private Text accessAmountA, accessAmountB;
        private Sprite activeExpeditionSkull, inactiveExpeditionSkull;
        internal void SetExpeditionSkulls(Sprite active, Sprite inactive)
        {
            activeExpeditionSkull = active;
            inactiveExpeditionSkull = inactive;
        }
        private DayCycleService day;
        private LocalizationService localization;

        private void OnEnable()
        {
            foreach (var upgrade in GetComponentsInChildren<BuildingUpgradeButton>(true))
                upgrade.transform.parent.gameObject.SetActive(false);
            if (expeditionLayout && expedition == null) expedition = new PortalExpeditionPanel(this);
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            // Runtime UnityEvent listeners are lost when scripts reload in Play Mode.
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
            travelButton.onClick.RemoveListener(Travel);
            travelButton.onClick.AddListener(Travel);
            foreach (var row in locations) if (row != null) row.Bind(this);
            BuildingUpgradeService.Changed += Refresh;
            CraftProduction.Changed += Refresh;
            Subscribe();
            Refresh();
        }
        private void Start() { Subscribe(); Refresh(); }
        private void Subscribe()
        {
            if (day == null && DayCycleService.Instance != null) { day = DayCycleService.Instance; day.Changed += Refresh; }
            if (localization == null && LocalizationService.Instance != null) { localization = LocalizationService.Instance; localization.LanguageChanged += Refresh; }
        }
        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            closeButton.onClick.RemoveListener(Close);
            travelButton.onClick.RemoveListener(Travel);
            BuildingUpgradeService.Changed -= Refresh;
            CraftProduction.Changed -= Refresh;
            if (day != null) day.Changed -= Refresh;
            if (localization != null) localization.LanguageChanged -= Refresh;
            day = null; localization = null;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();
        public void Close() => PopupMotion.Hide(gameObject);
        public void Select(PortalLocationDefinition location)
        {
            if (location == null || !locations.Any(row => row != null && row.location == location)) return;
            Selected = location;
            Refresh();
        }
        public void Refresh()
        {
            if (locations == null || locations.Length == 0) return;
            if (Selected == null)
                Selected = locations.FirstOrDefault(row => row != null && row.location != null && row.location.LocationId == DayCycleService.CurrentPortalLocationId)?.location
                    ?? locations.FirstOrDefault(row => row != null && row.location != null)?.location;
            if (Selected == null) return;
            foreach (var row in locations) if (row != null && row.location != null) row.Refresh(Selected);
            portalLevel.text = string.Format(Tr("base.portal.level", GameFoundation.Localization.GameText.Get("Уровень портала: {0} / {1}")), PortalProgression.Level, PortalProgression.MaxLevel);
            title.text = expeditionLayout ? PortalProgression.Name(Selected).ToUpperInvariant() : PortalProgression.Name(Selected);
            description.text = Tr(Selected.DescriptionKey, Selected.FallbackDescription);
            for (int i = 0; i < dangerSkulls.Length; i++)
            {
                var skull = dangerSkulls[i];
                if (skull == null) continue;
                bool active = i < Selected.DangerSkulls;
                if (activeExpeditionSkull == null) skull.gameObject.SetActive(active);
                else
                {
                    skull.sprite = active ? activeExpeditionSkull : inactiveExpeditionSkull ?? activeExpeditionSkull;
                    skull.color = active || inactiveExpeditionSkull != null ? Color.white : new Color(.18f, .16f, .19f);
                }
            }
            for (int i = 0; i < resourceRows.Length; i++)
            {
                var row = resourceRows[i];
                bool visible = i < Selected.Resources.Count && Selected.Resources[i].resource != null;
                row.root.SetActive(visible);
                if (!visible) continue;
                var resource = Selected.Resources[i];
                ResourceIconSizing.Apply(row.icon, resource.resource.resourceIcon);
                row.abundance.text = Tr(resource.abundanceKey, resource.fallbackAbundance);
                row.abundance.color = PortalResourceAbundance.ColorFor(resource.abundanceKey);
            }
            bool unlocked = PortalProgression.IsUnlocked(Selected);
            RefreshAccess(unlocked);
            var service = DayCycleService.Instance;
            bool entered = service?.EnteredToday == true;
            bool hasResidents = service?.HasExpeditionResidents == true;
            bool lacksResidents = unlocked && !entered && !hasResidents;
            travelButton.interactable = unlocked && !entered && hasResidents;
            BaseDisabledButtonHint.Set(travelButton, entered ? BaseDisabledButtonHint.Reason.TravelUsed :
                PortalProgression.IsDemoLocked(Selected) ? BaseDisabledButtonHint.Reason.Demo : BaseDisabledButtonHint.Reason.PortalLevel,
                reasonText: lacksResidents ? ExpeditionResidentsRequiredText : null);
            travelLabel.text = Tr("base.portal.depart", GameFoundation.Localization.GameText.Get("Отправиться"));
            status.text = entered ? Tr("base.portal.travel_used", "Сегодня вы уже пользовались порталом.") : !unlocked ? string.Format(Tr("base.portal.requires_level", GameFoundation.Localization.GameText.Get("Улучшите портал до ур. {0}")), Selected.RequiredPortalLevel) :
                lacksResidents ? ExpeditionResidentsRequiredText : Tr("base.portal.travel_once", "Путешествовать через портал можно только один раз в день.");
            status.color = travelButton.interactable ? readyColor : lockedColor;
            expedition?.Refresh();
        }

        private void RefreshAccess(bool unlocked)
        {
            description.gameObject.SetActive(unlocked);
            if (unlocked)
            {
                if (accessOverlay != null) accessOverlay.gameObject.SetActive(false);
                return;
            }
            if (accessOverlay == null) CreateAccessOverlay();
            accessOverlay.gameObject.SetActive(true);
            var price = PortalProgression.AccessPrice(Selected);
            bool demoLocked = PortalProgression.IsDemoLocked(Selected);
            bool next = PortalProgression.IsNextDestination(Selected);
            accessButton.interactable = !demoLocked && next && BuildingUpgradeService.CanUpgrade(PortalProgression.BuildingId);
            BaseDisabledButtonHint.Set(accessButton, demoLocked ? BaseDisabledButtonHint.Reason.Demo :
                next ? BaseDisabledButtonHint.Reason.Resources : BaseDisabledButtonHint.Reason.PortalLevel,
                reasonText: !demoLocked && !next ? Tr("base.portal.previous_upgrade", "Требуется предыдущее улучшение") : null);
            accessPrice.label.text = Tr("base.portal.upgrade_action", "Улучшить портал");
            BuildingUpgradeButton.SetPrice(accessIconA, accessAmountA, price?.resourceA, price?.costA ?? 0);
            BuildingUpgradeButton.SetPrice(accessIconB, accessAmountB, price?.resourceB, price?.costB ?? 0);
            accessPrice.Refresh();
        }

        private void CreateAccessOverlay()
        {
            var detail = (RectTransform)title.transform.parent;
            var theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
            accessOverlay = new GameObject("Destination locked overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var overlay = accessOverlay.rectTransform;
            overlay.SetParent(detail, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = new Vector2(8, 8);
            overlay.offsetMax = new Vector2(-8, -8);
            accessOverlay.color = new Color(.07f, .055f, .08f, .16f);
            accessOverlay.raycastTarget = false;

            var visual = new GameObject("Upgrade portal", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)visual.transform;
            rect.SetParent(overlay, false);
            rect.anchorMin = new Vector2(0, .5f);
            rect.anchorMax = new Vector2(1, .5f);
            var descriptionRect = description.rectTransform;
            rect.sizeDelta = new Vector2(descriptionRect.rect.width - detail.rect.width + 16, 64);
            rect.anchoredPosition = new Vector2(0, descriptionRect.anchoredPosition.y + (descriptionRect.rect.height - rect.rect.height) * .5f);
            var image = visual.GetComponent<Image>();
            image.sprite = theme.textButtonSprite;
            image.type = Image.Type.Sliced;
            accessButton = visual.GetComponent<Button>();
            accessButton.targetGraphic = image;
            accessButton.transition = Selectable.Transition.None;
            accessButton.onClick.AddListener(() =>
            {
                if (accessButton.IsInteractable() && PortalProgression.TryUnlockDestination(Selected))
                    GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.BuildingComplete);
            });
            visual.AddComponent<UnifiedButtonFeedback>();
            var label = AccessText(rect, "Label");
            visual.AddComponent<ActionButtonLabelColor>().Configure(label);
            accessIconA = AccessIcon(rect);
            accessAmountA = AccessText(rect, "Amount");
            accessIconB = AccessIcon(rect);
            accessAmountB = AccessText(rect, "Amount");
            accessPrice = visual.AddComponent<PriceButtonLayout>();
            accessPrice.visual = rect;
            accessPrice.label = label;
            accessPrice.prices = new[] { accessIconA.rectTransform, accessAmountA.rectTransform, accessIconB.rectTransform, accessAmountB.rectTransform };
        }

        private Text AccessText(Transform parent, string name)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = title.font;
            label.raycastTarget = false;
            QuestReadingText.Ensure(label);
            return label;
        }

        private static Image AccessIcon(Transform parent)
        {
            var icon = new GameObject("Resource", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(parent, false);
            icon.raycastTarget = false;
            return icon;
        }
        public void Travel()
        {
            if (PortalCloudTransition.IsLoading || (travelButton != null && !travelButton.IsInteractable())) return;
            var service = DayCycleService.Instance;
            var router = FindFirstObjectByType<RunSceneRouter>();
            if (service == null || router == null || Selected == null || !router.CanEnterRun) return;
            var site = service.Portals.FirstOrDefault(s => s.locationId == Selected.LocationId);
            if (service.Enter(site)) router.EnterRun();
            else Refresh();
        }
        internal static string ExpeditionResidentsRequiredText =>
            Tr("base.portal.minimum_residents", "Для вылазки необходимо два свободных человека.");

        public static string Tr(string key, string fallback)
        {
            var value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
