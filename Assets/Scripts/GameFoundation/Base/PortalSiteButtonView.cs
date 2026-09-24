using GameFoundation.MetaProgression;
using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalSiteButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string locationId;
        [SerializeField] private Button button;
        [SerializeField] private Image portalIcon;
        [SerializeField] private Image costIcon;
        [SerializeField] private Text titleText;
        [SerializeField] private Text costText;

        [Header("Цвета")]
        [SerializeField] private Color titleColor = new Color(0.94f, 0.91f, 0.82f);
        [SerializeField] private Color enoughResourceColor = new Color(0.55f, 0.85f, 0.35f);
        [SerializeField] private Color notEnoughResourceColor = new Color(0.95f, 0.38f, 0.35f);
        [SerializeField] private Color buttonNormalColor = Color.white;
        [SerializeField] private Color buttonHighlightedColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] private Color buttonPressedColor = new Color(0.78f, 0.72f, 0.78f);
        [SerializeField] private Color buttonDisabledColor = new Color(0.42f, 0.39f, 0.43f);

        private PortalLocationDefinition definition;
        public string LocationId => locationId;

        public void Refresh(PortalSite site, PortalLocationDefinition location, ResourceType ore,
            int availableOre, bool enteredToday, string enterLabel, string freeLabel)
        {
            if (site == null || location == null) return;
            definition = location;
            int price = location.ActivationCost;
            bool canPay = price == 0 || ore != null && availableOre >= price;
            if (button != null)
            {
                button.interactable = !enteredToday && (site.active || canPay);
                var colors = button.colors;
                colors.normalColor = buttonNormalColor;
                colors.highlightedColor = buttonHighlightedColor;
                colors.pressedColor = buttonPressedColor;
                colors.disabledColor = buttonDisabledColor;
                button.colors = colors;
            }
            if (titleText != null)
            {
                string locationName = Tr(location.NameKey, location.FallbackName);
                titleText.text = site.active ? enterLabel + ": " + locationName : locationName;
                titleText.color = titleColor;
                titleText.rectTransform.anchoredPosition = new Vector2(0f, site.active ? 0f : 23f);
            }
            if (costIcon != null)
            {
                costIcon.gameObject.SetActive(!site.active && price > 0);
                if (ore != null) ResourceIconSizing.Apply(costIcon, ore.resourceIcon);
            }
            if (costText != null)
            {
                costText.gameObject.SetActive(!site.active);
                costText.text = price == 0 ? freeLabel : price.ToString();
                costText.color = canPay ? enoughResourceColor : notEnoughResourceColor;
                costText.rectTransform.anchoredPosition = new Vector2(price == 0 ? 0f : 31f, -18f);
                costText.rectTransform.sizeDelta = new Vector2(price == 0 ? 170f : 60f, 40f);
            }
            if (portalIcon != null) portalIcon.color = Color.white;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            PortalLocationTooltip.Instance?.Show(definition, transform as RectTransform);

        public void OnPointerExit(PointerEventData eventData) => PortalLocationTooltip.Instance?.Hide();

        private void OnDisable() => PortalLocationTooltip.Instance?.Hide();

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
