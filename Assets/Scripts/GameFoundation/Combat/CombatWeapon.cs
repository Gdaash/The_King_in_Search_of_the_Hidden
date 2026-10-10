using UnityEngine;
namespace GameFoundation.Combat
{
    [CreateAssetMenu(menuName="Game/Combat/Weapon")]
    public sealed class CombatWeapon : ScriptableObject
    {
        public string title,englishTitle;
        public DamageType damageType;
        [Min(0)] public float damage=20;
        [Min(.05f)] public float interval=1;
        [Min(0)] public float range=6;
        [Min(1)] public int count=1;
        [Min(0)] public float radius=.1f;
        [Min(.01f)] public float expansionSpeed=3;
        public float SingleTargetDps => damage*count/Mathf.Max(.05f,interval);
    }
}
