using UnityEngine;
namespace GameFoundation.Combat
{
    [CreateAssetMenu(menuName="Game/Combat/Defense Profile")]
    public sealed class CombatDefenseProfile : ScriptableObject
    {
        [Range(0,3)] public float physicalMultiplier=1;
        [Tooltip("Applies to Magic, Fire, Ice and Electric.")]
        [Range(0,3)] public float magicalMultiplier=1;
        [Range(0,3)] public float fireMultiplier=1,iceMultiplier=1,electricMultiplier=1;
        public float Multiplier(DamageType type) => type switch
        {
            DamageType.Physical=>physicalMultiplier,
            DamageType.Fire=>magicalMultiplier*fireMultiplier,
            DamageType.Ice=>magicalMultiplier*iceMultiplier,
            DamageType.Electric=>magicalMultiplier*electricMultiplier,
            _=>magicalMultiplier
        };
    }
    public static class CombatDamage
    {
        public static float Calculate(float damage,DamageType type,float armor,float multiplier) =>
            Mathf.Max(0,damage-(type==DamageType.Electric?0:Mathf.Max(0,armor)))*Mathf.Max(0,multiplier);
    }
}
