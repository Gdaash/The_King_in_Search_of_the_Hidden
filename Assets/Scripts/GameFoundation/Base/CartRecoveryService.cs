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
                var quest = FirstExpeditionRecovery.Quest;
                if (QuestProgress.IsAccepted(quest) || view == null) return;
                var lines = SaveSlotPrefs.GetInt(FirstExpeditionRecovery.CartsKey, 0) > 0
                    ? new[] { FirstExpeditionRecovery.Dialogue, FirstExpeditionRecovery.CartDialogue } : new[] { FirstExpeditionRecovery.Dialogue };
                showing = true;
                if (!view.TryShowMessage("Стайк Пью", lines, () => showing = false, quest)) showing = false;
                return;
            }
            ResidentRecovery.Prepare(GlobalResourceManager.Instance);
            if (ResidentRecovery.Pending)
            {
                var quest = ResidentRecovery.Quest;
                if (QuestProgress.IsAccepted(quest) || view == null) return;
                showing = true;
                if (!view.TryShowMessage("Стайк Пью", new[] { ResidentRecovery.Dialogue }, () => showing = false, quest)) showing = false;
                return;
            }
            string notice = SaveSlotPrefs.GetString(RoyalTributeService.NoticeKey, "");
            if (scene == "Base" && !string.IsNullOrEmpty(notice) && view != null)
            {
                string[] parts = notice.Split(':'); int.TryParse(parts.Length > 1 ? parts[1] : "30", out int amount);
                string text = parts[0] == "paid" ? GameText.Format("Подать получена: {0} единиц магической руды. Следующая норма – тридцать единиц через тридцать дней. Не заставляйте корону напоминать дважды.", amount) :
                    GameText.Format("К сроку вы не собрали {0} единиц магической руды. Корона взыскивает оговорённый штраф. Новая норма – тридцать единиц через тридцать дней. Служба продолжается.", amount);
                showing = true;
                if (!view.TryShowMessage("Королевский писарь", new[]{text}, () => { SaveSlotPrefs.SetString(RoyalTributeService.NoticeKey, ""); SaveSlotPrefs.Save(); showing = false; })) showing = false;
                return;
            }
            if (!NeedsHelp(GlobalResourceManager.Instance, cart, wood, cartCost)) return;
            showing = true;
            bool opened = view != null && view.TryShowMessage("Стайк Пью", new[]{RescueText}, Grant);
            if (!opened) showing = false;
        }
        private void Grant()
        {
            try
            {
                if (SceneManager.GetActiveScene().name != "Base") return;
                var resources = GlobalResourceManager.Instance;
                if (!NeedsHelp(resources, cart, wood, cartCost)) return;
                using (SaveSlotPrefs.Batch())
                {
                    // Store the surcharge before resource callbacks can re-evaluate quests.
                    SaveSlotPrefs.SetInt(RoyalTributeService.CartDebtKey, RoyalTributeService.CartDebt + 2);
                    SaveSlotPrefs.SetInt(RescuesKey, SaveSlotPrefs.GetInt(RescuesKey, 0) + 1);
                    resources.AddResource(cart, 2); SaveSlotPrefs.Save();
                }
                QuestProgress.NotifyChanged();
            }
            finally { showing = false; }
        }
    }
}
