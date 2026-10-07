using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>Spawn-time rank. Enemies never earn military experience.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLevel : MonoBehaviour
    {
        private bool initialized;
        public int Level { get; private set; } = 1;
        public float StatMultiplier => MultiplierForLevel(Level);
        public static float MultiplierForLevel(int level) => 1f + (Mathf.Clamp(level, 1, 50) - 1) * .1f;

        private void Awake() => Initialize(AlarmSystem.Instance != null ? AlarmSystem.Instance.EnemyLevel : 1);

        public void Initialize(int level)
        {
            if (initialized) return;
            initialized = true;
            Level = Mathf.Clamp(level, 1, 50);
            if (TryGetComponent<Health>(out var health)) health.SetNormalizedHealth(1f);
        }
    }
}
