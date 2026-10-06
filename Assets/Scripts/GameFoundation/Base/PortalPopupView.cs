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
        private DayCycleService day;
        private LocalizationService localization;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            travelButton.onClick.AddListener(Travel);
            foreach (var row in locations) if (row != null) row.Bind(this);
        }
        private void OnEnable()
        {
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
            BuildingUpgradeService.Changed -= Refresh;
            if (day != null) day.Changed -= Refresh;
            if (localization != null) localization.LanguageChanged -= Refresh;
            day = null; localization = null;
        }
        public void Close() => gameObject.SetActive(false);
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
            portalLevel.text = string.Format(Tr("base.portal.level", "Уровень портала: {0} / {1}"), PortalProgression.Level, PortalProgression.MaxLevel);
            title.text = PortalProgression.Name(Selected);
            description.text = Tr(Selected.DescriptionKey, Selected.FallbackDescription);
            for (int i = 0; i < dangerSkulls.Length; i++)
                if (dangerSkulls[i] != null) dangerSkulls[i].gameObject.SetActive(i < Selected.Difficulty);
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
            travelLabel.text = Tr("base.portal.depart", "Отправиться");
            status.text = !unlocked ? string.Format(Tr("base.portal.requires_level", "Улучшите портал до ур. {0}"), Selected.RequiredPortalLevel) :
                entered ? Tr("base.portal.travel_used", "На сегодня вы исчерпали эту возможность.") : Tr("base.portal.ready", "Портал готов к переходу");
            status.color = unlocked && !entered ? readyColor : lockedColor;
        }
        public void Travel()
        {
            var service = DayCycleService.Instance;
            var router = FindFirstObjectByType<RunSceneRouter>();
            if (service == null || router == null || Selected == null) return;
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
