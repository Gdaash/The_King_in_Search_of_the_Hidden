using GameFoundation.Saves;

namespace GameFoundation.MetaProgression
{
    public enum PortalWeaponMode { Economical, Balanced, Overcharged }

    internal static class PortalWeaponProfiles
    {
        // Profiles are retained for a future iteration, but currently disabled.
        internal static bool Enabled => false;
        private const string Prefix = "foundation.arsenal.profile.";

        internal sealed class Profile
        {
            public Profile(float energy = 0, float damage = 1, float interval = 1,
                float area = 1, float duration = 1, float range = 1, float speed = 1,
                float support = 1, float chainChance = .15f, float spread = 1)
            {
                Energy = energy;
                Damage = damage;
                Interval = interval;
                Area = area;
                Duration = duration;
                Range = range;
                Speed = speed;
                Support = support;
                ChainChance = chainChance;
                Spread = spread;
            }

            public float Energy { get; }
            public float Damage { get; }
            public float Interval { get; }
            public float Area { get; }
            public float Duration { get; }
            public float Range { get; }
            public float Speed { get; }
            public float Support { get; }
            public float ChainChance { get; }
            public float Spread { get; }

            public float Cost(float baseline) => Energy > 0 ? Energy : baseline;
        }

        private static readonly Profile Balanced = new();
        private static readonly (Profile economical, Profile overcharged)[] Variants =
        {
            (Balanced, Balanced), // Off
            (new(1, damage: .85f, interval: 1.2f), new(2, damage: 1.12f, interval: .57f)), // Bolts
            (new(3, damage: .86f, area: .84f), new(5, damage: 1.16f, area: 1.18f)), // Beam
            (new(6, damage: .8f, range: .85f, speed: .85f), new(10, damage: 1.2f, range: 1.18f, speed: 1.35f)), // Rings
            (new(4, damage: .86f, chainChance: .09f), new(7, damage: 1.12f, chainChance: .30f)), // Lightning
            (new(4, damage: .84f, duration: .66f), new(7, duration: 1.55f)), // Frost Seal
            (new(1.4f, damage: .9f, spread: 1.15f), new(2.35f, damage: 1.25f, speed: 1.2f, spread: .8f)), // Fire Shot
            (new(8, damage: .84f, area: .82f), new(13, damage: 1.12f, area: 1.22f)), // Storm Orb
            (new(3, support: .7f, range: .9f), new(7, support: 1.35f, range: 1.2f)), // Healing Ray
            (new(6, support: .7f), new(12, support: 1.5f)), // Magic Shield
            (new(1.5f, support: .7f, area: .85f), new(2.75f, support: 1.35f, area: 1.2f)), // Defense Field
        };

        internal static Profile Get(PortalTowerBalance.Weapon weapon, PortalWeaponMode mode)
        {
            if (!Enabled) return Balanced;
            int index = (int)weapon;
            if (index < 0 || index >= Variants.Length) return Balanced;
            return mode switch
            {
                PortalWeaponMode.Economical => Variants[index].economical,
                PortalWeaponMode.Overcharged => Variants[index].overcharged,
                _ => Balanced
            };
        }

        internal static PortalWeaponMode Selected(PortalTowerBalance.Weapon weapon)
        {
            if (!Enabled) return PortalWeaponMode.Balanced;
            int saved = SaveSlotPrefs.GetInt(Prefix + weapon, (int)PortalWeaponMode.Balanced);
            return saved is 0 or 2 ? (PortalWeaponMode)saved : PortalWeaponMode.Balanced;
        }

        internal static void Select(PortalTowerBalance.Weapon weapon, PortalWeaponMode mode)
        {
            if (!Enabled) return;
            if (weapon == PortalTowerBalance.Weapon.None || !PortalArsenalProgress.IsUnlocked(weapon) || Selected(weapon) == mode) return;
            SaveSlotPrefs.SetInt(Prefix + weapon, (int)mode);
            SaveSlotPrefs.Save();
        }
    }
}
