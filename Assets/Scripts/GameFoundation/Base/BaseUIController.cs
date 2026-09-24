using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BaseUIController : MonoBehaviour
    {
        [SerializeField] private GameObject globalMap, laboratory, settings, warehouse, housing, refugees, square, blacksmith;
        [SerializeField] private GameObject nextDayConfirmation;
        [SerializeField] private FoodForecastView nextDayForecast, housingForecast;
        [SerializeField] private Button[] portalButtons;
        [SerializeField] private Text statusText, dayText;
        [SerializeField] private Text portalTravelNotice;
        [SerializeField] private Text portalTravelUsedNotice;
        [SerializeField, Min(0f)] private float notificationVisibleSeconds = 3f;
        [SerializeField, Min(0f)] private float notificationFadeSeconds = 2f;
        [SerializeField] private ResourceType human;
        [SerializeField] private ResourceType magicOre;
        [SerializeField] private DayReportPopup dayReportPopup;
        private bool daySubscribed;
        private bool languageSubscribed;
        private Coroutine notificationFade;

        private void Awake()
        {
            Bind("Base Panel/Portal", OpenMap);
            Bind("Base Panel/Laboratory", OpenLaboratory);
            Bind("Base Panel/Next Day", NextDay);
            Bind("Next Day Confirmation/Confirm", ConfirmNextDay);
            Bind("Next Day Confirmation/Cancel", CancelNextDay);
            Bind("Next Day Confirmation/Close", CancelNextDay);
            Bind("Base Panel/Warehouse", OpenWarehouse);
            Bind("Base Panel/Housing", OpenHousing);
            Bind("Base Panel/Refugees", OpenRefugees);
            Bind("Base Panel/Square", OpenSquare);
            Bind("Base Panel/Blacksmith", OpenBlacksmith);
            Bind("Base Panel/Settings", OpenSettings);
            Bind("Global Map/Search Portals", SearchPortals);
            Bind("Global Map/Close", CloseMap);
            Bind("Laboratory Popup/Close", CloseLaboratory);
            Bind("Warehouse Popup/Close", CloseWarehouse);
            Bind("Warehouse Popup/Buy Cart", BuyCart);
            Bind("Housing Popup/Close", CloseHousing);
            Bind("Refugees Popup/Close", CloseRefugees);
            Bind("Refugees Popup/Admit", AdmitRefugee);
            Bind("Square Popup/Close", CloseSquare);
            Bind("Blacksmith Popup/Close", CloseBlacksmith);
        }
        private void OnEnable()
        {
            SubscribeDay();
            SubscribeLanguage();
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }
        private void OnDisable()
        {
            if (notificationFade != null)
            {
                StopCoroutine(notificationFade);
                notificationFade = null;
            }
            if (daySubscribed && DayCycleService.Instance != null) DayCycleService.Instance.Changed -= Refresh;
            daySubscribed = false;
            if (languageSubscribed && LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
            languageSubscribed = false;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        }
        private void OnResourceChanged(ResourceType type, int amount)
        {
            RefreshForecasts();
            if (type == magicOre) Refresh();
        }
        private void Start()
        {
            DayResourceLedger.EnsureDay(DayCycleService.Instance != null ? DayCycleService.Instance.Day : 1);
            SubscribeDay();
            SubscribeLanguage();
            Refresh();
        }
        private void SubscribeDay()
        {
            if (daySubscribed || DayCycleService.Instance == null) return;
            DayCycleService.Instance.Changed += Refresh;
            daySubscribed = true;
        }
        private void SubscribeLanguage()
        {
            if (languageSubscribed || LocalizationService.Instance == null) return;
            LocalizationService.Instance.LanguageChanged += Refresh;
            languageSubscribed = true;
        }
        private void Refresh()
        {
            var day = DayCycleService.Instance;
            var oreAmount = GlobalResourceManager.Instance != null && magicOre != null
                ? GlobalResourceManager.Instance.GetResourceAmount(magicOre) : 0;
            if (portalTravelNotice != null)
                portalTravelNotice.text = Tr("base.portal.travel_once",
                    "Путешествовать через портал можно только один раз в день.");
            if (portalTravelUsedNotice != null)
            {
                portalTravelUsedNotice.text = Tr("base.portal.travel_used",
                    "На сегодня вы исчерпали эту возможность.");
                portalTravelUsedNotice.gameObject.SetActive(day != null && day.EnteredToday);
            }
            if (dayText) dayText.text = Tr("base.day", "День") + " " + (day?.Day ?? 1);
            var search = transform.Find("Global Map/Search Portals")?.GetComponent<Button>();
            if (search) search.gameObject.SetActive(false);
            var available = transform.Find("Refugees Popup/Available")?.GetComponent<Text>();
            if (available) available.text = Tr("base.waiting", "Ожидают:") + " " + (day?.RefugeesAvailable ?? 0);
            var admit = transform.Find("Refugees Popup/Admit")?.GetComponent<Button>();
            if (admit) admit.interactable = day != null && day.RefugeesAvailable > 0;
            for (var i = 0; portalButtons != null && i < portalButtons.Length; i++)
            {
                var button = portalButtons[i];
                if (!button) continue;
                var view = button.GetComponent<PortalSiteButtonView>();
                PortalSite site = day?.Portals.Find(item => item.locationId == view?.LocationId);
                if (site == null && day != null && i < day.Portals.Count) site = day.Portals[i];
                PortalLocationDefinition location = site != null ? DayCycleService.GetPortalLocation(site.locationId) : null;
                var visible = site != null && location != null;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                if (view != null)
                    view.Refresh(site, location, magicOre, oreAmount, day.EnteredToday,
                        Tr("base.enter", "Войти"), Tr("base.portal.free", "Бесплатно"));
                button.onClick.RemoveAllListeners();
                var portal = site;
                button.onClick.AddListener(() => SelectPortal(portal));
            }
            RefreshForecasts();
        }

        private void RefreshForecasts()
        {
            var day = DayCycleService.Instance;
            if (day == null) return;
            var forecast = day.GetFoodForecast();
            nextDayForecast?.SetData(forecast);
            housingForecast?.SetData(forecast);
        }
        private void SelectPortal(PortalSite site)
        {
            var day = DayCycleService.Instance;
            if (day == null) return;
            if (!site.active) { Message(day.Activate(site) ? "Портал активирован" : "Недостаточно магической руды"); return; }
            if (!day.Enter(site)) { Message("Сегодня уже был поход"); return; }
            FindFirstObjectByType<RunSceneRouter>()?.EnterRun();
        }
        private void Message(string value)
        {
            if (statusText == null) return;
            if (notificationFade != null) StopCoroutine(notificationFade);
            statusText.text = value;
            var color = statusText.color;
            color.a = 1f;
            statusText.color = color;
            notificationFade = StartCoroutine(FadeNotification());
        }

        private IEnumerator FadeNotification()
        {
            yield return new WaitForSecondsRealtime(notificationVisibleSeconds);
            float elapsed = 0f;
            while (elapsed < notificationFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var color = statusText.color;
                color.a = notificationFadeSeconds <= 0f ? 0f : 1f - Mathf.Clamp01(elapsed / notificationFadeSeconds);
                statusText.color = color;
                yield return null;
            }
            var finalColor = statusText.color;
            finalColor.a = 0f;
            statusText.color = finalColor;
            notificationFade = null;
        }
        private static string Tr(string key, string fallback)
        {
            var value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
        private void Show(GameObject panel, bool visible) { if (panel) panel.SetActive(visible); }
        private void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            var button = transform.Find(path)?.GetComponent<Button>();
            if (!button) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
        public void OpenMap() { Refresh(); Show(globalMap, true); }
        public void OpenLaboratory() => Show(laboratory, true);
        public void OpenSettings() => Show(settings, true);
        public void OpenWarehouse() => Show(warehouse, true);
        public void OpenHousing() { RefreshForecasts(); Show(housing, true); }
        public void OpenRefugees() => Show(refugees, true);
        public void OpenSquare() => Show(square, true);
        public void OpenBlacksmith() => Show(blacksmith, true);
        public void CloseMap() => Show(globalMap, false);
        public void CloseLaboratory() => Show(laboratory, false);
        public void CloseSettings() => Show(settings, false);
        public void CloseWarehouse() => Show(warehouse, false);
        public void CloseHousing() => Show(housing, false);
        public void CloseRefugees() => Show(refugees, false);
        public void CloseSquare() => Show(square, false);
        public void CloseBlacksmith() => Show(blacksmith, false);
        public void NextDay() { if (DayCycleService.Instance == null) return; RefreshForecasts(); Show(nextDayConfirmation, true); }
        public void CancelNextDay() => Show(nextDayConfirmation, false);
        public void ConfirmNextDay()
        {
            if (DayCycleService.Instance == null) return;
            Show(nextDayConfirmation, false);
            DayCycleService.Instance.NextDay();
            dayReportPopup?.Open(DayResourceLedger.LastReport);
            Message("Наступил следующий день");
        }
        public void SearchPortals() { DayCycleService.Instance?.Search(); Refresh(); }
        public void BuyCart()
        {
            var purchase = warehouse != null ? warehouse.GetComponent<WarehouseCartPurchaseView>() : null;
            if (purchase == null || !purchase.TryBuy()) { Message("Недостаточно дерева для телеги"); return; }
            Message("Телега куплена");
        }
        public void AdmitRefugee() { if (GlobalResourceManager.Instance && human && DayCycleService.Instance?.AdmitRefugee() == true) { GlobalResourceManager.Instance.AddResource(human, 1); Message("Новый житель принят"); } }
    }
}
