using System.Globalization;
using System.Linq;
using GameFoundation.Saves;
using UnityEngine;
using Weapon = GameFoundation.MetaProgression.PortalTowerBalance.Weapon;
namespace GameFoundation.MetaProgression
{
    internal static class PortalWeaponEnhancements
    {
        private const string Prefix = PortalArsenalProgress.Prefix + "enhancement.";
        private static PortalWeaponChallengeTable table;
        internal static PortalWeaponChallengeTable Table => table != null ? table : table = Resources.Load<PortalWeaponChallengeTable>("World/Weapon Challenges");
        internal static int Kills(string id) => SaveSlotPrefs.GetInt(PortalArsenalProgress.Prefix + "kills." + id);
        internal static int Rank(Weapon weapon)
        {
            int rank = SaveSlotPrefs.GetInt(Prefix + weapon + ".rank", -1);
            // Retain both previously earned upgrades, including the paid ore upgrade.
            if (rank < 0) rank = weapon == Weapon.Bolts ?
                (SaveSlotPrefs.GetInt(Prefix + "bolts.kills") != 0 ? 1 : 0) +
                (SaveSlotPrefs.GetInt(Prefix + "bolts.ore") != 0 ? 1 : 0) : 0;
            return Mathf.Clamp(rank, 0, Table?.Find(weapon)?.ranks.Count ?? 0);
        }
        internal static PortalWeaponChallengeTable.Enhancement Next(Weapon weapon)
        {
            var ranks = Table?.Find(weapon)?.ranks;
            int rank = Rank(weapon);
            return ranks != null && rank < ranks.Count ? ranks[rank] : null;
        }
        private static PortalWeaponChallengeTable.Enhancement Current(Weapon weapon) => Rank(weapon) > 0 ? Table.Find(weapon).ranks[Rank(weapon) - 1] : null;
        internal static float Damage(Weapon weapon) => Current(weapon)?.damage ?? 0;
        internal static float IntervalReduction(Weapon weapon) => Current(weapon)?.intervalReduction ?? 0;
        internal static float Support(Weapon weapon) => Current(weapon)?.support ?? 0;
        internal static bool CanPurchase(Weapon weapon) => PortalArsenalProgress.IsUnlocked(weapon) && Next(weapon) is { } next &&
            next.kills.Count > 0 && next.kills.All(k => Kills(k.enemyId) >= k.count);
        internal static bool HasAvailable => Table != null && Table.weapons.Any(w => CanPurchase(w.weapon));
        internal static bool TryPurchase(Weapon weapon)
        {
            if (!CanPurchase(weapon)) return false;
            using var batch = SaveSlotPrefs.Batch();
            SaveSlotPrefs.SetInt(Prefix + weapon + ".rank", Rank(weapon) + 1);
            GameSaveService.CommitArsenalUnlock(SaveSlotPrefs.SelectedSlot, null, 0);
            GameFoundation.Base.BuildingUpgradeService.NotifyChanged();
            return true;
        }
        internal static void Reset()
        {
            foreach (Weapon weapon in System.Enum.GetValues(typeof(Weapon))) SaveSlotPrefs.DeleteKey(Prefix + weapon + ".rank");
            SaveSlotPrefs.DeleteKey(Prefix + "bolts.kills");
            SaveSlotPrefs.DeleteKey(Prefix + "bolts.ore");
        }
        internal static string KillText(PortalWeaponChallengeTable.KillRequirement k)
        {
            string name = PortalArsenalPopup.T("enemy." + k.enemyId, k.enemyId);
            bool ready = Kills(k.enemyId) >= k.count;
            return $"{name}: <color=#{(ready ? "91D66C" : "FF8990")}>{Mathf.Min(Kills(k.enemyId), k.count)}/{k.count}</color>";
        }
        internal static string Hint(Weapon weapon)
        {
            var next = Next(weapon);
            if (next == null) return PortalArsenalPopup.T("enhance.maximum", "Все усиления изучены");
            var lines = new System.Collections.Generic.List<string>();
            void Gain(string key, string fallback, float value, string sign)
            {
                if (value > .0001f) lines.Add($"<color=#91D66C>{PortalArsenalPopup.T(key, fallback)}: {sign}{value.ToString("0.##", CultureInfo.InvariantCulture)}</color>");
            }
            Gain("stat.damage", "Урон", next.damage - Damage(weapon), "+");
            Gain("stat.interval", "Интервал между атаками", next.intervalReduction - IntervalReduction(weapon), "−");
            Gain(weapon == Weapon.HealingRay ? "stat.healing" : weapon == Weapon.MagicShield ? "stat.shield" : "stat.armor", "Сила защиты", next.support - Support(weapon), "+");
            lines.Add(PortalArsenalPopup.T("enhance.requirements", "Победить врагов за все вылазки:"));
            lines.AddRange(next.kills.Select(KillText));
            return string.Join("\n", lines);
        }
    }
}
