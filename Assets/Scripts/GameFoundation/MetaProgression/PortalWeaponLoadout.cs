using System;
using GameFoundation.Saves;
namespace GameFoundation.MetaProgression
{
    /// <summary>Slot-local assignments. Slot zero is always the immutable Off command.</summary>
    public static class PortalWeaponLoadout
    {
        public const int SlotCount = 5;
        private const string Prefix = "foundation.arsenal.loadout.";
        public static event Action Changed;
        internal static void ResetAssignments()
        {
            for (int i = 1; i < SlotCount; i++) SaveSlotPrefs.DeleteKey(Prefix + i);
            SaveSlotPrefs.DeleteKey(Prefix + "start");
            Changed?.Invoke();
        }
        public static PortalTowerBalance.Weapon Get(int slot)
        {
            if (slot <= 0 || slot >= SlotCount) return PortalTowerBalance.Weapon.None;
            var weapon = (PortalTowerBalance.Weapon)SaveSlotPrefs.GetInt(Prefix + slot, slot == 1 ? (int)PortalTowerBalance.Weapon.Bolts : 0);
            return Enum.IsDefined(typeof(PortalTowerBalance.Weapon), weapon) && PortalArsenalProgress.IsUnlocked(weapon) ? weapon : PortalTowerBalance.Weapon.None;
        }
        public static bool Contains(PortalTowerBalance.Weapon weapon)
        {
            if (weapon == PortalTowerBalance.Weapon.None) return true;
            for (int i = 1; i < SlotCount; i++) if (Get(i) == weapon) return true;
            return false;
        }
        public static bool Assign(int slot, PortalTowerBalance.Weapon weapon, PortalArsenalCatalog catalog)
        {
            if (slot <= 0 || slot >= SlotCount || weapon == PortalTowerBalance.Weapon.None || catalog == null ||
                catalog.Find(weapon) == null || !PortalArsenalProgress.IsUnlocked(weapon)) return false;
            using (SaveSlotPrefs.Batch())
            {
                // Move an existing assignment, swapping with the destination rather than duplicating it.
                var previous = Get(slot);
                for (int i = 1; i < SlotCount; i++)
                    if (i != slot && Get(i) == weapon) SaveSlotPrefs.SetInt(Prefix + i, (int)previous);
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
                int slot = UnityEngine.Mathf.Clamp(SaveSlotPrefs.GetInt(Prefix + "start", 1), 0, SlotCount - 1);
                return slot == 0 || Get(slot) != PortalTowerBalance.Weapon.None ? slot : 0;
            }
        }
        public static void SetStartingSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount || (slot > 0 && Get(slot) == PortalTowerBalance.Weapon.None)) return;
            SaveSlotPrefs.SetInt(Prefix + "start", slot); SaveSlotPrefs.Save(); Changed?.Invoke();
        }
    }
}
