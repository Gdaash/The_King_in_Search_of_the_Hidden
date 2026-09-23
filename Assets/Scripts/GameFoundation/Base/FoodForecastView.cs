using GameFoundation.MetaProgression;
using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class FoodForecastView : MonoBehaviour
    {
        [SerializeField] private ResourceType human;
        [SerializeField] private ResourceType berries;
        [SerializeField] private Image residentsIcon;
        [SerializeField] private Image berriesIcon;
        [SerializeField] private Image starvationIcon;
        [SerializeField] private Text residentsLabel;
        [SerializeField] private Text foodLabel;
        [SerializeField] private Text starvationLabel;
        [SerializeField] private Text residentsAmount;
        [SerializeField] private Text berriesAmount;
        [SerializeField] private Text starvationAmount;
        [SerializeField] private Color normalColor = new Color(0.94f, 0.91f, 0.82f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.47f, 0.47f, 1f);

        public void SetData(DayCycleService.FoodForecast forecast)
        {
            SetIcon(residentsIcon, human);
            SetIcon(berriesIcon, berries);
            SetIcon(starvationIcon, human);
            if (residentsLabel != null) residentsLabel.text = Tr("base.food_forecast.residents", "Жители");
            if (foodLabel != null) foodLabel.text = Tr("base.food_forecast.food", "Еда");
            if (starvationLabel != null) starvationLabel.text = Tr("base.food_forecast.deaths", "Умрёт от голода");
            SetAmount(residentsAmount, forecast.Residents, normalColor);
            SetAmount(berriesAmount, forecast.Berries, normalColor);
            SetAmount(starvationAmount, forecast.Starving,
                forecast.Starving > 0 ? dangerColor : normalColor);
            if (starvationIcon != null)
                starvationIcon.color = forecast.Starving > 0 ? dangerColor : Color.white;
        }

        private static void SetIcon(Image image, ResourceType resource)
        {
            if (image == null) return;
            ResourceIconSizing.Apply(image, resource != null ? resource.resourceIcon : null);
            image.enabled = image.sprite != null;
        }

        private static void SetAmount(Text text, int amount, Color color)
        {
            if (text == null) return;
            text.text = amount.ToString();
            text.color = color;
        }

        private static string Tr(string key, string fallback)
        {
            var value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value.TrimEnd(':');
        }
    }
}
