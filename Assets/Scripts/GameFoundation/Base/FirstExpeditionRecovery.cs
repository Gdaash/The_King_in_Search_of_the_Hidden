using System.Collections.Generic;
using GameFoundation.Quests;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>One saved, claimable royal replacement grant after losses in the first expedition.</summary>
    public static class FirstExpeditionRecovery
    {
        public const string QuestId = "first_expedition_replacements";
        public const string RecordKey = "foundation.firstExpeditionRecovery.recorded";
        public const string HumansKey = "foundation.firstExpeditionRecovery.humans";
        public const string CartsKey = "foundation.firstExpeditionRecovery.carts";
        public const string Title = "Помощь короны";
        public const string Description = "Король прислал людей взамен погибших в первой вылазке. Получи помощь Пью и продолжай восстановление земель портала.";
        public const string Status = "Забери королевскую помощь.";
        public const string Dialogue = "Жаль, рыцарь, ты не уберёг своих людей. На этот раз король прислал нам замену погибшим. Забери помощь – и береги тех, кто пойдёт за тобой дальше. Второго такого подарка не будет.";
        public const string CartDialogue = "И телеги для тебя нашлись. Эти корона отдаёт без долга, но впредь за потерянные придётся отвечать.";
        private static QuestDefinition cached;
        private static int cachedSlot;
        public static bool Pending => SaveSlotPrefs.GetInt(HumansKey, 0) > 0 &&
            SaveSlotPrefs.GetInt("foundation.quest." + QuestId + ".claimed", 0) == 0;
        public static bool IsRecovery(QuestDefinition quest) => quest != null && quest.id == QuestId;
        public static void Record(bool firstRun, int deadHumans, int lostCarts)
        {
            if (!firstRun || SaveSlotPrefs.GetInt(RecordKey, 0) != 0) return;
            using (SaveSlotPrefs.Batch())
            {
                SaveSlotPrefs.SetInt(RecordKey, 1);
                if (deadHumans > 0)
                {
                    SaveSlotPrefs.SetInt(HumansKey, deadHumans);
                    SaveSlotPrefs.SetInt(CartsKey, Mathf.Max(0, lostCarts));
                }
                SaveSlotPrefs.Save();
            }
        }
        public static QuestDefinition Quest
        {
            get
            {
                if (!Pending) return null;
                if (cached != null && cachedSlot == SaveSlotPrefs.SelectedSlot) return cached;
                if (cached != null) Object.Destroy(cached);
                cachedSlot = SaveSlotPrefs.SelectedSlot;
                cached = ScriptableObject.CreateInstance<QuestDefinition>();
                cached.id = QuestId; cached.title = Title; cached.description = Description; cached.inProgressStatus = Status;
                cached.resourceRewards = new List<QuestDefinition.ResourceReward>
                {
                    new() { resource = Resources.Load<ResourceType>("ResourceTypes/Human"), amount = SaveSlotPrefs.GetInt(HumansKey, 0) }
                };
                int carts = SaveSlotPrefs.GetInt(CartsKey, 0);
                if (carts > 0) cached.resourceRewards.Add(new QuestDefinition.ResourceReward
                { resource = Resources.Load<ResourceType>("ResourceTypes/Cart"), amount = carts });
                return cached;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { cached = null; cachedSlot = 0; }
    }
}
