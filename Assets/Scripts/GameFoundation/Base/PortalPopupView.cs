using System;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
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
            if (day != null) day.Changed -= Refresh;
            if (localization != null) localization.LanguageChanged -= Refresh;
            day = null; localization = null;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();
        public void Close() => gameObject.SetActive(false);
        public void Select(PortalLocationDefinition location)
        {
            if (!PortalProgression.IsUnlocked(location) || !locations.Any(row => row != null && row.location == location)) return;
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
            if (!PortalProgression.IsUnlocked(Selected))
                Selected = locations.FirstOrDefault(row => row != null && PortalProgression.IsUnlocked(row.location))?.location;
            if (Selected == null) return;
            foreach (var row in locations) if (row != null && row.location != null) row.Refresh(Selected);
            portalLevel.text = string.Format(Tr("base.portal.level", GameFoundation.Localization.GameText.Get("Уровень портала: {0} / {1}")), PortalProgression.Level, PortalProgression.MaxLevel);
            title.text = expeditionLayout ? PortalProgression.Name(Selected).ToUpperInvariant() : PortalProgression.Name(Selected);
            description.text = Tr(Selected.DescriptionKey, Selected.FallbackDescription);
            for (int i = 0; i < dangerSkulls.Length; i++)
            {
                var skull = dangerSkulls[i];
                if (skull == null) continue;
                bool active = i < Selected.Difficulty;
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
            bool entered = DayCycleService.Instance != null && DayCycleService.Instance.EnteredToday;
            travelButton.interactable = unlocked && !entered && DayCycleService.Instance != null;
            BaseDisabledButtonHint.Set(travelButton, entered ? BaseDisabledButtonHint.Reason.TravelUsed : PortalProgression.IsDemoLocked(Selected) ? BaseDisabledButtonHint.Reason.Demo : BaseDisabledButtonHint.Reason.PortalLevel);
            travelLabel.text = Tr("base.portal.depart", GameFoundation.Localization.GameText.Get("Отправиться"));
            status.text = entered ? Tr("base.portal.travel_used", "Сегодня вы уже пользовались порталом.") : !unlocked ? string.Format(Tr("base.portal.requires_level", GameFoundation.Localization.GameText.Get("Улучшите портал до ур. {0}")), Selected.RequiredPortalLevel) :
                Tr("base.portal.travel_once", "Путешествовать через портал можно только один раз в день.");
            status.color = unlocked && !entered ? readyColor : lockedColor;
            expedition?.Refresh();
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
        public static string Tr(string key, string fallback)
        {
            var value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
