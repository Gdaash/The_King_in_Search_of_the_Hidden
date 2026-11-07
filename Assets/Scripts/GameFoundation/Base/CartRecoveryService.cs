using System.Linq;
using GameFoundation.Quests;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Base
{
    /// <summary>Rescues a save only when no cart remains, including deployed carriers.</summary>
    public sealed class CartRecoveryService : MonoBehaviour
    {
        private const string PendingCartsKey = "foundation.royal.pendingCartAid";
        public const string RescuesKey = "foundation.royal.cartRescues";
        public const string RescueText = "Рыцарь, да как же ты умудрился остаться без последней телеги? Даже колёса не сберёг! Ладно, не вешай нос. Я раздобыл у королевского писаря парочку телег. Только он и доброту в ведомость заносит: за каждую телегу придётся отдать ещё единицу магической руды к ближайшей королевской подати.";
        private ResourceType cart, wood;
        private int cartCost;
        private float nextCheck;
        private bool showing;
        private void OnEnable() => SceneManager.sceneLoaded += SceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= SceneLoaded;
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { showing = false; nextCheck = 0; }
        private void Start()
        {
            cart = Resources.Load<ResourceType>("ResourceTypes/Cart"); wood = Resources.Load<ResourceType>("ResourceTypes/Wood");
            cartCost = WarehouseCartPurchaseView.DefaultWoodCost;
            var workshop = Object.FindAnyObjectByType<WarehouseCartPurchaseView>(FindObjectsInactive.Include);
            if (workshop != null) cartCost = workshop.WoodCost;
        }
        public static bool HasLivingCarrier() => Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)
            .Any(p => !p.TryGetComponent<Health>(out var health) || health.CurrentHealth > 0);
        public static bool NeedsHelp(GlobalResourceManager resources, ResourceType cart, ResourceType wood, int price) =>
            !FirstExpeditionRecovery.Pending && resources != null && cart != null && wood != null && resources.GetResourceAmount(cart) == 0 &&
            (!BuildingUpgradeService.IsBuilt("blacksmith") || resources.GetResourceAmount(wood) < price) && !HasLivingCarrier();
        private void Update()
        {
            if (showing || Time.unscaledTime < nextCheck || PlaytestSettings.TutorialsAndQuestsDisabled) return;
            nextCheck = Time.unscaledTime + .5f;
            if (DayReportPopup.BlocksQuestDialogues || FirstRunLightTutorial.BlocksInput || PortalTowerProgression.IsChoosingUpgrade || PortalCloudTransition.IsLoading) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene != "Base") return;
            var view = Object.FindAnyObjectByType<QuestDialogueView>(FindObjectsInactive.Include);
            if (FirstExpeditionRecovery.Pending)
            {
                if (SaveSlotPrefs.GetInt(FirstExpeditionRecovery.FlightStartedKey, 0) != 0 ||
                    SaveSlotPrefs.GetInt("foundation.quest." + FirstExpeditionRecovery.QuestId + ".accepted", 0) != 0)
                { BeginFirstExpeditionAid(); return; }
                if (view == null) return;
                var lines = SaveSlotPrefs.GetInt(FirstExpeditionRecovery.CartsKey, 0) > 0
                    ? new[] { FirstExpeditionRecovery.Dialogue, FirstExpeditionRecovery.CartDialogue } : new[] { FirstExpeditionRecovery.Dialogue };
                showing = true;
                if (!view.TryShowMessage("Стайк Пью", lines, BeginFirstExpeditionAid)) showing = false;
                return;
            }
            if (SaveSlotPrefs.GetInt(PendingCartsKey, 0) > 0) { BeginAidFlight(false); return; }
            ResidentRecovery.Prepare(GlobalResourceManager.Instance);
            if (ResidentRecovery.Pending)
            {
                if (view == null) return;
                showing = true;
                if (!view.TryShowMessage("Стайк Пью", new[] { ResidentRecovery.Dialogue }, () => BeginAidFlight(true))) showing = false;
                return;
            }
            string notice = SaveSlotPrefs.GetString(RoyalTributeService.NoticeKey, "");
            if (scene == "Base" && !string.IsNullOrEmpty(notice) && view != null)
            {
                string[] parts = notice.Split(':'); int.TryParse(parts.Length > 1 ? parts[1] : "30", out int amount);
                string text = parts[0] == "paid" ? GameText.Format("Подать получена: {0} единиц магической руды. Следующая норма – тридцать единиц через тридцать дней. Не заставляйте корону напоминать дважды.", amount) :
                    GameText.Format("К сроку вы не собрали {0} единиц магической руды. Корона взыскивает оговорённый штраф. Новая норма – тридцать единиц через тридцать дней. Служба продолжается.", amount);
                showing = true;
                if (!view.TryShowMessage("Королевский писарь", new[]{text}, () => ShowRoyalOutcome(view, parts[0] == "paid"))) showing = false;
                return;
            }
            if (!NeedsHelp(GlobalResourceManager.Instance, cart, wood, cartCost)) return;
            showing = true;
            bool opened = view != null && view.TryShowMessage("Стайк Пью", new[]{RescueText}, Grant);
            if (!opened) showing = false;
        }
        private void ShowRoyalOutcome(QuestDialogueView view, bool paid)
        {
            QuestRewardFlight.PlayChanges(AidCanvas(),RoyalTributeService.NoticeChanges(),()=>StartCoroutine(RoyalOutcome(view,paid)));
        }
        private System.Collections.IEnumerator RoyalOutcome(QuestDialogueView view, bool paid)
        {
            string key = paid ? "royal.pew.paid" : "royal.pew.failed";
            string text = LocalizationService.Instance.Get(key);
            while (view != null && !view.TryShowMessage("Стайк Пью",new[]{text},()=>
            {
                SaveSlotPrefs.SetString(RoyalTributeService.NoticeKey,"");
                SaveSlotPrefs.SetString(RoyalTributeService.ChangesKey,""); SaveSlotPrefs.Save(); showing=false;
            })) yield return null;
        }
        private void Grant()
        {
            try
            {
                if (SceneManager.GetActiveScene().name != "Base") return;
                var resources = GlobalResourceManager.Instance;
                if (!NeedsHelp(resources, cart, wood, cartCost)) return;
                SaveSlotPrefs.SetInt(PendingCartsKey, 2);
                SaveSlotPrefs.SetInt(RescuesKey, SaveSlotPrefs.GetInt(RescuesKey, 0) + 1);
                SaveSlotPrefs.Save();
                BeginAidFlight(false);
                return;
            }
            finally { if (SaveSlotPrefs.GetInt(PendingCartsKey, 0) == 0) showing = false; }

        }
        private void BeginFirstExpeditionAid()
        {
            if (!FirstExpeditionRecovery.Pending || SceneManager.GetActiveScene().name != "Base") { showing = false; return; }
            showing = true;
            SaveSlotPrefs.SetInt(FirstExpeditionRecovery.FlightStartedKey, 1); SaveSlotPrefs.Save();
            var canvas = AidCanvas();
            int people = SaveSlotPrefs.GetInt(FirstExpeditionRecovery.HumansKey, 0);
            int carts = SaveSlotPrefs.GetInt(FirstExpeditionRecovery.CartsKey, 0);
            QuestProgress.NotifyChanged();
            void Fly(ResourceType resource, string key, int count, Vector2 offset)
            {
                QuestRewardFlight.PlayGrant(canvas, resource, count, () =>
                {
                    if (this == null || SceneManager.GetActiveScene().name != "Base" || !FirstExpeditionRecovery.Pending) return;
                    int remaining = SaveSlotPrefs.GetInt(key, 0); if (remaining <= 0) return;
                    using (SaveSlotPrefs.Batch())
                    {
                        SaveSlotPrefs.SetInt(key, remaining - 1);
                        bool complete = SaveSlotPrefs.GetInt(FirstExpeditionRecovery.HumansKey, 0) == 0 &&
                            SaveSlotPrefs.GetInt(FirstExpeditionRecovery.CartsKey, 0) == 0;
                        if (complete) SaveSlotPrefs.SetInt("foundation.quest." + FirstExpeditionRecovery.QuestId + ".claimed", 1);
                        GlobalResourceManager.Instance.AddResource(resource, 1); SaveSlotPrefs.Save();
                    }
                    if (!FirstExpeditionRecovery.Pending) { showing = false; QuestProgress.NotifyChanged(); }
                }, offset);
            }
            bool twoRows = people > 0 && carts > 0;
            Fly(Resources.Load<ResourceType>("ResourceTypes/Human"), FirstExpeditionRecovery.HumansKey, people, new Vector2(0, twoRows ? 40 : 0));
            Fly(cart, FirstExpeditionRecovery.CartsKey, carts, new Vector2(0, twoRows ? -40 : 0));
        }
        private Canvas AidCanvas()
        {
            var canvas = Object.FindAnyObjectByType<QuestDialogueView>(FindObjectsInactive.Include)?.GetComponentInParent<Canvas>();
            if (canvas == null)
                foreach (var ui in Object.FindObjectsByType<ResourceUI>())
                    if (ui.DisplaysGlobalResources) { canvas = ui.GetComponentInParent<Canvas>(); break; }
            return canvas;
        }
        private void BeginAidFlight(bool residents)
        {
            showing = true;
            string pendingKey = residents ? ResidentRecovery.AmountKey : PendingCartsKey;
            var resource = residents ? Resources.Load<ResourceType>("ResourceTypes/Human") : cart;
            var canvas = AidCanvas();
            QuestRewardFlight.PlayGrant(canvas, resource, SaveSlotPrefs.GetInt(pendingKey, 0), () =>
            {
                if (this == null || SceneManager.GetActiveScene().name != "Base") return;
                int remaining = SaveSlotPrefs.GetInt(pendingKey, 0);
                if (remaining <= 0) return;
                using (SaveSlotPrefs.Batch())
                {
                    SaveSlotPrefs.SetInt(pendingKey, remaining - 1);
                    string debtKey = residents ? RoyalTributeService.ResidentDebtKey : RoyalTributeService.CartDebtKey;
                    SaveSlotPrefs.SetInt(debtKey, SaveSlotPrefs.GetInt(debtKey, 0) + 1);
                    GlobalResourceManager.Instance.AddResource(resource, 1);
                    SaveSlotPrefs.Save();
                }
                if (remaining == 1) { showing = false; QuestProgress.NotifyChanged(); }
            });
        }
    }
}
