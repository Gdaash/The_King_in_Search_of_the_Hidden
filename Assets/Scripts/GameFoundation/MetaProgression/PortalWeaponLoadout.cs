using System;
using GameFoundation.Saves;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    /// <summary>Slot-local assignments. Slot zero is always the immutable Off command.</summary>
    public static class PortalWeaponLoadout
    {
        public const int InitialSlotCount = 3;
        public const int MaximumSlotCount = 7;
        private static readonly string[] SlotResearch =
        {
            "science.weapon_slots.1", "science.weapon_slots.2",
            "science.weapon_slots.3", "science.weapon_slots.4"
        };
        private static GlobalStats researchStats;
        internal static GlobalStats ResearchStats => researchStats != null
            ? researchStats
            : researchStats = Resources.Load<GlobalStats>("Global/globalHexStats");
        public static int SlotCount
        {
            get
            {
                var stats = ResearchStats;
                int count = InitialSlotCount;

                foreach (string research in SlotResearch)
                {
                    if (stats == null || !stats.HasUpgrade(research)) break;
                    count++;
                }
                return Mathf.Clamp(Mathf.Max(count, SaveSlotPrefs.GetInt(Prefix + "capacity")), InitialSlotCount, MaximumSlotCount);
            }
        }
        internal const int SlotPrice = 10;
        internal static ResourceType SlotResource => ResourceCatalog.Find("MagicOre");
        internal static bool CanAffordSlot => SlotResource != null &&
            GlobalResourceManager.Instance?.GetResourceAmount(SlotResource) >= SlotPrice;

        internal static bool TryBuySlot(int index)
        {
            var ore = SlotResource;
            var resources = GlobalResourceManager.Instance;
            if (index != SlotCount || index >= MaximumSlotCount ||
                ore == null || resources == null) return false;
            using (SaveSlotPrefs.Batch())
            {
                if (!resources.TrySpendResource(ore, SlotPrice)) return false;
                SaveSlotPrefs.SetInt(Prefix + "capacity", index + 1);
                GameSaveService.CommitArsenalUnlock(SaveSlotPrefs.SelectedSlot, "Global_Resource_" + ore.Id, SlotPrice);
            }
            Changed?.Invoke();
            return true;
        }

        private const string Prefix = "foundation.arsenal.loadout.";
        public static event Action Changed;
        internal static void ResetAssignments()
        {
            for (int i = 1; i < MaximumSlotCount; i++) SaveSlotPrefs.DeleteKey(Prefix + i);
            SaveSlotPrefs.DeleteKey(Prefix + "start");
            SaveSlotPrefs.DeleteKey(Prefix + "capacity");
            Changed?.Invoke();
        }
        public static PortalTowerBalance.Weapon Get(int slot) =>
            slot > 0 && slot < SlotCount ? GetAssignedWeapon(slot) : PortalTowerBalance.Weapon.None;

        internal static PortalTowerBalance.Weapon GetAssignedWeapon(int slot)
        {
            if (slot == 0) return PortalTowerBalance.Weapon.None;
            var weapon = (PortalTowerBalance.Weapon)SaveSlotPrefs.GetInt(Prefix + slot, slot == 1 ? (int)PortalTowerBalance.Weapon.Bolts : 0);
            return Enum.IsDefined(typeof(PortalTowerBalance.Weapon), weapon) && PortalArsenalProgress.IsUnlocked(weapon) ? weapon : PortalTowerBalance.Weapon.None;
        }
        public static bool Contains(PortalTowerBalance.Weapon weapon)
        {
            if (weapon == PortalTowerBalance.Weapon.None) return true;
            int count = SlotCount;
            for (int i = 1; i < count; i++) if (GetAssignedWeapon(i) == weapon) return true;
            return false;
        }
        public static bool Assign(int slot, PortalTowerBalance.Weapon weapon, PortalArsenalCatalog catalog)
        {
            if (slot <= 0 || slot >= SlotCount || weapon == PortalTowerBalance.Weapon.None || catalog == null ||
                catalog.Find(weapon) == null || !PortalArsenalProgress.IsUnlocked(weapon)) return false;
            using (SaveSlotPrefs.Batch())
            {
                // Move an existing assignment, swapping with the destination rather than duplicating it.
                // Old saves may still have assignments in slots waiting for research.
                var previous = GetAssignedWeapon(slot);
                for (int i = 1; i < MaximumSlotCount; i++)
                    if (i != slot && GetAssignedWeapon(i) == weapon) SaveSlotPrefs.SetInt(Prefix + i, (int)previous);
                SaveSlotPrefs.SetInt(Prefix + slot, (int)weapon);
                SaveSlotPrefs.Save();
            }
            Changed?.Invoke();
            return true;
        }
        public static int StartingSlot
        {
            get
            {
                int slot = SaveSlotPrefs.GetInt(Prefix + "start", 1);
                if (slot < 0 || slot >= SlotCount) slot = 1;
                return slot == 0 || GetAssignedWeapon(slot) != PortalTowerBalance.Weapon.None ? slot : 0;
            }
        }
        public static void SetStartingSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount || (slot > 0 && Get(slot) == PortalTowerBalance.Weapon.None)) return;
            SaveSlotPrefs.SetInt(Prefix + "start", slot); SaveSlotPrefs.Save(); Changed?.Invoke();
        }
    }
}
