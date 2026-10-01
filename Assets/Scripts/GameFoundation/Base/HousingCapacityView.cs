using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class HousingCapacityView : MonoBehaviour
    {
        [SerializeField] private Text capacityLabel;
        [SerializeField] private Text amount;
        [SerializeField] private Image icon;
        private LocalizationService localization;
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
        }
        private void ResourceChanged(ResourceType _, int __) => Refresh();
        private void Refresh()
        {
            var resource = BuildingUpgradeService.Catalog?.Find("housing")?.capacityResource;
            var text = localization?.Get("base.housing.capacity");
            capacityLabel.text = string.IsNullOrEmpty(text) || text == "base.housing.capacity" ? "Места для жителей" : text;
            int count = resource != null && GlobalResourceManager.Instance != null ? GlobalResourceManager.Instance.GetResourceAmount(resource) : 0;
            amount.text = count + " / " + BuildingUpgradeService.Capacity("housing");
            ResourceIconSizing.Apply(icon, resource != null ? resource.resourceIcon : null);
        }
    }
}
