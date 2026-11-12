using System;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Base
{
    public static class BuildingUpgradeService
    {
        public static event Action Changed;
        private static BuildingUpgradeCatalog catalog;
        private static bool purchasing;
        public static BuildingUpgradeCatalog Catalog => catalog != null ? catalog : catalog = ProjectReferences.Instance != null ? ProjectReferences.Instance.buildingUpgrades : null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { catalog = null; purchasing = false; Changed = null; }
        public static bool IsBuilt(string id) =>
            ((id == CraftQuarterLayout.LeatherShop || id == CraftQuarterLayout.Tannery) && SaveSlotPrefs.GetInt("foundation.building.tannery.built", 0) != 0) ||
            Catalog?.Find(id)?.builtByDefault == true || SaveSlotPrefs.GetInt("foundation.building." + id + ".built", 0) != 0;
        private const string GarrisonSlotLevelKey = "foundation.building.garrison.slotLevel";
        public static int Level(string id)
        {
            int oldLevel = SaveSlotPrefs.GetInt("foundation.building." + id + ".upgradeLevel", 0);
            int level = id == Garrison ? SaveSlotPrefs.GetInt(GarrisonSlotLevelKey, oldLevel switch
            {
                1 => 4, 2 => 7, 3 => 10, >= 4 => 14,
                _ => GarrisonCount(GlobalResourceManager.Instance) >= 3 ? 1 : 0
            }) : oldLevel;
            return Mathf.Clamp(level, 0, Catalog?.Find(id)?.levels.Count ?? 0);
        }
        public static int Capacity(string id) => id == "housing" ? HousingDistrict.Capacity : Catalog?.Find(id)?.Capacity(IsBuilt(id), Level(id)) ?? 0;
        internal static bool CanPurchaseDistrictCost(BuildingUpgradeCatalog.Level cost) =>
            CraftProduction.Morning && !purchasing && cost != null && GlobalResourceManager.Instance is GlobalResourceManager manager &&
            (cost.costA == 0 || cost.resourceA != null) && (cost.costB == 0 || cost.resourceB != null) && CanAfford(cost, manager);

        internal static bool TryPurchaseDistrictCost(BuildingUpgradeCatalog.Level cost, Action apply)
        {
            if (!CanPurchaseDistrictCost(cost)) return false;
            using var save = SaveSlotPrefs.Batch();
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            var stock = GlobalResourceManager.Instance;
            purchasing = true;
            try
            {
                if (cost.costA > 0 && !stock.TrySpendResource(cost.resourceA, cost.costA)) return false;
                if (cost.costB > 0 && !stock.TrySpendResource(cost.resourceB, cost.costB))
                {
                    if (cost.costA > 0) stock.AddResource(cost.resourceA, cost.costA);
                    return false;
                }
                apply();
                SaveSlotPrefs.Save();
                return true;
            }
            finally { purchasing = false; NotifyChanged(); }
        }
        internal const string Garrison = "garrison";
        internal const int GarrisonMaxCapacity = 20;

        internal static bool IsGarrisonUnit(ResourceType resource) =>
            resource != null && resource.isMilitaryUnit && resource.Id != "Engineer";

        internal static int GarrisonCount(GlobalResourceManager stock)
        {
            if (stock == null) return 0;
            int count = 0;
            foreach (var resource in ResourceCatalog.All)
                if (IsGarrisonUnit(resource)) count += stock.GetResourceAmount(resource);
            return count;
        }

        public static int MilitaryCapacity(ResourceType warrior)
        {
            if (!IsGarrisonUnit(warrior)) return 0;
            string facility = warrior.Id switch
            {
                "Slinger" or "Spearman" => "barracks",
                "Archer" or "Crossbowman" => "archery_range",
                "Swordsman" => "fort",
                _ => Catalog?.buildings.Find(building => building.capacityResource == warrior)?.id
            };
            return facility != null && IsBuilt(facility) ? Capacity(Garrison) : 0;
        }

        public static bool CanAdmitResident
        {
            get
            {
                var human = Catalog?.Find("housing")?.capacityResource;
                return CraftProduction.Morning && human != null && GlobalResourceManager.Instance != null &&
                    GlobalResourceManager.Instance.GetResourceAmount(human) < Capacity("housing");
            }
        }
        public static BuildingUpgradeCatalog.Level Next(string id)
        {
            var b = Catalog?.Find(id);
            int level = Level(id);
            return b != null && level < b.levels.Count ? b.levels[level] : null;
        }
        public static bool CanUpgrade(string id)
        {
            if (id == GameFoundation.MetaProgression.PortalProgression.BuildingId && GameFoundation.MetaProgression.PortalProgression.DemoRestrictionsEnabled) return false;
            var cost = Next(id);
            var manager = GlobalResourceManager.Instance;
            if (!CraftProduction.Morning || purchasing || !IsBuilt(id) || cost == null || manager == null) return false;
            if (cost.costA < 0 || cost.costB < 0 || cost.costA > 0 && cost.resourceA == null || cost.costB > 0 && cost.resourceB == null) return false;
            return CanAfford(cost, manager);
        }

        private static bool CanAfford(BuildingUpgradeCatalog.Level cost, GlobalResourceManager manager)
        {
            return cost.resourceA != null && cost.resourceA == cost.resourceB
                ? manager.GetResourceAmount(cost.resourceA) >= cost.costA + cost.costB
                : (cost.costA == 0 || manager.GetResourceAmount(cost.resourceA) >= cost.costA) &&
                  (cost.costB == 0 || manager.GetResourceAmount(cost.resourceB) >= cost.costB);
        }
        public static bool TryUpgrade(string id)
        {
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            if (!CanUpgrade(id)) return false;
            var next = Next(id);
            var manager = GlobalResourceManager.Instance;
            purchasing = true;
            try
            {
                if (next.costA > 0 && !manager.TrySpendResource(next.resourceA, next.costA)) return false;
                if (next.costB > 0 && !manager.TrySpendResource(next.resourceB, next.costB))
                {
                    if (next.costA > 0) manager.AddResource(next.resourceA, next.costA);
                    return false;
                }
                SaveSlotPrefs.SetInt(id == Garrison ? GarrisonSlotLevelKey : "foundation.building." + id + ".upgradeLevel", Level(id) + 1);
                SaveSlotPrefs.Save();
                var definition = Catalog.Find(id);
                if (id == "housing")
                    GameFoundation.UI.GameNotifications.Post(GameFoundation.Localization.GameText.Get("Построено: ") +
                        LocalizedName("base.housing.house_name", "Дом"), GameFoundation.UI.NotificationKind.Positive);
                else
                    GameFoundation.UI.GameNotifications.Post(GameFoundation.Localization.GameText.Get("Улучшено: ") + definition.displayName + GameFoundation.Localization.GameText.Get(" · ур. ") + Level(id), GameFoundation.UI.NotificationKind.Positive);
            }
            finally { purchasing = false; NotifyChanged(); }
            return true;
        }
        private static string AnnexDistrict(string id) =>
            id == CraftQuarterLayout.Tannery || id == CraftQuarterLayout.Smithy || id == CraftQuarterLayout.LeatherShop || id == CraftQuarterLayout.CharcoalPit
                ? "blacksmith" : "housing";

        public static bool CanBuildAnnex(string id)
        {
            var cost = Catalog?.Find(id)?.constructionCost;
            var manager = GlobalResourceManager.Instance;
            return id != ExpeditionTasks.Tannery && CraftProduction.Morning && !purchasing && IsBuilt(AnnexDistrict(id)) && !IsBuilt(id) && cost != null && manager != null &&
                (cost.costA == 0 || cost.resourceA != null) && (cost.costB == 0 || cost.resourceB != null) &&
                CanAfford(cost, manager);
        }

        public static bool TryBuildAnnex(string id)
        {
            if (!CanBuildAnnex(id)) return false;
            using var save = SaveSlotPrefs.Batch();
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            var definition = Catalog.Find(id);
            var cost = definition.constructionCost;
            var manager = GlobalResourceManager.Instance;
            purchasing = true;
            try
            {
                if (cost.costA > 0 && !manager.TrySpendResource(cost.resourceA, cost.costA)) return false;
                if (cost.costB > 0 && !manager.TrySpendResource(cost.resourceB, cost.costB))
                {
                    if (cost.costA > 0) manager.AddResource(cost.resourceA, cost.costA);
                    return false;
                }
                SaveSlotPrefs.SetInt("foundation.building." + id + ".built", 1);
                SaveSlotPrefs.SetInt("foundation.building." + id + ".builtDay",
                    GameFoundation.MetaProgression.DayCycleService.Instance?.Day ?? 1);
                SaveSlotPrefs.Save();
                GameFoundation.UI.GameNotifications.Post(GameFoundation.Localization.GameText.Get("Построено: ") +
                    LocalizedName(definition.nameKey, definition.displayName),
                    GameFoundation.UI.NotificationKind.Positive);
                return true;
            }
            finally { purchasing = false; NotifyChanged(); }
        }

        private static string LocalizedName(string key, string fallback)
        {
            var value = GameFoundation.Localization.LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        public static void NotifyChanged() => Changed?.Invoke();
    }
}
