using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Base
{
    public static class HousingDistrict
    {
        public const int HouseCount = 8;
        public const string Fields = "housing_fields";
        private const string Prefix = "foundation.housing.district.";
        private const int ExtraHouseCapacity = 5;
        public static int HouseCapacityGain(int index, int rank) => index == 0 && rank == 0
            ? BuildingUpgradeService.Catalog?.Find("housing")?.constructionCapacity ?? 10 : ExtraHouseCapacity;

        internal static void ResetHouseCards()
        {
            for (int i = 0; i < HouseCount; i++) SaveSlotPrefs.DeleteKey(Prefix + "house." + i);
        }

        internal static void ResetCoopProduction()
        {
            foreach (string key in new[] { "egg.first", "egg.second", "egg.third", "restingPairs" })
                SaveSlotPrefs.DeleteKey(Prefix + key);
        }

        public static int HouseRank(int index)
        {
            if (index < 0 || index >= HouseCount || !BuildingUpgradeService.IsBuilt("housing")) return 0;
            int legacy = BuildingUpgradeService.Level("housing");
            int inherited = index <= legacy ? 1 : 0;
            if (index < legacy - (HouseCount - 1)) inherited++;
            string key = Prefix + "house." + index;
            return SaveSlotPrefs.HasKey(key) ? Mathf.Clamp(SaveSlotPrefs.GetInt(key, inherited), 0, 2) : inherited;
        }

        public static int Capacity
        {
            get
            {
                int total = 0;
                for (int i = 0; i < HouseCount; i++)
                {
                    int rank = HouseRank(i);
                    if (rank > 0) total += HouseCapacityGain(i, 0) + (rank - 1) * ExtraHouseCapacity;
                }
                return total;
            }
        }

        public static BuildingUpgradeCatalog.Level HouseCost(int index)
        {
            int rank = HouseRank(index);
            var levels = BuildingUpgradeService.Catalog?.Find("housing")?.levels;
            if (levels == null || levels.Count == 0 || index < 0 || index >= HouseCount || rank >= 2) return null;
            int price = rank == 0 ? index - 1 : 7;
            return levels[Mathf.Clamp(price, 0, levels.Count - 1)];
        }

        public static bool IsHouseLocked(int index) => index > 0 && index < HouseCount &&
            HouseRank(index) == 0 && HouseRank(index - 1) == 0;

        public static bool CanPurchaseHouse(int index) => BuildingUpgradeService.IsBuilt("housing") && !IsHouseLocked(index) &&
            BuildingUpgradeService.CanPurchaseDistrictCost(HouseCost(index));

        public static bool CanPurchaseAnnex(string id) => BuildingUpgradeService.IsBuilt("housing") &&
            (BuildingUpgradeService.IsBuilt(id) ? BuildingUpgradeService.CanUpgrade(id) : BuildingUpgradeService.CanBuildAnnex(id));

        public static bool HasAvailableAction()
        {
            if (!BuildingUpgradeService.IsBuilt("housing")) return false;
            for (int i = 0; i < HouseCount; i++)
                if (CanPurchaseHouse(i)) return true;
            return CanPurchaseAnnex(ExpeditionTasks.Coop) ||
                CanPurchaseAnnex(Fields);
        }

        public static bool TryPurchaseHouse(int index)
        {
            if (!CanPurchaseHouse(index)) return false;
            int rank = HouseRank(index);
            return BuildingUpgradeService.TryPurchaseDistrictCost(HouseCost(index), () =>
                SaveSlotPrefs.SetInt(Prefix + "house." + index, rank + 1));
        }

        public static bool TryConvertHenToFood()
        {
            if (!ExpeditionTasks.CoopBuilt) return false;
            var chicken = ResourceCatalog.Find("Chicken");
            return GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.TryExchangeResources(
                chicken, 1, chicken, 0, ResourceCatalog.Find("Berry"), 1);
        }

        public readonly struct Production
        {
            public readonly int Chickens, Capacity, Chicks, Eggs, FieldFood;
            internal readonly int First, Second, RestingPairs;
            public int ChicksKept => Mathf.Min(Chicks, Mathf.Max(0, Capacity - Chickens));
            public int Food => Eggs + Chicks - ChicksKept + FieldFood;
            internal Production(int chickens, int capacity, int chicks, int eggs, int fieldFood, int first, int second, int pairCredit)
            {
                Chickens = chickens; Capacity = capacity; Chicks = chicks; Eggs = eggs;
                FieldFood = fieldFood; First = first; Second = second; RestingPairs = pairCredit;
            }
        }

        public static Production Forecast()
        {
            var stock = GlobalResourceManager.Instance;
            int chickens = ExpeditionTasks.CoopBuilt ? stock?.GetResourceAmount(ResourceCatalog.Find("Chicken")) ?? 0 : 0;
            int fieldFood = BuildingUpgradeService.IsBuilt(Fields)
                ? BuildingUpgradeService.Catalog?.Find(Fields)?.constructionCapacity ?? 0 : 0;
            return Calculate(chickens, ExpeditionTasks.CoopCapacity, fieldFood,
                SaveSlotPrefs.GetInt(Prefix + "egg.first"), SaveSlotPrefs.GetInt(Prefix + "egg.second"),
                SaveSlotPrefs.GetInt(Prefix + "egg.third"), SaveSlotPrefs.GetInt(Prefix + "restingPairs"));
        }

        // Age buckets preserve each hen's three-day laying cadence. New birds start at age zero.
        internal static Production Calculate(int chickens, int capacity, int fieldFood,
            int first, int second, int third, int pairCredit)
        {
            third = Mathf.Clamp(third, 0, chickens);
            second = Mathf.Clamp(second, 0, chickens - third);
            first = Mathf.Clamp(first, 0, chickens - third - second);
            first += chickens - first - second - third;
            int pairs = chickens / 2;
            int completed = Mathf.Clamp(pairCredit, 0, pairs);
            return new Production(chickens, capacity, completed, third, fieldFood, first, second, pairs - completed);
        }

        public static void Produce()
        {
            var stock = GlobalResourceManager.Instance;
            if (stock == null) return;
            var result = Forecast();
            if (ExpeditionTasks.CoopBuilt)
            {
                SaveSlotPrefs.SetInt(Prefix + "egg.first", result.Eggs + result.ChicksKept);
                SaveSlotPrefs.SetInt(Prefix + "egg.second", result.First);
                SaveSlotPrefs.SetInt(Prefix + "egg.third", result.Second);
                SaveSlotPrefs.SetInt(Prefix + "restingPairs", result.RestingPairs);
                if (result.ChicksKept > 0) stock.AddResource(ResourceCatalog.Find("Chicken"), result.ChicksKept);
            }
            if (result.Food > 0) stock.AddResource(ResourceCatalog.Find("Berry"), result.Food);

        }
    }
}
