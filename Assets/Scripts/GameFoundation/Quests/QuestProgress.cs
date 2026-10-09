using GameFoundation.Saves;

namespace GameFoundation.Quests
{
    public static class QuestProgress
    {
        public static event System.Action Changed;
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Changed = null;
        private static string Key(QuestDefinition quest) => "foundation.quest." + quest.id + ".completed";
        private static int Today => GameFoundation.MetaProgression.DayCycleService.Instance != null ? GameFoundation.MetaProgression.DayCycleService.Instance.Day : 1;
        public static bool IsFailed(QuestDefinition quest) => quest != null && SaveSlotPrefs.GetInt("foundation.quest." + quest.id + ".failed", 0) == 1;
        public static int DeadlineDay(QuestDefinition quest) => SaveSlotPrefs.GetInt("foundation.quest." + quest.id + ".deadline", 0);
        public static int DaysRemaining(QuestDefinition quest) => UnityEngine.Mathf.Max(0, DeadlineDay(quest) - Today);
        public static bool IsReadyToClaim(QuestDefinition quest, GlobalResourceManager resources) => quest != null && resources != null &&
            IsAccepted(quest) && !IsClaimed(quest) && !IsFailed(quest) &&
            (quest.deadlineDays == 0 || DaysRemaining(quest) > 0) &&
            (quest.consumeRequirementsOnClaim ? quest.HasRequiredStock(resources.GetResourceAmount) : IsComplete(quest));
        public static bool IsAccepted(QuestDefinition quest) => quest != null &&
            (IsComplete(quest) || IsClaimed(quest) || SaveSlotPrefs.GetInt("foundation.quest." + quest.id + ".accepted", 0) == 1);
        public static bool Accept(QuestDefinition quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.id) || IsAccepted(quest)) return false;
            SaveSlotPrefs.SetInt("foundation.quest." + quest.id + ".accepted", 1);
            if (quest.deadlineDays > 0) SaveSlotPrefs.SetInt("foundation.quest." + quest.id + ".deadline", Today + quest.deadlineDays);
            SaveSlotPrefs.Save();
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.QuestAccept, .8f);
            Changed?.Invoke();
            return true;
        }
        public static bool IsComplete(QuestDefinition quest) => quest != null &&
            !string.IsNullOrWhiteSpace(quest.id) && SaveSlotPrefs.GetInt(Key(quest), 0) != 0;

        public static bool IsClaimed(QuestDefinition quest) => quest != null && !string.IsNullOrWhiteSpace(quest.id) &&
            SaveSlotPrefs.GetInt("foundation.quest." + quest.id + ".claimed", 0) == 1;

        public static bool TryClaim(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (!IsReadyToClaim(quest, resources)) return false;
            var costs = new System.Collections.Generic.Dictionary<ResourceType, long>();
            if (quest.consumeRequirementsOnClaim)
                foreach (var goal in quest.requirements)
                {
                    if (goal.resource == null || !string.IsNullOrEmpty(goal.buildingId)) return false;
                    costs.TryGetValue(goal.resource, out long total); costs[goal.resource] = total + goal.amount;
                }
            foreach (var cost in costs) if (cost.Value > resources.GetResourceAmount(cost.Key)) return false;
            var rewards = new System.Collections.Generic.Dictionary<ResourceType, long>();
            foreach (var reward in quest.resourceRewards)
            {
                if (reward == null || reward.resource == null || reward.amount <= 0) return false;
                rewards.TryGetValue(reward.resource, out long total);
                rewards[reward.resource] = total + reward.amount;
            }
            foreach (var reward in rewards)
            {
                costs.TryGetValue(reward.Key, out long expense);
                if (reward.Value + resources.GetResourceAmount(reward.Key) - expense > int.MaxValue) return false;
            }
            foreach (var unlock in quest.unlockRewards)
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.id)) return false;
            using (SaveSlotPrefs.Batch())
            using (GameFoundation.UI.GameNotifications.BeginAction())
            {
                // Set before resource events to reject synchronous duplicate claims.
                SaveSlotPrefs.SetInt("foundation.quest." + quest.id + ".claimed", 1);
                if (quest.consumeRequirementsOnClaim)
                {
                    SaveSlotPrefs.SetInt(Key(quest), 1);
                    foreach (var cost in costs) resources.TrySpendResource(cost.Key, (int)cost.Value);
                }
                foreach (var reward in rewards) resources.AddResource(reward.Key, (int)reward.Value);
                foreach (var unlock in quest.unlockRewards) ContentUnlocks.Grant(unlock);
                SaveSlotPrefs.Save();
                GameFoundation.UI.GameNotifications.Post("Награда получена: " + quest.title, GameFoundation.UI.NotificationKind.Positive);
            }
            ContentUnlocks.NotifyChanged();
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.QuestReward, .8f);
            Changed?.Invoke();
            return true;
        }

        public static bool TryComplete(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (quest == null || resources == null || string.IsNullOrWhiteSpace(quest.id) ||
                !IsAccepted(quest) || IsComplete(quest) || !quest.HasRequiredStock(resources.GetResourceAmount)) return false;
            if (IsFailed(quest) || quest.consumeRequirementsOnClaim) return false;
            SaveSlotPrefs.SetInt(Key(quest), 1);
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.QuestComplete, .75f);
            if (quest.unlockRewardsOnCompletion)
                foreach (var unlock in quest.unlockRewards)
                    if (unlock != null && !string.IsNullOrWhiteSpace(unlock.id)) ContentUnlocks.Grant(unlock);
            SaveSlotPrefs.Save();
            if (quest.unlockRewardsOnCompletion) ContentUnlocks.NotifyChanged();
            Changed?.Invoke();
            return true;
        }

        public static bool TryFail(QuestDefinition quest, GlobalResourceManager resources)
        {
            if (quest == null || resources == null || quest.deadlineDays <= 0 || !IsAccepted(quest) || IsClaimed(quest) || IsFailed(quest) || DaysRemaining(quest) > 0) return false;
            using (SaveSlotPrefs.Batch())
            using (GameFoundation.UI.GameNotifications.BeginAction())
            {
                // Persist the terminal state first: resource callbacks cannot apply the penalty twice.
                SaveSlotPrefs.SetInt("foundation.quest." + quest.id + ".failed", 1);
                if (quest.confiscateResourcesOnFailure)
                    foreach (var resource in resources.GetAllResourcesData().Keys)
                        if (resource != null && resource != quest.penaltyResource && !quest.protectedResources.Contains(resource)) resources.SetResourceAmount(resource, 0);
                if (quest.penaltyResource != null && quest.penaltyAmount > 0) resources.AddResource(quest.penaltyResource, -quest.penaltyAmount);
                SaveSlotPrefs.Save();
                GameFoundation.UI.GameNotifications.Post("Провалено: " + quest.title + (quest.confiscateResourcesOnFailure ? ". Припасы конфискованы." : ""), GameFoundation.UI.NotificationKind.Negative);
            }
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.UiDenied, .9f);
            Changed?.Invoke(); return true;
        }
        public static QuestDefinition Current(QuestCatalog catalog, bool parallel = false)
        {
            if (catalog == null || catalog.quests == null) return null;
            foreach (var quest in catalog.quests)
            {
                if (quest == null || quest.parallelQuest != parallel || IsFailed(quest)) continue;
                if (!IsClaimed(quest))
                {
                    if (parallel && !IsAccepted(quest)) continue;
                    return IsAccepted(quest) ? quest : null;
                }
            }
            return null;
        }
    }
}
