using System;
using GameFoundation.Saves;

namespace GameFoundation.Base
{
    /// <summary>Persistent, slot-specific settings selected in the Castle.</summary>
    public static class RoyalDecreeService
    {
        public const string CautiousWarriors = "cautious_warriors";
        public const string FinishOffEnemies = "finish_off_enemies";
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
            Changed?.Invoke(decreeId, enabled);
        }

        public static void Toggle(string decreeId) => SetEnabled(decreeId, !IsEnabled(decreeId));
    }
}
