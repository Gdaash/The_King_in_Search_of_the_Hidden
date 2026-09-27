using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Health))]
    public sealed class WorldMilitaryLowHealthRetreat : MonoBehaviour
    {
        private WorldMilitaryDeploymentController deployment;
        private GlobalStats scientificStats;
        private Health health;
        private bool retreatRequested;

        public void Initialize(WorldMilitaryDeploymentController owner, GlobalStats progressStats)
        {
            deployment = owner;
            scientificStats = progressStats;
            health = GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthChanged.RemoveListener(OnHealthChanged);
                health.OnHealthChanged.AddListener(OnHealthChanged);
                CheckHealth(health.NormalizedHealth);
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.OnHealthChanged.RemoveListener(OnHealthChanged);
        }

        private void OnHealthChanged(float normalizedHealth) => CheckHealth(normalizedHealth);

        private void CheckHealth(float normalizedHealth)
        {
            if (retreatRequested || deployment == null || scientificStats == null) return;
            float threshold = scientificStats.WarriorRetreatHealthThreshold;
            if (threshold <= 0f || normalizedHealth > threshold) return;
            retreatRequested = deployment.RequestLowHealthRetreat(gameObject);
        }
    }
}
