using System.Collections.Generic;
using GameFoundation.Quests;
using GameFoundation.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Base
{
    public static class ResidentRecovery
    {
        public const string CountKey = "foundation.royal.residentAidCount";
        public const string AmountKey = "foundation.royal.residentAidAmount";
        public const string Title = "Люди для земель портала";
        public const string Description = "Пью раздобыл новых жителей у королевского писаря. За каждого прибывшего к ближайшей подати добавится одна единица магической руды.";
        public const string Status = "Прими помощь Пью.";
        public const string Dialogue = "Рыцарь, нас осталось совсем мало. Кто же будет восстанавливать земли портала? Я уговорил писаря прислать людей, чтобы жителей снова стало четверо. Только за каждого прибывшего он добавит единицу магической руды к ближайшей подати. Принимай помощь и береги людей!";
        private static QuestDefinition cached;
        private static int cachedSlot;
        private static string Id => "resident_recovery_" + SaveSlotPrefs.GetInt(CountKey, 0);
        public static bool Pending => SaveSlotPrefs.GetInt(AmountKey, 0) > 0;
        public static bool IsRecovery(QuestDefinition quest) => Pending && quest != null && quest.id == Id;
        public static bool NeedsHelp(GlobalResourceManager resources) => resources != null &&
            !FirstExpeditionRecovery.Pending && !Pending && !BuildingUpgradeService.IsBuilt("refugees") &&
            resources.GetResourceAmount(Resources.Load<ResourceType>("ResourceTypes/Human")) < 2;
        public static void Prepare(GlobalResourceManager resources)
        {
            if (SceneManager.GetActiveScene().name != "Base" || !NeedsHelp(resources)) return;
            using (SaveSlotPrefs.Batch())
            {
                SaveSlotPrefs.SetInt(CountKey, SaveSlotPrefs.GetInt(CountKey, 0) + 1);
                SaveSlotPrefs.SetInt(AmountKey, 4 - resources.GetResourceAmount(Resources.Load<ResourceType>("ResourceTypes/Human")));
                SaveSlotPrefs.Save();
            }
            cached = null;
        }
        public static QuestDefinition Quest
        {
            get
            {
                if (!Pending) return null;
                if (cached != null && cached.id == Id && cachedSlot == SaveSlotPrefs.SelectedSlot) return cached;
                if (cached != null) Object.Destroy(cached);
                cachedSlot = SaveSlotPrefs.SelectedSlot;
                cached = ScriptableObject.CreateInstance<QuestDefinition>();
                cached.id = Id; cached.title = Title; cached.description = Description; cached.inProgressStatus = Status;
                cached.resourceRewards = new List<QuestDefinition.ResourceReward> { new() {
                    resource = Resources.Load<ResourceType>("ResourceTypes/Human"), amount = SaveSlotPrefs.GetInt(AmountKey, 0) } };
                return cached;
            }
        }
        // Called inside the same save batch as the reward, before resource callbacks.
        public static void RecordClaim(QuestDefinition quest)
        {
            if (!IsRecovery(quest)) return;
            SaveSlotPrefs.SetInt(RoyalTributeService.ResidentDebtKey, RoyalTributeService.ResidentDebt + SaveSlotPrefs.GetInt(AmountKey, 0));
            SaveSlotPrefs.SetInt(AmountKey, 0);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => cached = null;
    }
}
