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
        public static bool CanUnlock(PortalArsenalCatalog.Entry entry) => GameFoundation.Base.CraftProduction.Morning && entry != null && !IsUnlocked(entry.weapon) && PortalProgression.Level >= entry.requiredPortalLevel && Progress(entry) >= entry.amount;
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
