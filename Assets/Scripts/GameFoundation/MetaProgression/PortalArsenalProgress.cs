using GameFoundation.Saves;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    /// <summary>Slot-local milestones. No cached unlock state can leak into another save.</summary>
    public static class PortalArsenalProgress
    {
        public static event System.Action UnlocksChanged;
        internal const string Prefix = "foundation.arsenal.";
        private static string UnlockKey(PortalTowerBalance.Weapon weapon) => Prefix + "unlocked." + weapon;
        internal static void ResetUnlocks()
        {
            foreach (PortalTowerBalance.Weapon weapon in System.Enum.GetValues(typeof(PortalTowerBalance.Weapon)))
                SaveSlotPrefs.SetInt(UnlockKey(weapon), 0);
            PortalWeaponEnhancements.Reset();
            PortalWeaponLoadout.ResetAssignments();
            UnlocksChanged?.Invoke();
        }
        public static bool IsUnlocked(PortalTowerBalance.Weapon weapon) => weapon == PortalTowerBalance.Weapon.Bolts || SaveSlotPrefs.GetInt(UnlockKey(weapon)) == 1;
        public static int Progress(PortalArsenalCatalog.Entry entry)
        {
            if (entry == null) return 0;
            return entry.requirement switch
            {
                PortalArsenalCatalog.Requirement.Starting => entry.amount,
                PortalArsenalCatalog.Requirement.EnemyKills => SaveSlotPrefs.GetInt(Prefix + "kills." + entry.enemyId),
                PortalArsenalCatalog.Requirement.Population => SaveSlotPrefs.GetInt(Prefix + "population"),
                PortalArsenalCatalog.Requirement.ExploredHexes => SaveSlotPrefs.GetInt(Prefix + "hexes"),
                PortalArsenalCatalog.Requirement.TowerLevel => SaveSlotPrefs.GetInt(Prefix + "level"),
                PortalArsenalCatalog.Requirement.Resource => entry.resource != null && GlobalResourceManager.Instance != null ? GlobalResourceManager.Instance.GetResourceAmount(entry.resource) : 0,
                _ => 0
            };
        }
        public static int RemainingEnhancements(PortalArsenalCatalog.Entry entry) => entry == null ? 0 : Mathf.Max(0, entry.requiredEnhancements - PortalWeaponEnhancements.Rank(entry.requiredWeapon));
        public static bool MeetsPrerequisites(PortalArsenalCatalog.Entry entry) => entry != null &&
            (PortalProgression.BalancePreviewAllLocations || PortalProgression.Level >= entry.requiredPortalLevel) && RemainingEnhancements(entry) == 0 &&
            (entry.requiredKills == null || entry.requiredKills.TrueForAll(k => PortalWeaponEnhancements.Kills(k.enemyId) >= k.count));
        public static string PrerequisiteHint(PortalArsenalCatalog.Entry entry)
        {
            if (entry == null) return "";
            var lines = new System.Collections.Generic.List<string>();
            if (entry.requiredEnhancements > 0)
            {
                var definition = Resources.Load<PortalTowerBalance>("World/PortalTowerBalance").FindWeapon(entry.requiredWeapon);
                string title = PortalArsenalPopup.T("weapon." + entry.requiredWeapon, definition.title);
                lines.Add(string.Format(PortalArsenalPopup.T("enhance.unlock", "РЈСЃРёР»РёС‚СЊ {0}: {1}/{2}"), title,
                    Mathf.Min(entry.requiredEnhancements, PortalWeaponEnhancements.Rank(entry.requiredWeapon)), entry.requiredEnhancements));
            }
            if (!PortalProgression.BalancePreviewAllLocations && entry.requiredPortalLevel > PortalProgression.Level)
                lines.Add(string.Format(GameFoundation.Base.PortalPopupView.Tr("base.portal.requires_level", "РЈР»СѓС‡С€РёС‚Рµ РїРѕСЂС‚Р°Р» РґРѕ СѓСЂ. {0}"), entry.requiredPortalLevel));
            if (entry.requiredKills != null) foreach (var k in entry.requiredKills) lines.Add(PortalWeaponEnhancements.KillText(k));
            return string.Join("\n", lines);
        }
        public static bool CanUnlock(PortalArsenalCatalog.Entry entry) => entry != null && !IsUnlocked(entry.weapon) && MeetsPrerequisites(entry) && Progress(entry) >= entry.amount;
        public static bool TryUnlock(PortalArsenalCatalog.Entry entry)
        {
            if (!CanUnlock(entry)) return false;
            int cost = entry.requirement == PortalArsenalCatalog.Requirement.Resource ? entry.amount : 0;
            using (SaveSlotPrefs.Batch())
            {
                if (cost > 0 && (GlobalResourceManager.Instance == null || !GlobalResourceManager.Instance.TrySpendResource(entry.resource, cost))) return false;
                SaveSlotPrefs.SetInt(UnlockKey(entry.weapon), 1);
                GameSaveService.CommitArsenalUnlock(SaveSlotPrefs.SelectedSlot, cost > 0 ? "Global_Resource_" + entry.resource.Id : null, cost);
            }
            UnlocksChanged?.Invoke();
            return true;
        }
        public static void RecordKill(string id) { if (!string.IsNullOrEmpty(id)) Increment("kills." + id); }
        public static void RecordHex() => Increment("hexes");
        public static void RecordLevel(int level) => Peak("level", level);
        private static void Increment(string key)
        {
            int count = SaveSlotPrefs.GetInt(Prefix + key);
            if (count < int.MaxValue) SaveSlotPrefs.SetInt(Prefix + key, count + 1);
        }
        private static void Peak(string key, int amount)
        {
            if (amount > SaveSlotPrefs.GetInt(Prefix + key)) SaveSlotPrefs.SetInt(Prefix + key, amount);
        }
        public static void RecordPopulation()
        {
            var resources = GlobalResourceManager.Instance;
            if (resources == null) return;
            int total = 0;
            foreach (var type in ResourceCatalog.All)
                if (type.Id == "Human" || type.isMilitaryUnit) total += Mathf.Max(0, resources.GetResourceAmount(type));
            Peak("population", total);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Subscribe()
        {
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
        }
        private static void ResourceChanged(ResourceType type, int value)
        {
            if (type != null && (type.Id == "Human" || type.isMilitaryUnit)) RecordPopulation();
        }
    }
}
