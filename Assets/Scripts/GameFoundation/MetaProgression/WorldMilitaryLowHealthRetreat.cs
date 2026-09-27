using UnityEngine;
using GameFoundation.Base;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Health))]
    public sealed class WorldMilitaryLowHealthRetreat : MonoBehaviour
    {
        private WorldMilitaryDeploymentController deployment;
        private Health health;
        private bool retreatRequested;

        public void Initialize(WorldMilitaryDeploymentController owner, GlobalStats progressStats)
        {
            deployment = owner;
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
            if (retreatRequested || deployment == null || !RoyalDecreeService.IsEnabled(RoyalDecreeService.CautiousWarriors)) return;
            if (normalizedHealth > 0.1f) return;
            retreatRequested = deployment.RequestLowHealthRetreat(gameObject);
        }
    }
}
