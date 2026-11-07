using UnityEngine;
using GameFoundation.MetaProgression;
namespace GameFoundation.Combat
{
    [DisallowMultipleComponent]
    public sealed class Combatant : MonoBehaviour
    {
        [Header("Base values owned by this prefab")]
        [Min(1)] public float baseHealth=100;
        [Min(0)] public float baseSpeed=1.5f;
        [Min(0)] public float baseArmor;
        public CombatWeapon weapon;
        [Header("Knockback resistance")]
        public UnitKnockback.Weight knockbackWeight = UnitKnockback.Weight.Medium;
        [HideInInspector] public CombatDefenseProfile defense;
        [Header("Upgrade modifiers only")]
        public GlobalStats modifiers;
        public GameFoundation.Base.EquipmentKind equipment;
        public float HealthBeforeLevel => modifiers!=null?modifiers.ApplyHealthModifiers(baseHealth):baseHealth;
        public float SpeedBeforeLevel => Mathf.Max(.1f,baseSpeed+(modifiers!=null?modifiers.bonusSpeed:0)) * (equipment == GameFoundation.Base.EquipmentKind.Cart ? GameFoundation.Base.EquipmentUpgrades.Multiplier(equipment) : 1);
        public float DamageBeforeLevel => (weapon!=null?Mathf.Max(0,weapon.damage+(modifiers!=null?modifiers.UnifiedBonusDamage:0)):0) *
            (equipment == GameFoundation.Base.EquipmentKind.Sword || equipment == GameFoundation.Base.EquipmentKind.Bow ? GameFoundation.Base.EquipmentUpgrades.Multiplier(equipment) : 1);
        public float IntervalBeforeLevel => Mathf.Max(.05f,(weapon!=null?weapon.interval:1.5f)-(modifiers!=null?modifiers.bonusAttackSpeed:0));
        public float RangeBeforeLevel => Mathf.Max(0,(weapon!=null?weapon.range:0)+(modifiers!=null?modifiers.bonusAttackRange:0));
        public float Damage => DamageBeforeLevel*MilitaryExperience.Multiplier(this);
        public float Interval => IntervalBeforeLevel/MilitaryExperience.Multiplier(this);
        public float Range => RangeBeforeLevel*MilitaryExperience.Multiplier(this);
        public float ArmorForLevel(int level) => Mathf.Max(0,baseArmor*(1+(Mathf.Clamp(level,1,50)-1)*.025f)+(modifiers!=null?modifiers.bonusArmor:0));
        public float Armor => ArmorForLevel(GetComponent<EnemyLevel>()?.Level??1);
        public float ResistanceMultiplier(DamageType type)
        {
            return 1f;
        }
        private readonly System.Collections.Generic.List<Health> attackTargets = new();
        public const float MeleeContactTolerance = .1f;
        public float MeleeHitRadius => Range + MeleeContactTolerance;
        public void Hit(Health target)
        {
            if(target==null || weapon==null)return;
            if (weapon.targetMode == CombatWeapon.TargetMode.Area)
            {
                Vector2 origin = transform.position;
                Vector2 forward = (Vector2)target.transform.position - origin;
                if (forward.sqrMagnitude < .0001f) forward = transform.localScale.x < 0 ? Vector2.left : Vector2.right;
                AttackFootprintVfx.Melee(this, forward, MeleeHitRadius, weapon.meleeArc);
                bool hostile = CompareTag("Enemy1");
                CombatTargetRegistry.CollectHealth(hostile ? "Player" : "Enemy1", attackTargets, hostile);
                // Snapshot collection before damage: deaths can unregister targets.
                foreach (var victim in attackTargets)
                {
                    if (victim == null || victim.IsDead || victim.gameObject == gameObject) continue;
                    Vector2 offset = (Vector2)victim.transform.position - origin;
                    var body = victim.GetComponent<Collider2D>();
                    Vector2 closest = body != null && body.enabled ? body.ClosestPoint(origin) : (Vector2)victim.transform.position;
                    var portal = hostile ? victim.GetComponentInParent<PortalMeleeBoundary>() : null;
                    bool inReach = portal != null ? portal.IsAtBoundary(origin) && (weapon.meleeArc >= 360 || Vector2.Angle(forward, offset) <= weapon.meleeArc * .5f) :
                        InsideMeleeArea(forward, offset, Vector2.Distance(origin, closest), Range, weapon.meleeArc);
                    if (!inReach) continue;
                    HitSingle(victim);
                }
                return;
            }
            HitSingle(target);
        }
        internal static bool InsideMeleeArea(Vector2 forward, Vector2 offset, float distance, float range, float arc)
            => distance <= range + MeleeContactTolerance && (arc >= 360 || Vector2.Angle(forward, offset) <= arc * .5f);
        private void HitSingle(Health target)
        {
            var body = target.GetComponent<Collider2D>();
            Vector2 contact = body != null && body.enabled ? body.ClosestPoint(transform.position) : (Vector2)target.transform.position;
            // Portal AI attacks the ground hex boundary, not the tower's small physical footprint.
            var portal = CompareTag("Enemy1") ? target.GetComponentInParent<PortalMeleeBoundary>() : null;
            if (portal != null ? !portal.IsAtBoundary(transform.position) : Vector2.Distance(transform.position, contact) > MeleeHitRadius) return;
            var spear=GetComponent<SpearmanTactics>();
            float before=target.CurrentHealth;
            target.TakeDamage(Damage,weapon.damageType,transform);
            if (target.CurrentHealth < before) CombatImpactBurst.Spawn(target.HitEffectPosition, DamagePalette.For(weapon.damageType));
            if(spear!=null && target.CurrentHealth<before)spear.OnHit(target);
        }
    }
}
