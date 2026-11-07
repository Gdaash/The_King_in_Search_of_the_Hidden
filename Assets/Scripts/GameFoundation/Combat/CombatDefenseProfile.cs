using UnityEngine;
namespace GameFoundation.Combat
{
    [CreateAssetMenu(menuName="Game/Combat/Defense Profile")]
    public sealed class CombatDefenseProfile : ScriptableObject
    {
        [HideInInspector, Range(0,3)] public float physicalMultiplier=1;
        [Tooltip("Applies to Magic, Fire, Ice and Electric.")]
        [HideInInspector, Range(0,3)] public float magicalMultiplier=1;
        [Tooltip("Additional multiplier for Magic only; does not affect elemental damage.")]
        [HideInInspector, Range(0,3)] public float magicOnlyMultiplier=1;
        [HideInInspector, Range(0,3)] public float fireMultiplier=1,iceMultiplier=1,electricMultiplier=1;
        // Serialized profiles remain readable, but elemental resistance no longer changes damage.
        public float Multiplier(DamageType type) => 1f;
    }
    public static class CombatDamage
    {
        public static float Calculate(float damage,DamageType type,float armor,float multiplier) =>
            Calculate(damage, armor);

        public static float Calculate(float damage, float armor) => Mathf.Max(0, damage - Mathf.Max(0, armor));
    }
}
