using System;
using System.Collections.Generic;
using GameFoundation.MetaProgression;
using Stat = GameFoundation.MetaProgression.CombatUpgradeCatalog.Stat;
using UnityEngine;

namespace GameFoundation.Combat
{
    public static class CombatChainLightning
    {
        /// <summary>Each hop rolls independently. Secondary hits cannot start another chain.</summary>
        public static int Resolve(Health initial, float damage, Transform attacker, Func<float> roll = null,
            float chance = CombatHitEffects.LightningJumpChance)
        {
            if (initial == null || damage <= 0) return 0;
            // Any nearest living target on the struck side is eligible, including ground and flying units.
            string tag = initial.CompareTag("Enemy1") ? "Enemy1" : initial.tag;
            var candidates = new List<Health>();
            CombatTargetRegistry.CollectHealth(tag, candidates);
            var visited = new HashSet<Health> { initial };
            Health previous = initial;
            int jumps = 0;
            var portal = PortalTowerProgression.FromAttack(attacker);
            // These run upgrades apply to both electrical weapons, including a sphere already in flight.
            const PortalTowerBalance.Weapon electric = PortalTowerBalance.Weapon.Lightning;
            bool firstGuaranteed = portal?.RunAdjustment(electric, Stat.Chain) > 0;
            chance = Mathf.Clamp01(chance + (portal?.RunAdjustment(electric, Stat.ChainChance) ?? 0) / 100f);
            float retention = Mathf.Clamp01(CombatHitEffects.LightningDamageFraction +
                (portal?.RunAdjustment(electric, Stat.ChainRetention) ?? 0) / 100f);
            int limit = CombatHitEffects.MaximumLightningJumps + (portal?.RunAdjustment(electric, Stat.ChainLimit) ?? 0);
            for (int i = 0; i < limit; i++)
            {
                if (!(firstGuaranteed && i == 0) && !CombatHitEffects.Procs(roll != null ? roll() : UnityEngine.Random.value, chance)) break;
                Health nearest = null;
                float distance = float.PositiveInfinity;
                foreach (var target in candidates)
                {
                    if (target == null || target.IsDead || !target.isActiveAndEnabled || visited.Contains(target) ||
                        target.gameObject.scene != initial.gameObject.scene) continue;
                    float d = (target.transform.position - previous.transform.position).sqrMagnitude;
                    if (d < distance) { distance = d; nearest = target; }
                }
                if (nearest == null) break;
                damage = CombatHitEffects.NextLightningDamage(damage, retention);
                CombatStatusVfx.LightningArc(previous, nearest, initial.gameObject.scene, i * .065f);
                nearest.TakeChainLightningDamage(damage, attacker);
                CombatStatusVfx.Attach(nearest).ElectricBurst();
                visited.Add(nearest);
                previous = nearest;
                jumps++;
            }
            return jumps;
        }
    }
}
