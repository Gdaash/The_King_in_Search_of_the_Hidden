using UnityEngine;

namespace GameFoundation.Bestiary
{
    public enum EnemyMovementType { Ground = 0, Flying = 1 }

    [DisallowMultipleComponent]
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField, Tooltip("Постоянный ключ бестиария. Не меняйте после выпуска игры.")]
        private string persistentId;
        public string Id => persistentId;
        [SerializeField, Tooltip("Тип передвижения для выбора целей оружием. Наземный – значение по умолчанию.")]
        private EnemyMovementType movementType;
        public EnemyMovementType MovementType => movementType;
        public bool IsFlying => movementType == EnemyMovementType.Flying;
        [SerializeField, Min(1), Tooltip("Порядок опасности типа врага: 1 – самый слабый.")]
        private int dangerRank = 1;
        public int DangerRank => dangerRank;
    }
}
