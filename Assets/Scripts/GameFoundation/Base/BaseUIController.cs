using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BaseUIController : MonoBehaviour
    {
        [SerializeField] private Transform worldBuildingButtons;
        [SerializeField] private Button portalButton;
        [SerializeField] private Button laboratoryButton;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Button confirmDayButton;
        [SerializeField] private Button cancelDayButton;
        [SerializeField] private Button closeDayButton;
        [SerializeField] private Button warehouseButton;
        [SerializeField] private Button housingButton;
        [SerializeField] private Button refugeesButton;
        [SerializeField] private Button squareButton;
        [SerializeField] private Button blacksmithButton;
        [SerializeField] private Button libraryButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button searchPortalsButton;
        [SerializeField] private Button closeMapButton;
        [SerializeField] private Button closeLaboratoryButton;
        [SerializeField] private Button closeWarehouseButton;
        [SerializeField] private Button buyCartButton;
        [SerializeField] private Button closeHousingButton;
        [SerializeField] private Button closeRefugeesButton;
        [SerializeField] private Button admitRefugeeButton;
        [SerializeField] private Button closeSquareButton;
        [SerializeField] private Button closeBlacksmithButton;
        [SerializeField] private Button closeLibraryButton;
        [SerializeField] private Text refugeesAvailableText;
        [SerializeField] private GameObject globalMap, laboratory, settings, warehouse, housing, refugees, square, blacksmith, magicLibrary;
        [SerializeField] private GameObject nextDayConfirmation;
        [SerializeField] private FoodForecastView nextDayForecast, housingForecast;
        [SerializeField] private Button[] portalButtons;
        [SerializeField] private Text statusText, dayText;
        [SerializeField] private Text portalTravelNotice;
        [SerializeField] private Text portalTravelUsedNotice;
        [SerializeField] private ResourceType human;
        [SerializeField] private ResourceType magicOre;
        [SerializeField] private DayReportPopup dayReportPopup;
        private bool daySubscribed;
        private bool languageSubscribed;

        private void Awake()
        {
            if (statusText != null) statusText.gameObject.SetActive(false);
            Bind(portalButton, OpenMap);
            Bind(laboratoryButton, OpenLaboratory);
            Bind(nextDayButton, NextDay);
            Bind(confirmDayButton, ConfirmNextDay);
            Bind(cancelDayButton, CancelNextDay);
            Bind(closeDayButton, CancelNextDay);
            Bind(warehouseButton, OpenWarehouse);
            Bind(housingButton, OpenHousing);
            Bind(refugeesButton, OpenRefugees);
            Bind(squareButton, OpenSquare);
            Bind(blacksmithButton, OpenBlacksmith);
            Bind(libraryButton, OpenMagicLibrary);
            Bind(settingsButton, OpenSettings);
            Bind(searchPortalsButton, SearchPortals);
            Bind(closeMapButton, CloseMap);
            Bind(closeLaboratoryButton, CloseLaboratory);
            Bind(closeWarehouseButton, CloseWarehouse);
            Bind(buyCartButton, BuyCart);
            Bind(closeHousingButton, CloseHousing);
            Bind(closeRefugeesButton, CloseRefugees);
            Bind(admitRefugeeButton, AdmitRefugee);
            Bind(closeSquareButton, CloseSquare);
            Bind(closeBlacksmithButton, CloseBlacksmith);
            // The library close icon is part of its window, unlike several older popups.
            Bind(closeLibraryButton, CloseMagicLibrary);
        }
        private void OnEnable()
        {
            SubscribeDay();
            SubscribeLanguage();
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            BuildingUpgradeService.Changed += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (daySubscribed && DayCycleService.Instance != null) DayCycleService.Instance.Changed -= Refresh;
            daySubscribed = false;
            if (languageSubscribed && LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
            languageSubscribed = false;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            BuildingUpgradeService.Changed -= Refresh;
        }
        private void OnResourceChanged(ResourceType type, int amount)
        {
            RefreshForecasts();
            if (type == magicOre || type == human) Refresh();
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
            var search = searchPortalsButton;
            if (search) search.gameObject.SetActive(false);
            var available = refugeesAvailableText;
            if (available) available.text = Tr("base.waiting", "Ожидают:") + " " + (day?.RefugeesAvailable ?? 0);
            var admit = admitRefugeeButton;
            if (admit) admit.interactable = day != null && day.RefugeesAvailable > 0 && BuildingUpgradeService.CanAdmitResident;
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
            if (!PortalProgression.IsUnlocked(DayCycleService.GetPortalLocation(site.locationId))) { Message("Улучшите портал для доступа к этой локации", GameFoundation.UI.NotificationKind.Negative); return; }
            if (!day.Enter(site)) { Message("Сегодня уже был поход", GameFoundation.UI.NotificationKind.Negative); return; }
            FindFirstObjectByType<RunSceneRouter>()?.EnterRun();
        }
        private void Message(string value, GameFoundation.UI.NotificationKind kind = GameFoundation.UI.NotificationKind.Normal)
        {
            GameFoundation.UI.GameNotifications.Post(value, kind);
        }
        private static string Tr(string key, string fallback)
        {
            var value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
        private void Show(GameObject panel, bool visible) { if (panel) panel.SetActive(visible); }
        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
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
        public void OpenMagicLibrary() => Show(magicLibrary, true);
        public void CloseMap() => Show(globalMap, false);
        public void CloseLaboratory() => Show(laboratory, false);
        public void CloseSettings() => Show(settings, false);
        public void CloseWarehouse() => Show(warehouse, false);
        public void CloseHousing() => Show(housing, false);
        public void CloseRefugees() => Show(refugees, false);
        public void CloseSquare() => Show(square, false);
        public void CloseBlacksmith() => Show(blacksmith, false);
        public void CloseMagicLibrary() => Show(magicLibrary, false);
        public void NextDay() { if (DayCycleService.Instance == null) return; RefreshForecasts(); Show(nextDayConfirmation, true); }
        public void CancelNextDay() => Show(nextDayConfirmation, false);
        public void ConfirmNextDay()
        {
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            if (DayCycleService.Instance == null) return;
            Show(nextDayConfirmation, false);
            DayCycleService.Instance.NextDay();
            dayReportPopup?.Open(DayResourceLedger.LastReport);
            Message("Наступил следующий день");
        }
        public void SearchPortals() { DayCycleService.Instance?.Search(); Refresh(); }
        public void BuyCart()
        {
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            var purchase = warehouse != null ? warehouse.GetComponent<WarehouseCartPurchaseView>() : null;
            if (purchase == null || !purchase.TryBuy()) { Message("Недостаточно дерева для телеги", GameFoundation.UI.NotificationKind.Negative); return; }
            Message("Телега куплена", GameFoundation.UI.NotificationKind.Positive);
        }
        public void AdmitRefugee() { using var notification = GameFoundation.UI.GameNotifications.BeginAction(); if (GlobalResourceManager.Instance && human && DayCycleService.Instance?.AdmitRefugee() == true) { GlobalResourceManager.Instance.AddResource(human, 1); Message("Новый житель принят", GameFoundation.UI.NotificationKind.Positive); } }
    }
}
