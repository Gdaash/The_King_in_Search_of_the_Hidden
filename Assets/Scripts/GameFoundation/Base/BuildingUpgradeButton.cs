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
        private bool hovering;
        private LocalizationService localization;
        private void Awake() { button.onClick.AddListener(Upgrade); }
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
            bool visible = BuildingUpgradeService.IsBuilt(buildingId) && level < max;
            bool affordable = visible && BuildingUpgradeService.CanUpgrade(buildingId);
            visibility.alpha = visible ? 1 : 0;
            visibility.blocksRaycasts = visible;
            visibility.interactable = visible;
            button.interactable = affordable;
            if (availabilityMarker != null) availabilityMarker.SetActive(affordable);
            if (!visible && hovering)
            {
                hovering = false;
                BuildingConstructionTooltip.Instance?.Hide();
            }
            label.text = (level < max ? Tr("base.upgrade.action", "Улучшить") : Tr("base.upgrade.max", "Максимум")) + " " + level + "/" + max;
            if (hovering) ShowTooltip();
        }
        public void OnPointerEnter(PointerEventData _) { hovering = true; ShowTooltip(); }
        public void OnPointerExit(PointerEventData _) { hovering = false; BuildingConstructionTooltip.Instance?.Hide(); }
        private void ShowTooltip()
        {
            if (!BuildingUpgradeService.IsBuilt(buildingId)) return;
            var definition = BuildingUpgradeService.Catalog?.Find(buildingId);
            var next = BuildingUpgradeService.Next(buildingId);
            if (definition == null || next == null) { BuildingConstructionTooltip.Instance?.Hide(); return; }
            int capacity = BuildingUpgradeService.Capacity(buildingId);
            string effect = definition.effect == BuildingUpgradeCatalog.UpgradeEffect.PortalAccess
                ? string.Format(Tr("base.portal.upgrade_effect", "Открывает локацию: {0}\nДоступ сохраняется между походами."), PortalProgression.DestinationsAt(BuildingUpgradeService.Level(buildingId) + 1))
                : string.Format(Tr("base.upgrade.effect", "Дополнительные места: +{0}\nВместимость: {1} → {2}"), next.additionalCapacity, capacity, capacity + next.additionalCapacity);
            BuildingConstructionTooltip.Instance?.Show(Tr(definition.nameKey, definition.displayName),
                effect,
                Tr("base.upgrade.price", "Цена улучшения"), next.resourceA, next.costA, next.resourceB, next.costB);
        }
        private static string Tr(string key, string fallback)
        {
            string result = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(result) || result == key ? fallback : result;
        }
    }
}
