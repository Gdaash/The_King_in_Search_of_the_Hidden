using UnityEngine;
namespace GameFoundation.Combat
{
    [CreateAssetMenu(menuName="Game/Combat/Weapon")]
    public sealed class CombatWeapon : ScriptableObject
    {
        public enum TargetMode { SingleTarget, Area }
        [Tooltip("Одна цель или все цели в области удара/столкновения.")]
        public TargetMode targetMode;
        [Tooltip("Сектор ближнего удара: 180 – впереди, 360 – вокруг атакующего.")]
        [Range(1, 360)] public float meleeArc = 180;
        public string title,englishTitle;
        // Legacy serialized visual theme; it does not select a damage formula.
        [InspectorName("Attack effect and visual theme")] public DamageType damageType;
        [Min(0)] public float damage=20;
        [Min(.05f)] public float interval=1;
        [Min(0)] public float range=6;
        [Min(1)] public int count=1;
        [Min(0)] public float radius=.1f;
        [Min(.01f)] public float expansionSpeed=3;
        public float SingleTargetDps => damage*count/Mathf.Max(.05f,interval);
    }
}
