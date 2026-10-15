using System;
using System.Collections.Generic;

namespace GameFoundation.MetaProgression
{
    /// <summary>Matches acquired ranks to weapon categories, including weapons acquired later.</summary>
    public sealed class PortalUpgradeModifiers
    {
        private readonly IEnumerable<PortalTowerBalance.Upgrade> upgrades;
        private readonly Func<string, int> rank;
        private readonly Func<PortalTowerBalance.Upgrade, float> acquiredValue;

        public PortalUpgradeModifiers(IEnumerable<PortalTowerBalance.Upgrade> upgrades, Func<string, int> rank, Func<PortalTowerBalance.Upgrade, float> acquiredValue = null)
        { this.upgrades = upgrades; this.rank = rank; this.acquiredValue = acquiredValue; }

        public static bool Applies(PortalTowerBalance.Upgrade upgrade, PortalTowerBalance.DeliveryMethod method, DamageType damage)
        {
            if (upgrade == null || upgrade.effect == PortalTowerBalance.Effect.UnlockWeapon || upgrade.effect == PortalTowerBalance.Effect.ExtraLight)
                return false;
            if (upgrade.effect == PortalTowerBalance.Effect.Projectiles && method != PortalTowerBalance.DeliveryMethod.Projectile) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.Area && method != PortalTowerBalance.DeliveryMethod.Area) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.Targets && method != PortalTowerBalance.DeliveryMethod.InstantHit) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.ExpansionSpeed && method != PortalTowerBalance.DeliveryMethod.Projectile) return false;
            if ((upgrade.effect == PortalTowerBalance.Effect.Duration || upgrade.effect == PortalTowerBalance.Effect.SlowStrength) && method != PortalTowerBalance.DeliveryMethod.Area) return false;
            return upgrade.scope switch
            {
                PortalTowerBalance.UpgradeScope.AllWeapons => true,
                PortalTowerBalance.UpgradeScope.DeliveryMethod => upgrade.deliveryMethod == method,
                PortalTowerBalance.UpgradeScope.DamageType => upgrade.damageType == damage,
                _ => false
            };
        }

        public static bool Applies(PortalTowerBalance.Upgrade upgrade, PortalTowerBalance.WeaponDefinition weapon)
        {
            if (weapon == null || upgrade == null) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.TickInterval && !weapon.HasTicks) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.AttackInterval && weapon.continuous) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.Duration && !weapon.HasDuration) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.SlowStrength && weapon.slowFraction <= 0) return false;
            return Applies(upgrade, weapon.deliveryMethod, weapon.damageType);
        }

        public float Total(PortalTowerBalance.Effect effect, PortalTowerBalance.WeaponDefinition weapon)
        {
            float value = 0;
            foreach (var upgrade in upgrades)
                if (upgrade.effect == effect && Applies(upgrade, weapon)) value += (acquiredValue != null ? acquiredValue(upgrade) : upgrade.value * rank(upgrade.id));
            return value;
        }

        public float Total(PortalTowerBalance.Effect effect, PortalTowerBalance.DeliveryMethod method, DamageType damage)
        {
            float value = 0;
            foreach (var upgrade in upgrades)
                if (upgrade.effect == effect && Applies(upgrade, method, damage)) value += (acquiredValue != null ? acquiredValue(upgrade) : upgrade.value * rank(upgrade.id));
            return value;
        }

        public float PortalTotal(PortalTowerBalance.Effect effect)
        {
            float value = 0;
            foreach (var upgrade in upgrades)
                if (upgrade.effect == effect && upgrade.scope == PortalTowerBalance.UpgradeScope.Portal)
                    value += (acquiredValue != null ? acquiredValue(upgrade) : upgrade.value * rank(upgrade.id));
            return value;
        }
    }
}
