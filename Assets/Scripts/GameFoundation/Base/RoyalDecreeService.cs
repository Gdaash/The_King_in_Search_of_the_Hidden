using System;
using GameFoundation.Saves;

namespace GameFoundation.Base
{
    /// <summary>Persistent, slot-specific settings selected in the Castle.</summary>
    public static class RoyalDecreeService
    {
        public const string CautiousWarriors = "cautious_warriors";
        public const string FinishOffEnemies = "finish_off_enemies";
        public const string ForestForaging = "forest_foraging";
        private const string Prefix = "foundation.decree.";

        public static event Action<string, bool> Changed;

        public static bool IsEnabled(string decreeId) =>
            !string.IsNullOrWhiteSpace(decreeId) && SaveSlotPrefs.GetInt(Prefix + decreeId, 0) != 0;

        public static void SetEnabled(string decreeId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(decreeId)) return;
            if (IsEnabled(decreeId) == enabled) return;
            SaveSlotPrefs.SetInt(Prefix + decreeId, enabled ? 1 : 0);
            SaveSlotPrefs.Save();
            string title = decreeId == CautiousWarriors ? "Не трус, а осторожный" : decreeId == FinishOffEnemies ? "Бей раненых" : decreeId == ForestForaging ? "Поиски еды в лесу" : "Указ";
            GameFoundation.UI.GameNotifications.Post((enabled ? "Указ включён: " : "Указ выключен: ") + title);
            Changed?.Invoke(decreeId, enabled);
        }

        public static void Toggle(string decreeId) => SetEnabled(decreeId, !IsEnabled(decreeId));
        public static bool TryEnable(RoyalDecreeDefinition decree)
        {
            if (decree == null || string.IsNullOrWhiteSpace(decree.id) || IsEnabled(decree.id) || ForestForagingService.IsPending) return false;
            var resources = GlobalResourceManager.Instance;
            if (resources == null || decree.influence == null || decree.activationCost < 0 || resources.GetResourceAmount(decree.influence) < decree.activationCost) return false;
            using var save = SaveSlotPrefs.Batch();
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            if (!resources.TrySpendResource(decree.influence, decree.activationCost)) return false;
            SetEnabled(decree.id, true);
            return true;
        }
    }
}
