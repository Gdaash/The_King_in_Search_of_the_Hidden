using System.Collections.Generic;
using System.Globalization;
using GameFoundation.Combat;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    /// <summary>Read-only presentation of the same values used by tower attacks.</summary>
    public static class PortalWeaponDescription
    {
        public static List<(string key, string label, string value)> Stats(PortalTowerProgression p, PortalTowerBalance.WeaponDefinition w, ArcherTower tower)
        {
            var result = new List<(string key, string label, string value)>();
            void Add(string key, string fallback, float value, string suffix = "") => result.Add((key, PortalArsenalPopup.T("stat." + key, fallback), value.ToString("0.##", CultureInfo.InvariantCulture) + suffix));
            var stats = tower != null ? tower.GetStats() : null;
            if (w.IsSupport)
            {
                if (w.weapon == PortalTowerBalance.Weapon.HealingRay) Add("healing", "Лечение", w.supportAmount);
                if (w.weapon == PortalTowerBalance.Weapon.MagicShield) Add("shield", "Прочность щита", w.supportAmount);
                if (w.weapon == PortalTowerBalance.Weapon.DefenseField)
                {
                    Add("armor", "Дополнительная броня", w.supportAmount);
                    Add("radius", "Радиус области", w.radius);
                    Add("range", "Дальность", w.range);
                    Add("energyPerSecond", "Энергия в секунду", w.energyPerSecond);
                    return result;
                }
                Add("range", "Дальность", w.range);
                Add("supportInterval", "Интервал применения, с", w.cooldown);
                Add("supportEnergy", "Энергия за применение", Mathf.Max(1, w.energyPerShot));
                return result;
            }
            Add("damage", "Урон", (w.damage + (stats?.UnifiedBonusDamage ?? 0)) * (p != null ? p.WeaponDamageMultiplier(w.weapon) : w.BaseDamageScale(true)));
            Add("range", "Дальность", Mathf.Max(0, w.range + (stats?.bonusAttackRange ?? 0) + (p?.AddedRange(w.weapon) ?? 0)));
            if (!w.continuous) Add("interval", "Интервал атак", (p != null ? p.AttackInterval(w.weapon, stats?.bonusAttackSpeed ?? 0) : Mathf.Max(PortalTowerBalance.MinimumInterval, w.cooldown - (stats?.bonusAttackSpeed ?? 0))));
            if (w.HasTicks) Add("tick", "Интервал урона", (p != null ? p.TickInterval(w.weapon) : Mathf.Max(1, w.damageTickInterval)));
            if (w.HasDuration) Add("duration", "Длительность области", w.duration * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.Duration) ?? 1));
            if (!w.continuous) Add(w.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit ? "targets" : w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile ? "projectiles" : "areas", w.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit ? "Целей за атаку" : "Объектов за атаку", (p?.Count(w.weapon) ?? Mathf.Max(1, w.count)));
            if (w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Area)
            {
                float radius = w.radius * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.Area) ?? 1);
                Add(w.weapon == PortalTowerBalance.Weapon.Rings ? "ringWidth" : "radius", w.weapon == PortalTowerBalance.Weapon.Rings ? "Ширина кольца" : "Радиус области", w.weapon == PortalTowerBalance.Weapon.Rings ? radius * 2 : radius);
                if (w.weapon == PortalTowerBalance.Weapon.Rings) Add("speed", "Скорость распространения", w.expansionSpeed * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.ExpansionSpeed) ?? 1));
            }
            if (w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile)
            {
                var projectile = w.projectilePrefab != null ? w.projectilePrefab.GetComponent<EnemyProjectile>() : null;
                float speed = projectile != null ? projectile.BaseSpeed : tower != null ? tower.GetComponent<TowerVisuals>()?.ProjectileBaseSpeed ?? 0 : 0;
                if (speed > 0) Add("projectileSpeed", "Скорость снаряда", speed * w.projectileSpeedMultiplier * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.ExpansionSpeed) ?? 1));
                if (w.spreadAngle > 0) Add("spread", "Угол разброса", w.spreadAngle, "°");
                if (w.combat != null && w.combat.targetMode == CombatWeapon.TargetMode.Area && w.projectilePrefab != null)
                {
                    var circle = w.projectilePrefab.GetComponentInChildren<CircleCollider2D>();
                    if (circle != null) Add("impactRadius", "Радиус попадания", circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y)) * w.projectileScale * 1.2f);
                }
            }
            if (w.slowFraction > 0) Add("areaSlow", "Замедление в области", Mathf.Min(.8f, w.slowFraction * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.SlowStrength) ?? 1)) * 100, "%");
            if (p == null || p.UsesEnergyLoadout) Add("energyCost", "Энергия за выстрел", Mathf.Max(1, w.energyPerShot));
            switch (w.damageType)
            {
                case DamageType.Fire:
                    Add("burnChance", "Шанс поджога", CombatHitEffects.IgniteChance * 100, "%");
                    Add("burnDuration", "Горение, секунды", CombatHitEffects.BurnSeconds);
                    Add("burnDamage", "Урон горения от попадания", CombatHitEffects.BurnDamageFraction * 100, "%");
                    Add("burnTick", "Интервал горения", CombatHitEffects.BurnTickSeconds); break;
                case DamageType.Ice:
                    Add("chill", "Замедление от попадания", CombatHitEffects.ChillFraction * 100, "%");
                    Add("chillDuration", "Замедление, секунды", CombatHitEffects.ChillSeconds); break;
                case DamageType.Physical:
                    Add("stunChance", "Шанс оглушения", CombatHitEffects.StunChance * 100, "%");
                    Add("stunDuration", "Оглушение, секунды", CombatHitEffects.StunSeconds); break;
                case DamageType.Electric:
                    Add("chainChance", "Шанс перескока", CombatHitEffects.LightningJumpChance * 100, "%");
                    Add("chainCount", "Максимум перескоков", CombatHitEffects.MaximumLightningJumps);
                    Add("chainDamage", "Урон следующего перескока", CombatHitEffects.LightningDamageFraction * 100, "%"); break;
                default:
                    Add("echoChance", "Шанс эха", CombatHitEffects.EchoChance * 100, "%");
                    Add("echoDamage", "Урон эха от попадания", CombatHitEffects.EchoDamageFraction * 100, "%");
                    Add("echoDelay", "Задержка эха, секунды", CombatHitEffects.EchoDelaySeconds); break;
            }
            return result;
        }
    }
}
