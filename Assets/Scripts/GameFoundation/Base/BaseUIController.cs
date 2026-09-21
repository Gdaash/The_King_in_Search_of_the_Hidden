using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BaseUIController : MonoBehaviour
    {
        [SerializeField] private GameObject globalMap, laboratory, settings, warehouse, housing, refugees, square;
        [SerializeField] private Button[] portalButtons;
        [SerializeField] private Text statusText, dayText;
        [SerializeField] private ResourceType wood, cart, human;
        [SerializeField] private DayReportPopup dayReportPopup;
        private bool daySubscribed;
        private bool languageSubscribed;

        private void Awake()
        {
            Bind("Base Panel/Portal", OpenMap);
            Bind("Base Panel/Laboratory", OpenLaboratory);
            Bind("Base Panel/Next Day", NextDay);
            Bind("Base Panel/Warehouse", OpenWarehouse);
            Bind("Base Panel/Housing", OpenHousing);
            Bind("Base Panel/Refugees", OpenRefugees);
            Bind("Base Panel/Square", OpenSquare);
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
        }
        private void OnEnable()
        {
            SubscribeDay();
            SubscribeLanguage();
            Refresh();
        }
        private void OnDisable()
        {
            if (daySubscribed && DayCycleService.Instance != null) DayCycleService.Instance.Changed -= Refresh;
            daySubscribed = false;
            if (languageSubscribed && LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
            languageSubscribed = false;
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
            if (dayText) dayText.text = Tr("base.day", "День") + " " + (day?.Day ?? 1);
            var search = transform.Find("Global Map/Search Portals")?.GetComponent<Button>();
            if (search) search.interactable = day != null && !day.SearchedToday && day.Portals.Count < DayCycleService.MaxPortals;
            var available = transform.Find("Refugees Popup/Available")?.GetComponent<Text>();
            if (available) available.text = Tr("base.waiting", "Ожидают:") + " " + (day?.RefugeesAvailable ?? 0);
            var admit = transform.Find("Refugees Popup/Admit")?.GetComponent<Button>();
            if (admit) admit.interactable = day != null && day.RefugeesAvailable > 0;
            for (var i = 0; portalButtons != null && i < portalButtons.Length; i++)
            {
                var button = portalButtons[i];
                if (!button) continue;
                var visible = day != null && i < day.Portals.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                var site = day.Portals[i];
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(site.x, site.y);
                rect.anchoredPosition = Vector2.zero;
                var label = button.GetComponentInChildren<Text>();
                if (label) label.text = site.active ? Tr("base.enter", "Войти") + " · " + site.difficulty : Tr("base.level", "Ур.") + " " + site.difficulty + " · " + (site.difficulty * 5) + " " + Tr("base.ore", "руды");
                button.onClick.RemoveAllListeners();
                var portal = site;
                button.onClick.AddListener(() => SelectPortal(portal));
            }
        }
        private void SelectPortal(PortalSite site)
        {
            var day = DayCycleService.Instance;
            if (day == null) return;
            if (!site.active) { Message(day.Activate(site) ? "Портал активирован" : "Недостаточно магической руды"); return; }
            if (!day.Enter(site)) { Message("Сегодня уже был поход"); return; }
            FindFirstObjectByType<RunSceneRouter>()?.EnterRun();
        }
        private void Message(string value) { if (statusText) statusText.text = value; }
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
        public void OpenMap() => Show(globalMap, true);
        public void OpenLaboratory() => Show(laboratory, true);
        public void OpenSettings() => Show(settings, true);
        public void OpenWarehouse() => Show(warehouse, true);
        public void OpenHousing() => Show(housing, true);
        public void OpenRefugees() => Show(refugees, true);
        public void OpenSquare() => Show(square, true);
        public void CloseMap() => Show(globalMap, false);
        public void CloseLaboratory() => Show(laboratory, false);
        public void CloseSettings() => Show(settings, false);
        public void CloseWarehouse() => Show(warehouse, false);
        public void CloseHousing() => Show(housing, false);
        public void CloseRefugees() => Show(refugees, false);
        public void CloseSquare() => Show(square, false);
        public void NextDay() { if (DayCycleService.Instance == null) return; DayCycleService.Instance.NextDay(); dayReportPopup?.Open(DayResourceLedger.LastReport); Message("Наступил следующий день"); }
        public void SearchPortals() { DayCycleService.Instance?.Search(); Refresh(); }
        public void BuyCart()
        {
            var resources = GlobalResourceManager.Instance;
            if (!resources || !wood || !cart || !resources.TrySpendResource(wood, 10)) { Message("Для телеги нужно 10 дерева"); return; }
            resources.AddResource(cart, 1); Message("Телега куплена");
        }
        public void AdmitRefugee() { if (GlobalResourceManager.Instance && human && DayCycleService.Instance?.AdmitRefugee() == true) { GlobalResourceManager.Instance.AddResource(human, 1); Message("Новый житель принят"); } }
    }
}
