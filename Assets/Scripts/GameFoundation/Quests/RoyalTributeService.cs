using GameFoundation.Saves;
using GameFoundation.Localization;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GameFoundation.Quests
{
    /// <summary>The recurring royal levy and cart assistance surcharge belong to the current save slot.</summary>
    public static class RoyalTributeService
    {
        public const string QuestId = "royal_magic_tribute";
        public const string CartDebtKey = "foundation.royal.cartDebt";
        public const string ResidentDebtKey = "foundation.royal.residentDebt";
        public static int ResidentDebt => Mathf.Max(0, SaveSlotPrefs.GetInt(ResidentDebtKey, 0));
        public const string ResidentDebtTemplate = "И ещё {0} единиц магической руды за предоставленных вам жителей.";
        public const string ChangesKey = "foundation.royal.noticeChanges";
        [System.Serializable] private sealed class Delta { public string id; public int amount; }
        [System.Serializable] private sealed class Changes { public List<Delta> items = new(); }
        public static List<QuestDefinition.ResourceReward> NoticeChanges()
        {
            string json = SaveSlotPrefs.GetString(ChangesKey, "");
            var result = new List<QuestDefinition.ResourceReward>(); if (string.IsNullOrEmpty(json)) return result;
            var data = JsonUtility.FromJson<Changes>(json);
            var types = Resources.LoadAll<ResourceType>("ResourceTypes");
            foreach(var delta in data.items)
            {
                var resource = types.FirstOrDefault(r=>r.Id==delta.id);
                if(resource!=null&&delta.amount!=0)result.Add(new QuestDefinition.ResourceReward{resource=resource,amount=delta.amount});
            }
            return result;
        }
        public const string NoticeKey = "foundation.royal.notice";
        public static int CartDebt => Mathf.Max(0, SaveSlotPrefs.GetInt(CartDebtKey, 0));
        public static bool IsRoyal(QuestDefinition quest) => quest != null && quest.id == QuestId;
        public static int Required(QuestDefinition quest, QuestDefinition.Requirement goal) => goal.amount +
            (IsRoyal(quest) && quest.requirements.Count > 0 && quest.requirements[0] == goal ? CartDebt + ResidentDebt : 0);
        public const string DebtLineTemplate = "И ещё {0} единиц магической руды за предоставленные вам телеги. Королевская помощь тоже подлежит учёту.";
        public static string DebtLine => GameText.Format(DebtLineTemplate, CartDebt);
        public static string Description(QuestDefinition quest) => GameText.Get(quest.description) + (IsRoyal(quest) && CartDebt > 0 ? "\n\n" + DebtLine : "") + (IsRoyal(quest) && ResidentDebt > 0 ? "\n\n" + GameText.Format(ResidentDebtTemplate, ResidentDebt) : "");
        public static void Process(QuestCatalog catalog, GlobalResourceManager resources)
        {
            if (PlaytestSettings.TutorialsAndQuestsDisabled || catalog == null || resources == null) return;
            foreach (var quest in catalog.quests)
            {
                if (!IsRoyal(quest) || !QuestProgress.IsAccepted(quest)) continue;
                int today = GameFoundation.MetaProgression.DayCycleService.Instance?.Day ?? 1;
                string prefix = "foundation.quest." + quest.id + ".";
                if (QuestProgress.DeadlineDay(quest) <= 0) { Restart(quest, today + 30); QuestProgress.NotifyChanged(); return; }
                // Migrate old terminal quests into the recurring levy without charging them twice.
                if (QuestProgress.IsClaimed(quest) || QuestProgress.IsFailed(quest))
                {
                    Restart(quest, today + 30); QuestProgress.NotifyChanged(); return;
                }
                if (QuestProgress.DeadlineDay(quest) > today) continue;
                var before = new Dictionary<ResourceType,int>(resources.GetAllResourcesData());
                bool paid = quest.HasRequiredStock(resources.GetResourceAmount);
                int due = quest.requirements.Count > 0 ? Required(quest, quest.requirements[0]) : 30;
                using (SaveSlotPrefs.Batch())
                using (GameFoundation.UI.GameNotifications.BeginAction())
                {
                    // Renew before emitting resource changes: no callback can settle this period twice.
                    Restart(quest, today + 30);
                    SaveSlotPrefs.SetInt(CartDebtKey, 0);
                    SaveSlotPrefs.SetInt(ResidentDebtKey, 0);
                    if (paid)
                    {
                        foreach (var goal in quest.requirements) resources.TrySpendResource(goal.resource, goal.amount + (goal == quest.requirements[0] ? due - goal.amount : 0));
                        foreach (var reward in quest.resourceRewards) resources.AddResource(reward.resource, reward.amount);
                    }
                    else
                    {
                        if (quest.confiscateResourcesOnFailure)
                            foreach (var resource in resources.GetAllResourcesData().Keys)
                                if (quest.CanConfiscate(resource)) resources.SetResourceAmount(resource, 0);
                        if (quest.penaltyResource != null && quest.penaltyAmount > 0) resources.AddResource(quest.penaltyResource, -quest.penaltyAmount);
                    }
                    var changes = new Changes();
                    foreach(var resource in before.Keys.Union(resources.GetAllResourcesData().Keys))
                    {
                        before.TryGetValue(resource,out int previous);
                        int delta = resources.GetResourceAmount(resource)-previous;
                        if(delta!=0)changes.items.Add(new Delta{id=resource.Id,amount=delta});
                    }
                    SaveSlotPrefs.SetString(ChangesKey,JsonUtility.ToJson(changes));
                    SaveSlotPrefs.SetString(NoticeKey, paid ? "paid:" + due : "failed:" + due);
                    SaveSlotPrefs.Save();
                }
                QuestProgress.NotifyChanged();
            }
        }
        private static void Restart(QuestDefinition quest, int deadline)
        {
            string prefix = "foundation.quest." + quest.id + ".";
            SaveSlotPrefs.SetInt(prefix + "accepted", 1);
            SaveSlotPrefs.SetInt(prefix + "completed", 0);
            SaveSlotPrefs.SetInt(prefix + "claimed", 0);
            SaveSlotPrefs.SetInt(prefix + "failed", 0);
            SaveSlotPrefs.SetInt(prefix + "deadline", deadline);
            SaveSlotPrefs.Save();
        }
    }
}
