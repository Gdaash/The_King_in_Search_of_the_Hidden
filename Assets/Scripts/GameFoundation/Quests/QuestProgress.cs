using GameFoundation.Saves;

namespace GameFoundation.Quests
{
    public static class QuestProgress
    {
        public static event System.Action Changed;
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Changed = null;
        private static string Key(QuestDefinition quest) => "foundation.quest." + quest.id + ".completed";
        public static bool IsComplete(QuestDefinition quest) => quest != null &&
            !string.IsNullOrWhiteSpace(quest.id) && SaveSlotPrefs.GetInt(Key(quest), 0) != 0;

        public static bool TryComplete(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (quest == null || resources == null || string.IsNullOrWhiteSpace(quest.id) ||
                IsComplete(quest) || !quest.HasRequiredStock(resources.GetResourceAmount)) return false;
            SaveSlotPrefs.SetInt(Key(quest), 1);
            SaveSlotPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static QuestDefinition Current(QuestCatalog catalog)
        {
            if (catalog == null || catalog.quests == null) return null;
            QuestDefinition last = null;
            foreach (var quest in catalog.quests)
            {
                if (quest == null) continue;
                last = quest;
                if (!IsComplete(quest)) return quest;
            }
            return last;
        }
    }
}
