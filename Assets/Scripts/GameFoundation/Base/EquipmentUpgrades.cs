using System;
using UnityEngine;
using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;

namespace GameFoundation.Base
{
    public enum EquipmentKind { None, Sword, Bow, Cart, Spear, LeatherArmor, IronArmor }

    /// <summary>Permanent, save-slot scoped equipment improvements shared by every unit.</summary>
    public static class EquipmentUpgrades
    {
        public static event Action Changed;
        internal static void ResetBlacksmithUpgrades()
        {
            foreach (var kind in new[] { EquipmentKind.Sword, EquipmentKind.Bow, EquipmentKind.Cart, EquipmentKind.Spear, EquipmentKind.LeatherArmor, EquipmentKind.IronArmor })
                PlayerPrefs.SetInt(Key(kind), 0);
            Changed?.Invoke();
        }
        public static string Key(EquipmentKind kind) => "blacksmith.upgrade." + kind;
        public static int Maximum(EquipmentKind kind) => kind == EquipmentKind.None ? 0 : kind == EquipmentKind.Cart ? 5 : 10;
        public static int Level(EquipmentKind kind) => Mathf.Clamp(PlayerPrefs.GetInt(Key(kind), 0), 0, Maximum(kind));
        public static int PercentPerLevel(EquipmentKind kind) => kind == EquipmentKind.LeatherArmor ? 5 : 10;
        public static float Multiplier(EquipmentKind kind) => 1 + PercentPerLevel(kind) * .01f * Level(kind);
        public static int Cost(EquipmentKind kind) => kind switch {
            EquipmentKind.Sword or EquipmentKind.Spear => 5 + 3 * Level(kind),
            EquipmentKind.LeatherArmor or EquipmentKind.IronArmor => 2 + Level(kind), EquipmentKind.Bow => 10 + 5 * Level(kind),
            EquipmentKind.Cart => Level(kind) == 0 ? 5 : 10 + 10 * Level(kind), _ => 0 };
        public static bool CanBuy(EquipmentKind kind, ResourceType resource) => kind != EquipmentKind.None && Level(kind) < Maximum(kind)
            && resource != null && GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(resource) >= Cost(kind);
        public static bool TryBuy(EquipmentKind kind, ResourceType resource)
        {
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            if (!CanBuy(kind, resource)) return false;
            int next = Level(kind) + 1;
            if (!GlobalResourceManager.Instance.TrySpendResource(resource, Cost(kind))) return false;
            PlayerPrefs.SetInt(Key(kind), next); PlayerPrefs.Save();
            Changed?.Invoke();
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.ContentUnlock);
            return true;
        }
    }
}
