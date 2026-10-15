using GameFoundation.Saves;
using UnityEngine;
namespace GameFoundation.Quests
{
    public static class ContentUnlocks
    {
        public static event System.Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Changed = null;
        public static bool IsUnlocked(ContentUnlockDefinition definition) => definition == null ||
            (!string.IsNullOrWhiteSpace(definition.id) && SaveSlotPrefs.GetInt("foundation.unlock." + definition.id, 0) == 1);
        internal static void RestoreClaimedRewards(QuestCatalog catalog)
        {
            if (catalog == null || catalog.quests == null) return;
            bool changed = false;
            foreach (var quest in catalog.quests)
            {
                if (!QuestProgress.IsClaimed(quest)) continue;
                foreach (var unlock in quest.unlockRewards)
                {
                    if (unlock == null || string.IsNullOrWhiteSpace(unlock.id) || IsUnlocked(unlock)) continue;
                    Grant(unlock); changed = true;
                }
            }
            if (!changed) return;
            SaveSlotPrefs.Save();
            NotifyChanged();
        }
        internal static void Grant(ContentUnlockDefinition definition) => SaveSlotPrefs.SetInt("foundation.unlock." + definition.id, 1);
        internal static void NotifyChanged() => Changed?.Invoke();
    }
}
