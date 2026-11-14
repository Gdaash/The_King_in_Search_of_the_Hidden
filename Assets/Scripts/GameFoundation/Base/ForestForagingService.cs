using System;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using UnityEngine;
namespace GameFoundation.Base
{
    public static class ForestForagingService
    {
        private const string Key = "foundation.forestForaging";
        [Serializable] public sealed class Result
        {
            public int day, sent, survived, dead, food, humanDeaths, swordsmanDeaths, archerDeaths, spearmanDeaths, slingerDeaths;
            public long readyTicks;
            public bool completed;
        }
        private static string cachedJson;
        private static Result cachedResult;
        public static Result Current
        {
            get
            {
                string json = SaveSlotPrefs.GetString(Key, "{}");
                if (cachedResult == null || json != cachedJson)
                {
                    cachedJson = json;
                    cachedResult = JsonUtility.FromJson<Result>(json) ?? new Result();
                }
                return cachedResult;
            }
        }
        public static bool IsPending { get { var r = Current; return r.sent > 0 && !r.completed; } }
        public static float Remaining { get { var r = Current; return Mathf.Max(0, (float)((r.readyTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond)); } }
        public static bool UsedToday => Current.sent > 0 && Current.day == DayCycleService.Instance?.Day;
        public static bool CanStart(ForestForagingSettings settings)
        {
            var day = DayCycleService.Instance;
            var resources = GlobalResourceManager.Instance;
            return settings != null && settings.influence != null && settings.food != null &&
                settings.humans != null && settings.swordsmen != null && settings.archers != null &&
                day != null && resources != null && RoyalDecreeService.IsEnabled(RoyalDecreeService.ForestForaging) &&
                !UsedToday && !IsPending && day.GetFoodForecast().Starving > 0 &&
                resources.GetResourceAmount(settings.influence) >= Mathf.Max(0, settings.cost);
        }
        public static bool TryStart(ForestForagingSettings settings)
        {
            if (!CanStart(settings)) return false;
            var forecast = DayCycleService.Instance.GetFoodForecast();
            var result = new Result { day = DayCycleService.Instance.Day, sent = forecast.Starving,
                readyTicks = DateTime.UtcNow.AddSeconds(Mathf.Max(.1f, settings.duration)).Ticks };
            int humans = CraftProduction.FreeResidents(settings.humans), swordsmen = forecast.Swordsmen, archers = forecast.Archers, spearmen = forecast.Spearmen, slingers = forecast.Slingers;
            result.sent = Mathf.Min(result.sent, humans + swordsmen + archers + spearmen + slingers);
            if (result.sent == 0) return false;
            for (int i = 0; i < result.sent; i++)
            {
                int group = UnityEngine.Random.Range(0, humans + swordsmen + archers + spearmen + slingers);
                int type;
                if (group < humans) { humans--; type = 0; }
                else if (group < humans + swordsmen) { swordsmen--; type = 1; }
                else if (group < humans + swordsmen + archers) { archers--; type = 2; }
                else if (group < humans + swordsmen + archers + spearmen) { spearmen--; type = 3; }
                else { slingers--; type = 4; }
                if (UnityEngine.Random.value < settings.survivalChance)
                {
                    result.survived++;
                    int min = Mathf.Max(0, settings.minimumFood);
                    result.food += UnityEngine.Random.Range(min, Mathf.Max(min, settings.maximumFood) + 1);
                }
                else
                {
                    result.dead++;
                    if (type == 0) result.humanDeaths++;
                    else if (type == 1) result.swordsmanDeaths++;
                    else if (type == 2) result.archerDeaths++;
                    else if (type == 3) result.spearmanDeaths++;
                    else result.slingerDeaths++;
                }
            }
            using var batch = SaveSlotPrefs.Batch();
            // Reserve the daily attempt before resource callbacks can re-enter.
            SaveSlotPrefs.SetString(Key, JsonUtility.ToJson(result));
            GlobalResourceManager.Instance.TrySpendResource(settings.influence, Mathf.Max(0, settings.cost));
            SaveSlotPrefs.Save();
            ForagingInputLock.SetLocked(true);
            return true;
        }
        public static bool CompleteIfReady(ForestForagingSettings settings)
        {
            var result = Current;
            if (settings == null || result.sent == 0 || result.completed || Remaining > 0 || GlobalResourceManager.Instance == null) return false;
            using var batch = SaveSlotPrefs.Batch();
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            // All mutations, the ledger and the result are committed in one save envelope.
            result.completed = true;
            SaveSlotPrefs.SetString(Key, JsonUtility.ToJson(result));
            var resources = GlobalResourceManager.Instance;
            if (result.humanDeaths > 0) resources.TrySpendResource(settings.humans, result.humanDeaths);
            if (result.swordsmanDeaths > 0) resources.TrySpendResource(settings.swordsmen, result.swordsmanDeaths);
            if (result.archerDeaths > 0) resources.TrySpendResource(settings.archers, result.archerDeaths);
            var spearman = ResourceCatalog.Find("Spearman");
            if (result.spearmanDeaths > 0 && spearman != null) resources.TrySpendResource(spearman, result.spearmanDeaths);
            for (int i = 0; i < result.swordsmanDeaths; i++) MilitaryExperienceService.RemoveStored(settings.swordsmen);
            for (int i = 0; i < result.archerDeaths; i++) MilitaryExperienceService.RemoveStored(settings.archers);
            for (int i = 0; i < result.spearmanDeaths; i++) MilitaryExperienceService.RemoveStored(spearman);
            if (result.slingerDeaths > 0 && ResourceCatalog.Find("Slinger") is ResourceType slinger)
            {
                resources.TrySpendResource(slinger, result.slingerDeaths);
                for (int i = 0; i < result.slingerDeaths; i++) MilitaryExperienceService.RemoveStored(slinger);
            }
            if (result.food > 0) resources.AddResource(settings.food, result.food);
            SaveSlotPrefs.Save();
            ForagingInputLock.SetLocked(false);
            GameFoundation.UI.GameNotifications.Post(GameFoundation.Localization.GameText.Format("Поиски еды: выжило {0}, погибло {1}", result.survived, result.dead));
            return true;
        }
    }
}
