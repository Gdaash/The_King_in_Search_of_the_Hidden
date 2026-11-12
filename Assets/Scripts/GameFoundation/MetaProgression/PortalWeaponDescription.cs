using System.Collections.Generic;
using System.Globalization;
using GameFoundation.Combat;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    /// <summary>Read-only presentation of the same values used by tower attacks.</summary>
    public static class PortalWeaponDescription
    {
        public static List<(string key, string label, string value, float amount)> Stats(PortalTowerProgression p,
            PortalTowerBalance.WeaponDefinition w, ArcherTower tower, PortalWeaponMode mode = PortalWeaponMode.Balanced)
        {
            var result = new List<(string key, string label, string value, float amount)>();
            void Add(string key, string fallback, float value, string suffix = "") => result.Add((key, PortalArsenalPopup.T("stat." + key, fallback),
                value.ToString(key == "damage" ? "0" : "0.##", CultureInfo.InvariantCulture) + suffix, value));
            var stats = tower != null ? tower.GetStats() : null;
            var profile = p != null ? p.Profile(w.weapon) : PortalWeaponProfiles.Get(w.weapon, mode);
            float interval = p != null ? p.AttackInterval(w.weapon, stats?.bonusAttackSpeed ?? 0) :
                Mathf.Max(PortalTowerBalance.MinimumInterval, w.cooldown * profile.Interval - (stats?.bonusAttackSpeed ?? 0));
            float range = p != null ? p.WeaponRange(w.weapon, stats?.bonusAttackRange ?? 0) :
                Mathf.Max(0, w.range * profile.Range + (stats?.bonusAttackRange ?? 0));
            float tick = p != null ? p.TickInterval(w.weapon) : Mathf.Max(1, w.damageTickInterval);
            float cost = p != null ? p.ShotEnergy(w.weapon) : profile.Cost(Mathf.Max(1, w.energyPerShot));
            int count = p?.Count(w.weapon) ?? Mathf.Max(1, w.count);
            float area = p?.Multiplier(w.weapon, PortalTowerBalance.Effect.Area) ?? profile.Area;
            float speedScale = p?.Multiplier(w.weapon, PortalTowerBalance.Effect.ExpansionSpeed) ?? profile.Speed;
            bool showEnergy = p == null || p.UsesEnergyLoadout;
            if (w.IsSupport)
            {
                float support = w.supportAmount * profile.Support;
                if (w.weapon == PortalTowerBalance.Weapon.HealingRay) Add("healing", "Лечение", support);
                if (w.weapon == PortalTowerBalance.Weapon.MagicShield) Add("shield", "Прочность щита", support);
                if (w.weapon == PortalTowerBalance.Weapon.DefenseField)
                {
                    float drain = profile.Cost(w.energyPerSecond) * (p?.RunMultiplier(w.weapon, CombatUpgradeCatalog.Stat.Drain) ?? 1);
                    Add("armor", "Дополнительная броня", support);
                    Add("radius", "Радиус области", w.radius * area);
                    Add("range", "Дальность", range);
                    Add("energyPerSecond", "Энергия в секунду", drain);
                    Add("energyPerMinute", "Энергия в минуту", drain * 60);
                    return result;
                }
                Add("range", "Дальность", range);
                Add("supportInterval", "Интервал применения, с", interval);
                Add("supportEnergy", "Энергия за применение", cost);
                Add("energyPerMinute", "Энергия в минуту", 60 * cost / interval);
                return result;
            }
            Add("damage", "Урон", (w.damage + (stats?.UnifiedBonusDamage ?? 0)) *
                (p != null ? p.WeaponDamageMultiplier(w.weapon) : w.BaseDamageScale(true) * profile.Damage));
            Add("range", "Дальность", range);
            if (!w.continuous) Add("interval", "Интервал атак", interval);
            if (w.HasTicks) Add("tick", "Интервал урона", tick);
            if (w.HasDuration) Add("duration", "Длительность области", w.duration * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.Duration) ?? profile.Duration));
            if (!w.continuous) Add(w.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit ? "targets" : w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile ? "projectiles" : "areas", w.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit ? "Целей за атаку" : "Объектов за атаку", count);
            if (w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Area)
            {
                float radius = w.radius * area;
                Add(w.weapon == PortalTowerBalance.Weapon.Rings ? "ringWidth" : "radius", w.weapon == PortalTowerBalance.Weapon.Rings ? "Ширина кольца" : "Радиус области", w.weapon == PortalTowerBalance.Weapon.Rings ? radius * 2 : radius);
                if (w.weapon == PortalTowerBalance.Weapon.Rings) Add("speed", "Скорость распространения", w.expansionSpeed * speedScale);
            }
            if (w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile)
            {
                var projectile = w.projectilePrefab != null ? w.projectilePrefab.GetComponent<EnemyProjectile>() : null;
                float speed = projectile != null ? projectile.BaseSpeed : tower != null ? tower.GetComponent<TowerVisuals>()?.ProjectileBaseSpeed ?? 0 : 0;
                if (speed > 0) Add("projectileSpeed", "Скорость снаряда", speed * w.projectileSpeedMultiplier * speedScale);
                if (w.spreadAngle > 0) Add("spread", "Угол разброса", w.spreadAngle * profile.Spread, "°");
                if (w.combat != null && w.combat.targetMode == CombatWeapon.TargetMode.Area && w.projectilePrefab != null)
                {
                    var circle = w.projectilePrefab.GetComponentInChildren<CircleCollider2D>();
                    if (circle != null) Add("impactRadius", "Радиус попадания", circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y)) * w.projectileScale * area * 1.2f);
                }
            }
            if (w.slowFraction > 0) Add("areaSlow", "Замедление в области", Mathf.Min(.8f, w.slowFraction * (p?.Multiplier(w.weapon, PortalTowerBalance.Effect.SlowStrength) ?? 1)) * 100, "%");
            if (showEnergy)
            {
                Add("energyCost", "Энергия за выстрел", cost);
                Add("energyPerMinute", "Энергия в минуту", w.continuous ? 60 * cost / tick : 60 * cost * count / interval);
            }
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
                    Add("chainChance", "Шанс перескока", profile.ChainChance * 100, "%");
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
