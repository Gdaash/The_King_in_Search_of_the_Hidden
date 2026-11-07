using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Combat
{
    public static class CombatChainLightning
    {
        /// <summary>Each hop rolls independently. Secondary hits cannot start another chain.</summary>
        public static int Resolve(Health initial, float damage, Transform attacker, Func<float> roll = null)
        {
            if (initial == null || damage <= 0) return 0;
            // Any nearest living target on the struck side is eligible, including ground and flying units.
            string tag = initial.CompareTag("Enemy1") ? "Enemy1" : initial.tag;
            var candidates = new List<Health>();
            CombatTargetRegistry.CollectHealth(tag, candidates);
            var visited = new HashSet<Health> { initial };
            Health previous = initial;
            int jumps = 0;
            for (int i = 0; i < CombatHitEffects.MaximumLightningJumps; i++)
            {
                if (!CombatHitEffects.Procs(roll != null ? roll() : UnityEngine.Random.value, CombatHitEffects.LightningJumpChance)) break;
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
                damage = CombatHitEffects.NextLightningDamage(damage);
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
