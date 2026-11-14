using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>Permanent destination access uses the shared building upgrade catalog and save slot.</summary>
    public static class PortalProgression
    {
        public const string BuildingId = "portal";
        public static bool BalancePreviewAllLocations => Resources.Load<PortalTowerBalance>("World/PortalTowerBalance")?.openAllLocationsForBalanceTest == true;
        public static bool DemoRestrictionsEnabled => !BalancePreviewAllLocations;
        public const string DemoLocationId = "forest";
        public static bool IsDemoLocked(PortalLocationDefinition location) => DemoRestrictionsEnabled && location != null && location.LocationId != DemoLocationId;
        private const string LevelKey = "foundation.building.portal.upgradeLevel";
        private const string MigrationKey = "foundation.portal.upgradesMigrated";
        private const string CityMigrationKey = "foundation.portal.cityAccessMigrated";
        internal static void ResetDestinationUpgrades()
        {
            SaveSlotPrefs.SetInt(LevelKey, 0);
            SaveSlotPrefs.SetInt(MigrationKey, 1);
            SaveSlotPrefs.SetInt(CityMigrationKey, 1);
        }
        public static int Level => BuildingUpgradeService.Level(BuildingId);
        public static int MaxLevel => BuildingUpgradeService.Catalog?.Find(BuildingId)?.levels.Count ?? 0;
        public static bool IsUnlocked(PortalLocationDefinition location) => location != null && (BalancePreviewAllLocations || !IsDemoLocked(location) && Level >= location.RequiredPortalLevel);

        public static BuildingUpgradeCatalog.Level AccessPrice(PortalLocationDefinition location)
        {
            var levels = BuildingUpgradeService.Catalog?.Find(BuildingId)?.levels;
            int index = (location?.RequiredPortalLevel ?? 0) - 1;
            return levels != null && index >= 0 && index < levels.Count ? levels[index] : null;
        }

        public static bool IsNextDestination(PortalLocationDefinition location) =>
            location != null && location.RequiredPortalLevel == Level + 1 && AccessPrice(location) != null;

        public static bool TryUnlockDestination(PortalLocationDefinition location) =>
            IsNextDestination(location) && BuildingUpgradeService.TryUpgrade(BuildingId);

        public static string DestinationsAt(int level) => string.Join(", ", DayCycleService.GetPortalLocations()
            .Where(l => l.RequiredPortalLevel == level).Select(l => Name(l)));

        public static string Name(PortalLocationDefinition location)
        {
            if (location == null) return "";
            string text = LocalizationService.Instance?.Get(location.NameKey);
            return string.IsNullOrEmpty(text) || text == location.NameKey ? location.FallbackName : text;
        }

        public static void MigrateLegacyAccess(IEnumerable<PortalSite> sites)
        {
            bool migrateCity = SaveSlotPrefs.GetInt(CityMigrationKey, 0) == 0;
            bool migrateLegacy = SaveSlotPrefs.GetInt(MigrationKey, 0) == 0;
            if (!migrateCity && !migrateLegacy) return;

            int level = Level;
            if (migrateCity && level > 0) level++;
            if (migrateLegacy)
            {
                // Previously paid destinations remain accessible when an existing save is upgraded.
                int legacyLevel = sites.Where(s => s.active)
                    .Select(s => DayCycleService.GetPortalLocation(s.locationId)?.RequiredPortalLevel ?? 0)
                    .DefaultIfEmpty(0).Max();
                level = Mathf.Max(level, legacyLevel);
            }

            SaveSlotPrefs.SetInt(LevelKey, Mathf.Clamp(level, 0, MaxLevel));
            SaveSlotPrefs.SetInt(CityMigrationKey, 1);
            SaveSlotPrefs.SetInt(MigrationKey, 1);
            SaveSlotPrefs.Save();
        }
    }
}
