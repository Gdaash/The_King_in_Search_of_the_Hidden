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
        private const string LevelKey = "foundation.building.portal.upgradeLevel";
        private const string MigrationKey = "foundation.portal.upgradesMigrated";
        public static int Level => BuildingUpgradeService.Level(BuildingId);
        public static int MaxLevel => BuildingUpgradeService.Catalog?.Find(BuildingId)?.levels.Count ?? 0;
        public static bool IsUnlocked(PortalLocationDefinition location) => location != null && Level >= location.RequiredPortalLevel;

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
            if (SaveSlotPrefs.GetInt(MigrationKey, 0) != 0) return;
            // Previously paid destinations remain accessible when an existing save is upgraded.
            int level = sites.Where(s => s.active).Select(s => DayCycleService.GetPortalLocation(s.locationId)?.RequiredPortalLevel ?? 0)
                .DefaultIfEmpty(0).Max();
            SaveSlotPrefs.SetInt(LevelKey, Mathf.Clamp(Mathf.Max(Level, level), 0, MaxLevel));
            SaveSlotPrefs.SetInt(MigrationKey, 1);
            SaveSlotPrefs.Save();
        }
    }
}
