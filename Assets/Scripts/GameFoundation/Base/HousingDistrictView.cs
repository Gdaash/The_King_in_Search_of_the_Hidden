using System;
using System.Collections.Generic;
using System.Globalization;
using GameFoundation.Audio;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class HousingDistrictView : MonoBehaviour
    {
        private const float CardWidth = 348;
        private static readonly Color Ink = new(.94f, .88f, .73f);
        private static readonly Color Heading = new(.72f, .66f, .78f);
        private static readonly Color Sufficient = new(.55f, .85f, .47f);
        private static readonly Color Missing = new(1f, .35f, .36f);
        private readonly List<Card> cards = new();
        private TMP_Text summary, foodSummary, hungerSummary;
        private Card convertHen;
        private Image summaryIcon, foodSummaryIcon, hungerSummaryIcon;
        private Sprite frame, star;
        private ButtonVisualTheme theme;
        private TMP_FontAsset font;
        private LocalizationService language;
        private DayCycleService day;
        private bool dirty;

        private sealed class Card
        {
            public TMP_Text title, description, action;
            public TMP_Text lockMessage;
            public Button button;
            public RisingLabelStars stars;
            public readonly Image[] icons = new Image[2];
            public readonly TMP_Text[] amounts = new TMP_Text[2];
            public readonly TMP_Text[] stats = new TMP_Text[4];
            public readonly Image[] statIcons = new Image[4];
        }

        public static void Install(GameObject popup)
        {
            if (popup == null || popup.GetComponent<HousingDistrictView>() != null) return;
            var window = popup.transform.Find("Window") as RectTransform;
            var artwork = window?.Find("Artwork Frame") as RectTransform;
            if (window == null || artwork == null || !artwork.TryGetComponent<Image>(out var image)) return;
            var legacyStar = window.GetComponentInChildren<RisingLabelStars>(true)?.StarSprite;
            foreach (string path in new[] { "Description", "Residents Panel", "Construction Panel" })
                if (window.Find(path) is Transform legacy) legacy.gameObject.SetActive(false);
            window.sizeDelta = new Vector2(1500, 960);
            window.anchoredPosition = Vector2.zero;
            artwork.anchorMin = Vector2.zero;
            artwork.anchorMax = Vector2.one;
            artwork.offsetMin = artwork.offsetMax = Vector2.zero;
            if (window.Find("Title") is RectTransform title)
            {
                title.anchorMin = title.anchorMax = title.pivot = Vector2.one * .5f;
                title.anchoredPosition = new Vector2(0, 437);
            }
            var view = popup.AddComponent<HousingDistrictView>();
            view.Build(window, image.sprite, legacyStar);
            if (window.Find("Close") is Transform close) close.SetAsLastSibling();
            window.GetComponent<CanvasWindowFit>()?.Fit();
        }

        private void Build(RectTransform window, Sprite panel, Sprite particles)
        {
            frame = panel;
            star = particles;
            theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
            font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            var summaryBar = Rect(window, "District overview", 1130, 44, 0, 386);
            summary = Summary(summaryBar, "Capacity", -295, out summaryIcon);
            foodSummary = Summary(summaryBar, "Food", 96, out foodSummaryIcon);
            hungerSummary = Summary(summaryBar, "Starvation forecast", 376, out hungerSummaryIcon);
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                bool house = i < HousingDistrict.HouseCount;
                float height = house ? 154 : 446;
                var root = Rect(window, "District card " + (i + 1), CardWidth, height,
                    -549 + i % 4 * 366, i < 4 ? 274 : i < 8 ? 102 : -216);
                Panel(root, frame, false);
                if (i == 11) continue;
                var card = new Card();
                cards.Add(card);
                card.title = Label(root, "Building", 308, 44, 0, height / 2 - 31, 28, TextAlignmentOptions.Left);
                card.title.color = Heading;
                card.description = Label(root, "Description", 308, house ? 40 : 84, 0, house ? 10 : 126, 22, TextAlignmentOptions.TopLeft);
                card.description.textWrappingMode = TextWrappingModes.Normal;
                if (!house)
                    for (int j = 0; j < (i == 10 ? 1 : 3); j++)
                    {
                        float y = 50 - 54 * j;
                        card.stats[j] = Label(root, "Production " + j, 200, 40, -54, y, 22, TextAlignmentOptions.Left);
                        card.statIcons[j] = Icon(root, 74, y);
                    }
                BuildAction(root, card, "Action", -height / 2 + 37, () => Purchase(index));
                if (i == 8)
                {
                    convertHen = new Card();
                    BuildAction(root, convertHen, "Convert hen", -height / 2 + 97, () =>
                    {
                        if (HousingDistrict.TryConvertHenToFood())
                            GameAudioController.PlayUI(GameAudioCue.BuildingComplete);
                        Refresh();
                    });
                }
                if (house)
                {
                    var overlay = Rect(root, "House construction lock", CardWidth - 16, height - 16, 0, 0);
                    Panel(overlay, null, true).color = new Color(.105f, .095f, .115f, .96f);
                    var icon = Icon(overlay, 0, 29);
                    ResourceIconSizing.Apply(icon, theme?.lockIconSprite);
                    icon.enabled = icon.sprite != null;
                    icon.color = new Color(.64f, .59f, .68f);
                    card.lockMessage = Label(overlay, "Lock reason", 296, 64, 0, -28, 22, TextAlignmentOptions.Center);
                    card.lockMessage.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            Refresh();
        }

        private void BuildAction(RectTransform root, Card card, string name, float y, UnityEngine.Events.UnityAction onClick)
        {
            var hit = Rect(root, name, 308, 52, 0, y);
            var image = Panel(hit, theme != null ? theme.textButtonSprite : frame, true);
            card.button = hit.gameObject.AddComponent<Button>();
            card.button.targetGraphic = image;
            card.button.transition = Selectable.Transition.None;
            hit.gameObject.AddComponent<UnifiedButtonFeedback>();
            card.action = Label(hit, "Caption", 276, 44, 0, 0, 22, TextAlignmentOptions.Left);
            card.action.textWrappingMode = TextWrappingModes.NoWrap;
            hit.gameObject.AddComponent<ActionButtonLabelColor>().Configure(null, card.action);
            if (star != null)
            {
                card.stars = Rect(hit, "Available action particles", 308, 52, 0, 0).gameObject.AddComponent<RisingLabelStars>();
                card.stars.Configure(card.action.rectTransform, star);
                card.stars.color = theme != null ? theme.positiveLabelColor : Sufficient;
            }
            for (int j = 0; j < 2; j++)
            {
                card.icons[j] = Icon(hit, 0, 0);
                card.amounts[j] = Label(hit, "Price amount", 26, 44, 0, 0, 22, TextAlignmentOptions.Center);
            }
            card.button.onClick.AddListener(onClick);
        }

        private void OnEnable()
        {
            language = LocalizationService.Instance;
            day = DayCycleService.Instance;
            if (language != null) language.LanguageChanged += Invalidate;
            if (day != null) day.Changed += Invalidate;
            BuildingUpgradeService.Changed += Invalidate;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            SaveSlotPrefs.SelectionChanged += Invalidate;
            Refresh();
        }

        private void OnDisable()
        {
            if (language != null) language.LanguageChanged -= Invalidate;
            if (day != null) day.Changed -= Invalidate;
            BuildingUpgradeService.Changed -= Invalidate;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            SaveSlotPrefs.SelectionChanged -= Invalidate;
        }

        private void OnResourceChanged(ResourceType _, int __) => Invalidate();
        private void Invalidate() => dirty = true;
        private void LateUpdate() { if (dirty) Refresh(); }

        private void Purchase(int index)
        {
            string id = Annex(index);
            bool purchased = index < HousingDistrict.HouseCount ? HousingDistrict.TryPurchaseHouse(index)
                : BuildingUpgradeService.IsBuilt(id) ? BuildingUpgradeService.TryUpgrade(id)
                : BuildingUpgradeService.TryBuildAnnex(id);
            if (purchased) GameAudioController.PlayUI(GameAudioCue.BuildingComplete, .9f, .98f, 1.02f, .1f);
            Refresh();
        }

        private void Refresh()
        {
            if (summary == null) return;
            dirty = false;
            var stock = GlobalResourceManager.Instance;
            var human = ResourceCatalog.Find("Human");
            ResourceIconSizing.Apply(summaryIcon, human?.resourceIcon);
            summary.text = T("capacity", "Места для жителей: {0} / {1}", stock?.GetResourceAmount(human) ?? 0, HousingDistrict.Capacity);
            var production = HousingDistrict.Forecast();
            var food = ResourceCatalog.Find("Berry");
            ResourceIconSizing.Apply(foodSummaryIcon, food?.resourceIcon);
            ResourceIconSizing.Apply(hungerSummaryIcon, human?.resourceIcon);
            foodSummary.text = Translate("base.food_forecast.food", "Еда:") + " " + (stock?.GetResourceAmount(food) ?? 0);
            var dayForecast = day?.GetFoodForecast(false);
            int starving = dayForecast.HasValue ? Mathf.Max(0, dayForecast.Value.Residents - dayForecast.Value.Food - production.Food) : 0;
            hungerSummary.text = Translate("base.food_forecast.deaths", "Умрёт от голода:") + " " + starving;
            hungerSummary.color = starving > 0 ? Missing : Heading;
            if (convertHen != null)
            {
                convertHen.button.gameObject.SetActive(ExpeditionTasks.CoopBuilt);
                convertHen.button.interactable = production.Chickens > 0;
                convertHen.action.text = T("hen_to_food", "Курицу в еду");
                if (convertHen.stars != null) convertHen.stars.Emit = convertHen.button.interactable;
                Price(convertHen, new BuildingUpgradeCatalog.Level
                {
                    resourceA = ResourceCatalog.Find("Chicken"), costA = 1, costB = 0
                }, stock);
            }
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool house = i < HousingDistrict.HouseCount;
                string id = Annex(i);
                var definition = house ? null : BuildingUpgradeService.Catalog?.Find(id);
                int rank = house ? HousingDistrict.HouseRank(i)
                    : BuildingUpgradeService.IsBuilt(id) ? 1 + BuildingUpgradeService.Level(id) : 0;
                var cost = house ? HousingDistrict.HouseCost(i)
                    : rank == 0 ? definition?.constructionCost : BuildingUpgradeService.Next(id);
                bool canBuy = house ? HousingDistrict.CanPurchaseHouse(i) : HousingDistrict.CanPurchaseAnnex(id);
                string title = house ? T("house", "Дом {0}", i + 1) : Translate(definition?.nameKey, definition?.displayName ?? id);
                card.title.text = title;
                if (house)
                {
                    int capacity = HousingDistrict.HouseCapacityGain(i, 0);
                    if (rank == 2) capacity += HousingDistrict.HouseCapacityGain(i, 1);
                    card.description.text = T("house_places", "Места: {0} · Ур. {1}/2", capacity, rank);
                }
                else if (i == 8)
                {
                    card.description.text = T("coop_desc", "Разводит кур. Яйца и лишние цыплята идут в еду.");
                    Stat(card, 0, T("hens", "Курицы"), "Chicken", $"{production.Chickens} / {production.Capacity}");
                    Stat(card, 1, T("chicks_per_day", "Цыплят в день"), "Chicken", Rate((production.Chickens / 2) / 2f));
                    Stat(card, 2, T("eggs_per_day", "Яиц в день"), "Berry", Rate(production.Chickens / 3f));
                }
                else if (i == 9)
                {
                    card.description.text = T("tannery_desc", "За день превращает все принесённые шкуры в кожу.");
                    Stat(card, 0, T("hides", "Шкуры в запасе"), "Hide", stock?.GetResourceAmount(ResourceCatalog.Find("Hide")) ?? 0);
                    Stat(card, 1, T("leather_per_day", "Кожи за день"), "Leather", production.Hides);
                    Stat(card, 2, T("leather_stock", "Кожа в запасе"), "Leather", stock?.GetResourceAmount(ResourceCatalog.Find("Leather")) ?? 0);
                }
                else
                {
                    card.description.text = T("fields_desc", "Каждый день дают еду для жителей квартала.");
                    Stat(card, 0, T("food_per_day", "Еды в день"), "Berry", definition?.constructionCapacity ?? 0);
                }
                card.button.interactable = canBuy;
                bool locked = house && HousingDistrict.IsHouseLocked(i);
                if (card.lockMessage != null)
                {
                    card.lockMessage.text = T("previous_house", "Постройте предыдущий дом");
                    card.lockMessage.transform.parent.gameObject.SetActive(locked);
                }
                if (card.stars != null) card.stars.Emit = canBuy;
                card.action.text = cost == null ? (house || i == 8 ? Translate("base.upgrade.max", "Максимум") : Translate("base.housing.built", "Построено"))
                    : rank == 0 ? Translate("base.building.build", "Построить") : Translate("base.upgrade.action", "Улучшить");
                string detail = null;
                if (cost != null && !locked)
                {
                    if (house) detail = T("housing_gain", "Жилых мест: +{0}", HousingDistrict.HouseCapacityGain(i, rank));
                    else if (i == 8)
                        detail = T("coop_gain", "Мест для куриц: +{0}", rank == 0 ? definition.constructionCapacity : cost.additionalCapacity);
                }
                BaseDisabledButtonHint.Set(card.button, BaseDisabledButtonHint.Reason.Resources, detail);
                card.button.GetComponent<BaseDisabledButtonHint>().enabled = detail != null;
                Price(card, cost, stock);
            }
        }

        private static string Annex(int index) => index == 8 ? ExpeditionTasks.Coop : index == 9 ? ExpeditionTasks.Tannery : HousingDistrict.Fields;

        private static string Rate(float amount)
        {
            string locale = LocalizationService.Instance?.Language ?? "ru";
            return amount.ToString("0.##", CultureInfo.GetCultureInfo(locale));
        }

        private static void Stat(Card card, int index, string caption, string resource, int amount) =>
            Stat(card, index, caption, resource, amount.ToString());

        private static void Stat(Card card, int index, string caption, string resource, string amount)
        {
            var label = card.stats[index];
            label.text = caption;
            var icon = card.statIcons[index];
            ResourceIconSizing.Apply(icon, ResourceCatalog.Find(resource)?.resourceIcon);
            icon.enabled = icon.sprite != null;
            var value = label.transform.Find("Value")?.GetComponent<TMP_Text>();
            if (value == null)
            {
                value = Rect(label.rectTransform, "Value", 68, 40, 174, 0).gameObject.AddComponent<TextMeshProUGUI>();
                value.font = label.font;
                value.fontSize = 22;
                value.color = Ink;
                value.alignment = TextAlignmentOptions.Right;
                value.raycastTarget = false;
            }
            value.text = amount;
        }

        private TMP_Text Summary(RectTransform parent, string title, float x, out Image icon)
        {
            var row = Rect(parent, title, 340, 44, x, 0);
            icon = Icon(row, -150, 0);
            var label = Label(row, "Caption", 294, 44, 26, 0, 24, TextAlignmentOptions.Left);
            label.color = Heading;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static void Price(Card card, BuildingUpgradeCatalog.Level cost, GlobalResourceManager stock)
        {
            float right = 140;
            for (int i = 1; i >= 0; i--)
            {
                var resource = i == 0 ? cost?.resourceA : cost?.resourceB;
                int amount = (i == 0 ? cost?.costA : cost?.costB) ?? 0;
                bool visible = resource != null && amount > 0;
                card.icons[i].enabled = card.amounts[i].enabled = visible;
                if (!visible) continue;
                var number = card.amounts[i];
                number.text = amount.ToString();
                number.color = (stock?.GetResourceAmount(resource) ?? 0) >= amount ? Sufficient : Missing;
                number.rectTransform.anchoredPosition = new Vector2(right - 13, 0);
                right -= 29;
                var icon = card.icons[i];
                ResourceIconSizing.Apply(icon, resource.resourceIcon);
                float width = icon.rectTransform.rect.width;
                icon.rectTransform.anchoredPosition = new Vector2(right - width / 2, 0);
                right -= width + 4;
            }
            float labelRight = cost == null ? 138 : right;
            card.action.rectTransform.sizeDelta = new Vector2(labelRight + 136, 44);
            card.action.rectTransform.anchoredPosition = new Vector2((labelRight - 136) / 2, 0);
        }

        private static string T(string key, string fallback, params object[] args) =>
            string.Format(Translate("base.district." + key, fallback), args);
        private static string Translate(string key, string fallback)
        {
            var value = key == null ? null : LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private TMP_Text Label(RectTransform parent, string name, float width, float height, float x, float y, int size, TextAlignmentOptions alignment)
        {
            var label = Rect(parent, name, width, height, x, y).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Ink;
            label.raycastTarget = false;
            return label;
        }

        private static Image Icon(RectTransform parent, float x, float y)
        {
            var image = Rect(parent, "Resource icon", 0, 0, x, y).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Image Panel(RectTransform root, Sprite sprite, bool raycast)
        {
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = raycast;
            return image;
        }

        private static RectTransform Rect(Transform parent, string name, float width, float height, float x, float y)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }
    }
}
