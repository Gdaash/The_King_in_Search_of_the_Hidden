using GameFoundation.Base;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public static class ExpeditionTasks
    {
        public const string Coop = "chicken_coop";
        public const string Tannery = "tannery";
        private const float BoarHideChance = .20f;
        private static int chickensRemaining;
        private static int chickensBefore;
        private static bool collectHides;
        private static bool active;

        public static bool CoopBuilt => BuildingUpgradeService.IsBuilt(Coop);
        public static bool TanneryBuilt => BuildingUpgradeService.IsBuilt(CraftQuarterLayout.Tannery);
        public static int CoopCapacity => BuildingUpgradeService.Catalog?.Find(Coop) != null
            ? BuildingUpgradeService.Capacity(Coop) : 5;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset() => active = false;

        public static void Begin()
        {
            chickensBefore = GlobalResourceManager.Instance != null
                ? GlobalResourceManager.Instance.GetResourceAmount(ResourceCatalog.Find("Chicken")) : 0;
            chickensRemaining = CoopBuilt ? Mathf.Max(0, CoopCapacity - chickensBefore) : 0;
            collectHides = TanneryBuilt;
            active = true;
        }

        public static void ConvertAnimalDrop(ResourceItem item, ExpeditionAnimalSource.Kind source)
        {
            if (!active || item == null || item.type == null || item.type.Id != "Berry") return;
            if (source != ExpeditionAnimalSource.Kind.Chicken || chickensRemaining <= 0) return;
            var replacement = ResourceCatalog.Find("Chicken");
            if (replacement == null) return;
            SetDropType(item, replacement);
            chickensRemaining--;
        }

        public static ResourceType RollBonusDrop(ResourceItem item, ExpeditionAnimalSource.Kind source) =>
            active && collectHides && source == ExpeditionAnimalSource.Kind.Boar &&
            item?.type?.Id == "Berry" && Random.value < BoarHideChance ? ResourceCatalog.Find("Hide") : null;

        public static void SetDropType(ResourceItem item, ResourceType replacement)
        {
            item.type = replacement;
            item.carrySprite = replacement.defaultCarrySprite;
            if (replacement.groundSprite != null && item.TryGetComponent<SpriteRenderer>(out var visual))
                visual.sprite = replacement.groundSprite;
        }

        public static void Settle()
        {
            if (!active) return;
            active = false;
            var stock = GlobalResourceManager.Instance;
            var chicken = ResourceCatalog.Find("Chicken");
            var food = ResourceCatalog.Find("Berry");
            if (stock == null || chicken == null || food == null) return;
            int amount = stock.GetResourceAmount(chicken);
            int overflow = Mathf.Min(Mathf.Max(0, amount - chickensBefore), Mathf.Max(0, amount - CoopCapacity));
            if (overflow == 0) return;
            using var batch = SaveSlotPrefs.Batch();
            if (stock.TrySpendResource(chicken, overflow)) stock.AddResource(food, overflow);
        }
    }
}
