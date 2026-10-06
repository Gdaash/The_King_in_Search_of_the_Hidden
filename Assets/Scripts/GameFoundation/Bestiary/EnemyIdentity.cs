using UnityEngine;

namespace GameFoundation.Bestiary
{
    [DisallowMultipleComponent]
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField, Tooltip("Постоянный ключ бестиария. Не меняйте после выпуска игры.")]
        private string persistentId;
        public string Id => persistentId;
    }
}
