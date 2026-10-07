using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;
namespace GameFoundation.Base
{
    public sealed class ForestForagingView : MonoBehaviour
    {
        public ForestForagingSettings settings;
        public FoodForecastView forecast;
        public RectTransform frame, title, close, forecastBlock, confirm, cancel;
        public GameObject block, offer, results, timer;
        public Text warning, timerLabel, survivorsLabel, deathsLabel, foodAmount, priceAmount;
        public Image foodIcon, priceIcon;
        public Button send;
        private void OnEnable() { send.onClick.RemoveListener(Send); send.onClick.AddListener(Send); Refresh(); }
        private void OnDisable() { if(send)send.onClick.RemoveListener(Send); }
        private void Update() => Refresh();
        public void Send()
        {
            if (ForestForagingService.TryStart(settings)) Refresh();
        }
        public void Refresh()
        {
            var day = DayCycleService.Instance;
            if (day == null || settings == null) { block.SetActive(false); return; }
            var data = ForestForagingService.Current;
            bool used = data.sent > 0 && data.day == day.Day;
            bool busy = ForestForagingService.IsPending;
            confirm.GetComponent<Button>().interactable = !busy;
            cancel.GetComponent<Button>().interactable = !busy;
            close.GetComponent<Button>().interactable = !busy;
            bool visible = used || (RoyalDecreeService.IsEnabled(RoyalDecreeService.ForestForaging) && day.GetFoodForecast().Starving > 0);
            block.SetActive(visible);
            frame.sizeDelta = new Vector2(960, visible ? 602 : 350);
            title.anchoredPosition = new Vector2(0,visible ? 244 : 118);
            close.anchoredPosition = new Vector2(442,visible ? 266 : 140);
            forecastBlock.anchoredPosition = new Vector2(0,visible ? 134 : 8);
            confirm.anchoredPosition = new Vector2(-164,visible ? -246 : -120);
            cancel.anchoredPosition = new Vector2(164,visible ? -246 : -120);
            offer.SetActive(visible && !used); timer.SetActive(visible && busy); results.SetActive(visible && used && !busy);
            send.interactable = ForestForagingService.CanStart(settings);
            priceAmount.text = settings.cost.ToString();
            warning.text = "ВНИМАНИЕ! " + Mathf.RoundToInt((1f - settings.survivalChance) * 100f) + "% шанс гибели.";
            ResourceIconSizing.Apply(priceIcon,settings.influence.resourceIcon);
            ResourceIconSizing.Apply(foodIcon,settings.food.resourceIcon);
            if (busy) timerLabel.text = "Поиски еды… " + ForestForagingService.Remaining.ToString("0.0") + " с";
            else
            {
                forecast.SetData(day.GetFoodForecast());
                survivorsLabel.text = "Выжило: " + data.survived;
                deathsLabel.text = "Погибло: " + data.dead;
                foodAmount.text = data.food.ToString();
            }
        }
    }
}
