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
        public CombatDefenseProfile defense;
        [Header("Upgrade modifiers only")]
        public GlobalStats modifiers;
        public float HealthBeforeLevel => modifiers!=null?modifiers.ApplyHealthModifiers(baseHealth):baseHealth;
        public float SpeedBeforeLevel => Mathf.Max(.1f,baseSpeed+(modifiers!=null?modifiers.bonusSpeed:0));
        public float DamageBeforeLevel => weapon!=null?Mathf.Max(0,weapon.damage+(modifiers!=null?modifiers.damageSettings.Find(d=>d.type==weapon.damageType)?.bonusDamage??0:0)):0;
        public float IntervalBeforeLevel => Mathf.Max(.05f,(weapon!=null?weapon.interval:1.5f)-(modifiers!=null?modifiers.bonusAttackSpeed:0));
        public float RangeBeforeLevel => Mathf.Max(0,(weapon!=null?weapon.range:0)+(modifiers!=null?modifiers.bonusAttackRange:0));
        public float Damage => DamageBeforeLevel*MilitaryExperience.Multiplier(this);
        public float Interval => IntervalBeforeLevel/MilitaryExperience.Multiplier(this);
        public float Range => RangeBeforeLevel*MilitaryExperience.Multiplier(this);
        public float ArmorForLevel(int level) => Mathf.Max(0,baseArmor*(1+(Mathf.Clamp(level,1,50)-1)*.025f)+(modifiers!=null?modifiers.bonusArmor:0));
        public float Armor => ArmorForLevel(GetComponent<EnemyLevel>()?.Level??1);
        public float ResistanceMultiplier(DamageType type)
        {
            var modifier=modifiers!=null?modifiers.resistances.Find(r=>r.type==type):null;
            return Mathf.Clamp((defense!=null?defense.Multiplier(type):1)-(modifier?.bonusResist??0),0,3);
        }
        public void Hit(Health target)
        {
            if(target!=null && weapon!=null)target.TakeDamage(Damage,weapon.damageType,transform);
        }
    }
}
