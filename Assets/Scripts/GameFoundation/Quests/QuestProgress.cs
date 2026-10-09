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

        public static bool IsClaimed(QuestDefinition quest) => quest != null && !string.IsNullOrWhiteSpace(quest.id) &&
            SaveSlotPrefs.GetInt("foundation.quest." + quest.id + ".claimed", 0) == 1;

        public static bool TryClaim(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (!IsComplete(quest) || IsClaimed(quest) || resources == null) return false;
            var rewards = new System.Collections.Generic.Dictionary<ResourceType, long>();
            foreach (var reward in quest.resourceRewards)
            {
                if (reward == null || reward.resource == null || reward.amount <= 0) return false;
                rewards.TryGetValue(reward.resource, out long total);
                rewards[reward.resource] = total + reward.amount;
            }
            foreach (var reward in rewards)
                if (reward.Value + resources.GetResourceAmount(reward.Key) > int.MaxValue) return false;
            foreach (var unlock in quest.unlockRewards)
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.id)) return false;
            using (SaveSlotPrefs.Batch())
            using (GameFoundation.UI.GameNotifications.BeginAction())
            {
                // Set before resource events to reject synchronous duplicate claims.
                SaveSlotPrefs.SetInt("foundation.quest." + quest.id + ".claimed", 1);
                foreach (var reward in rewards) resources.AddResource(reward.Key, (int)reward.Value);
                foreach (var unlock in quest.unlockRewards) ContentUnlocks.Grant(unlock);
                SaveSlotPrefs.Save();
                GameFoundation.UI.GameNotifications.Post("Награда получена: " + quest.title, GameFoundation.UI.NotificationKind.Positive);
            }
            ContentUnlocks.NotifyChanged();
            Changed?.Invoke();
            return true;
        }

        public static bool TryComplete(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (quest == null || resources == null || string.IsNullOrWhiteSpace(quest.id) ||
                IsComplete(quest) || !quest.HasRequiredStock(resources.GetResourceAmount)) return false;
            SaveSlotPrefs.SetInt(Key(quest), 1);
            if (quest.unlockRewardsOnCompletion)
                foreach (var unlock in quest.unlockRewards)
                    if (unlock != null && !string.IsNullOrWhiteSpace(unlock.id)) ContentUnlocks.Grant(unlock);
            SaveSlotPrefs.Save();
            if (quest.unlockRewardsOnCompletion) ContentUnlocks.NotifyChanged();
            Changed?.Invoke();
            return true;
        }

        public static QuestDefinition Current(QuestCatalog catalog)
        {
            if (catalog == null || catalog.quests == null) return null;
            foreach (var quest in catalog.quests)
            {
                if (quest == null) continue;
                if (!IsClaimed(quest)) return quest;
            }
            return null;
        }
    }
}
