using System;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Base
{
    public static class BuildingUpgradeService
    {
        public static event Action Changed;
        private static BuildingUpgradeCatalog catalog;
        private static bool purchasing;
        public static BuildingUpgradeCatalog Catalog => catalog != null ? catalog : catalog = Resources.Load<BuildingUpgradeCatalog>("Base/Building Upgrades");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { catalog = null; purchasing = false; Changed = null; }
        public static bool IsBuilt(string id) => Catalog?.Find(id)?.builtByDefault == true || SaveSlotPrefs.GetInt("foundation.building." + id + ".built", 0) != 0;
        public static int Level(string id) => Mathf.Clamp(SaveSlotPrefs.GetInt("foundation.building." + id + ".upgradeLevel", 0), 0, Catalog?.Find(id)?.levels.Count ?? 0);
        public static int Capacity(string id) => Catalog?.Find(id)?.Capacity(IsBuilt(id), Level(id)) ?? 0;
        public static int MilitaryCapacity(ResourceType warrior)
        {
            var definition = Catalog?.buildings.Find(b => b.capacityResource == warrior);
            return definition == null ? 0 : Capacity(definition.id);
        }
        // Soldiers have their own accommodation in the fort / range. Housing limits civilians.
        public static bool CanAdmitResident
        {
            get
            {
                var human = Catalog?.Find("housing")?.capacityResource;
                return human != null && GlobalResourceManager.Instance != null &&
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
            var cost = Next(id);
            var manager = GlobalResourceManager.Instance;
            if (purchasing || !IsBuilt(id) || cost == null || manager == null) return false;
            if (cost.costA < 0 || cost.costB < 0 || cost.costA > 0 && cost.resourceA == null || cost.costB > 0 && cost.resourceB == null) return false;
            return cost.resourceA != null && cost.resourceA == cost.resourceB
                ? manager.GetResourceAmount(cost.resourceA) >= cost.costA + cost.costB
                : (cost.costA == 0 || manager.GetResourceAmount(cost.resourceA) >= cost.costA) &&
                  (cost.costB == 0 || manager.GetResourceAmount(cost.resourceB) >= cost.costB);
        }
        public static bool TryUpgrade(string id)
        {
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
                SaveSlotPrefs.SetInt("foundation.building." + id + ".upgradeLevel", Level(id) + 1);
                SaveSlotPrefs.Save();
            }
            finally { purchasing = false; NotifyChanged(); }
            return true;
        }
        public static void NotifyChanged() => Changed?.Invoke();
    }
}
