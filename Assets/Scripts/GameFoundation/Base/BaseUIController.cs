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
        [SerializeField] private GameObject nextDayPanel;
        [SerializeField] private Button confirmDayButton;
        [SerializeField] private Button cancelDayButton;
        [SerializeField] private Button closeDayButton;
        [SerializeField] private Button warehouseButton;
        [SerializeField] private Button housingButton;
        [SerializeField] private Button refugeesButton;
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
        [SerializeField] private Button closeBlacksmithButton;
        [SerializeField] private Button closeLibraryButton;
        [SerializeField] private Text refugeesAvailableText;
        [SerializeField] private GameObject globalMap, laboratory, settings, warehouse, housing, refugees, blacksmith, magicLibrary;
        [SerializeField] private WarehouseCartPurchaseView cartWorkshop;
        [SerializeField] private GameObject nextDayConfirmation;
        [SerializeField] private FoodForecastView nextDayForecast, housingForecast;
        [SerializeField] private Button[] portalButtons;
        [SerializeField] private Text statusText, dayText;
        [SerializeField] private Text portalTravelNotice;
        [SerializeField] private Text portalTravelUsedNotice;
        [SerializeField] private ResourceType human;
        [SerializeField] private ResourceType magicOre;
        [SerializeField] private DayReportPopup dayReportPopup;
        private bool CanAdvanceDay => PlaytestSettings.TutorialsAndQuestsDisabled || BuildingUpgradeService.IsBuilt("housing");
        private bool NextDayReady => CanAdvanceDay &&
            (nextDayButton == null || !nextDayButton.TryGetComponent<NextDayReminder>(out var reminder) || !reminder.BlocksActivation);
        private EngineeringBureauRecruitment engineeringBureau;
        private PortalArsenalPopup baseArsenal;
        private bool daySubscribed;
        private bool languageSubscribed;
        private RectTransform waitingIcons;
        private readonly List<Image> waitingPeople = new();

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
            Bind(blacksmithButton, OpenBlacksmith);
            Bind(libraryButton, OpenMagicLibrary);
            if (settingsButton != null && settingsButton.GetComponent<GameFoundation.Settings.GameMenuLauncher>() == null)
                Bind(settingsButton, OpenSettings);
            Bind(searchPortalsButton, SearchPortals);
            Bind(closeMapButton, CloseMap);
            Bind(closeLaboratoryButton, CloseLaboratory);
            Bind(closeWarehouseButton, CloseWarehouse);
            Bind(buyCartButton, BuyCart);
            Bind(closeHousingButton, CloseHousing);
            Bind(closeRefugeesButton, CloseRefugees);
            Bind(admitRefugeeButton, AdmitRefugee);
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
            DayResourceLedger.EnterShelter();
            if (warehouse != null)
            {
                engineeringBureau = warehouse.AddComponent<EngineeringBureauRecruitment>();
                engineeringBureau.Initialize(buyCartButton, human);
            }
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
            bool canAdvanceDay = CanAdvanceDay;
            var dayBlock = nextDayPanel != null ? nextDayPanel : nextDayButton != null && nextDayButton.transform.parent.name == "Day Panel"
                ? nextDayButton.transform.parent.gameObject : nextDayButton != null ? nextDayButton.gameObject : null;
            if (dayBlock != null) dayBlock.SetActive(canAdvanceDay);
            if (nextDayButton != null) nextDayButton.interactable = NextDayReady;
            var day = DayCycleService.Instance;
            var oreAmount = GlobalResourceManager.Instance != null && magicOre != null
                ? GlobalResourceManager.Instance.GetResourceAmount(magicOre) : 0;
            if (portalTravelNotice != null)
                portalTravelNotice.text = Tr("base.portal.travel_once",
                    GameFoundation.Localization.GameText.Get("Путешествовать через портал можно только один раз в день."));
            if (portalTravelUsedNotice != null)
            {
                portalTravelUsedNotice.text = Tr("base.portal.travel_used",
                    GameFoundation.Localization.GameText.Get("На сегодня вы исчерпали эту возможность."));
                portalTravelUsedNotice.gameObject.SetActive(day != null && day.EnteredToday);
            }
            if (dayText) dayText.text = Tr("base.day", GameFoundation.Localization.GameText.Get("День")) + " " + (day?.Day ?? 1);
            var search = searchPortalsButton;
            if (search) search.gameObject.SetActive(false);
            var available = refugeesAvailableText;
            if (available) available.text = Tr("base.waiting", GameFoundation.Localization.GameText.Get("Ожидают:")) + " " + (day?.RefugeesAvailable ?? 0);
            RefreshRefugeePresentation(day?.RefugeesAvailable ?? 0);
            var admit = admitRefugeeButton;
            if (admit) admit.interactable = day != null && day.RefugeesAvailable > 0 && BuildingUpgradeService.CanAdmitResident;
            BaseDisabledButtonHint.Set(admit, day != null && day.RefugeesAvailable > 0 ? BaseDisabledButtonHint.Reason.Housing : BaseDisabledButtonHint.Reason.Refugees);
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
                        Tr("base.enter", GameFoundation.Localization.GameText.Get("Войти")), Tr("base.portal.free", GameFoundation.Localization.GameText.Get("Бесплатно")));
                button.onClick.RemoveAllListeners();
                var portal = site;
                button.onClick.AddListener(() => SelectPortal(portal));
            }
            RefreshForecasts();
        }

        private void RefreshRefugeePresentation(int count)
        {
            if (admitRefugeeButton == null || refugeesAvailableText == null || human == null) return;
            var label = admitRefugeeButton.GetComponentInChildren<Text>(true);
            float width = 360;
            if (label != null) { label.fontSize = 26; label.resizeTextForBestFit = false; width = Mathf.Max(width, label.preferredWidth + 48); }
            foreach (var text in admitRefugeeButton.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.fontSize = 26; text.enableAutoSizing = false;
                width = Mathf.Max(width, text.GetPreferredValues(text.text, Mathf.Infinity, Mathf.Infinity).x + 48);
            }
            var buttonRect = (RectTransform)admitRefugeeButton.transform;
            buttonRect.sizeDelta = new Vector2(Mathf.Min(720, width), 60);
            buttonRect.anchoredPosition = new Vector2(0, -95);
            refugeesAvailableText.rectTransform.anchoredPosition = new Vector2(0,-5);
            refugeesAvailableText.fontSize = 28;
            refugeesAvailableText.resizeTextForBestFit = false;
            var parent = refugeesAvailableText.transform.parent;
            var frame = parent.Find("Artwork Frame") as RectTransform;
            if (frame != null) frame.sizeDelta = new Vector2(800,340);
            var title = parent.Find("Title") as RectTransform;
            if (title != null) title.anchoredPosition = new Vector2(0,120);
            var close = parent.Find("Close") as RectTransform;
            if (close != null) close.anchoredPosition = new Vector2(388,158);
            if (waitingIcons == null)
            {
                waitingIcons = new GameObject("Waiting residents",typeof(RectTransform)).GetComponent<RectTransform>();
                waitingIcons.SetParent(parent,false); waitingIcons.anchorMin = waitingIcons.anchorMax = new Vector2(.5f,.5f);
                waitingIcons.anchoredPosition = new Vector2(0,55);
            }
            while (waitingPeople.Count < count)
            {
                var image = new GameObject("Waiting resident", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(waitingIcons,false); image.raycastTarget = false;
                ResourceIconSizing.Apply(image,human.resourceIcon); waitingPeople.Add(image);
            }
            float spacing = human.resourceIcon.rect.width * 2 + 12;
            for (int i = 0; i < waitingPeople.Count; i++)
            {
                waitingPeople[i].gameObject.SetActive(i < count);
                if (i < count) waitingPeople[i].rectTransform.anchoredPosition = new Vector2((i-(count-1)*.5f)*spacing,0);
            }
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
            if (!PortalProgression.IsUnlocked(DayCycleService.GetPortalLocation(site.locationId))) { Message(GameFoundation.Localization.GameText.Get("Улучшите портал для доступа к этой локации"), GameFoundation.UI.NotificationKind.Negative); return; }
            var router = FindAnyObjectByType<RunSceneRouter>();
            if (router == null || !router.CanEnterRun) return;
            if (!day.EnteredToday && !day.HasExpeditionResidents)
            {
                Message(PortalPopupView.ExpeditionResidentsRequiredText, GameFoundation.UI.NotificationKind.Negative);
                return;
            }
            // Capture the shelter before Enter marks this day as already used.
            if (!day.Enter(site)) { Message(GameFoundation.Localization.GameText.Get("Сегодня уже был поход"), GameFoundation.UI.NotificationKind.Negative); return; }
            router.EnterRun();
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
        private void Show(GameObject panel, bool visible)
        {
            if (panel == null) return;
            if (visible) panel.SetActive(true);
            else GameFoundation.UI.PopupMotion.Hide(panel);
        }
        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (!button) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
        public void OpenMap() { Refresh(); Show(globalMap, true); }
        public void OpenLaboratory() => Show(laboratory, true);
        public void OpenSettings() => Show(settings, true);
        public void OpenWarehouse()
        {
            Show(warehouse, true);
            engineeringBureau?.Refresh();
        }
        public void OpenHousing() { HousingDistrictView.Install(housing); Show(housing, true); }
        public void OpenRefugees() => Show(refugees, true);
        public void OpenBlacksmith() => Show(blacksmith, true);
        public void OpenMagicLibrary()
        {
            if (!PortalArsenalPopup.IsOpen && !TryOpenArsenal()) Show(magicLibrary, true);
        }
        internal void OpenArsenalFromPortal(PortalPopupView portal)
        {
            if (portal == null || PortalArsenalPopup.IsOpen) return;
            GameFoundation.UI.PopupMotion.Hide(portal.gameObject, () =>
            {
                if (portal == null) return;
                void ReturnToPortal() { if (portal != null) portal.gameObject.SetActive(true); }
                if (!TryOpenArsenal(ReturnToPortal)) ReturnToPortal();
            });
        }
        private bool TryOpenArsenal(System.Action onClose = null)
        {
            var balance = Resources.Load<PortalTowerBalance>("World/PortalTowerBalance");
            if (balance != null && balance.useSelectableArsenal && balance.useEnergyLoadout && balance.arsenalPopupPrefab != null)
            {
                if (baseArsenal == null) baseArsenal = Instantiate(balance.arsenalPopupPrefab).GetComponent<PortalArsenalPopup>();
                return baseArsenal.OpenBase(balance, onClose);
            }
            return false;
        }
        public void CloseMap() => Show(globalMap, false);
        public void CloseLaboratory() => Show(laboratory, false);
        public void CloseSettings() => Show(settings, false);
        public void CloseWarehouse() => Show(warehouse, false);
        public void CloseHousing() => Show(housing, false);
        public void CloseRefugees() => Show(refugees, false);
        public void CloseBlacksmith() => Show(blacksmith, false);
        public void CloseMagicLibrary() { baseArsenal?.Close(); Show(magicLibrary, false); }
        private void OnDestroy() { if (baseArsenal != null) Destroy(baseArsenal.gameObject); }
        public void NextDay() { if (PortalCloudTransition.IsLoading || !NextDayReady || DayCycleService.Instance == null) return; RefreshForecasts(); Show(nextDayConfirmation, true); }
        public void CancelNextDay() { if (!ForestForagingService.IsPending) Show(nextDayConfirmation, false); }
        public void ConfirmNextDay()
        {
            if (PortalCloudTransition.IsLoading || !NextDayReady || ForestForagingService.IsPending || DayCycleService.Instance == null) return;
            if (PortalCloudTransition.Cover(AdvanceDayUnderClouds)) Show(nextDayConfirmation, false);
        }
        private void AdvanceDayUnderClouds()
        {
            if (this == null || DayCycleService.Instance == null || ForestForagingService.IsPending) return;
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            dayReportPopup?.PrepareForNextDay();
            DayCycleService.Instance.NextDay();
            dayReportPopup?.Open(DayResourceLedger.LastReport);
            Message(GameFoundation.Localization.GameText.Get("Наступил следующий день"));
        }
        public void SearchPortals() { DayCycleService.Instance?.Search(); Refresh(); }
        public void BuyCart()
        {
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            var purchase = cartWorkshop;
            if (purchase == null || !purchase.TryBuy()) { Message(GameFoundation.Localization.GameText.Get("Недостаточно дерева для телеги"), GameFoundation.UI.NotificationKind.Negative); return; }
        }
        public void AdmitRefugee() { using var notification = GameFoundation.UI.GameNotifications.BeginAction(); if (GlobalResourceManager.Instance && human && DayCycleService.Instance?.AdmitRefugee() == true) { GlobalResourceManager.Instance.AddResource(human, 1); Message(GameFoundation.Localization.GameText.Get("Новый житель принят"), GameFoundation.UI.NotificationKind.Positive); GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.RefugeeAdmit, .65f); } }
    }
}
