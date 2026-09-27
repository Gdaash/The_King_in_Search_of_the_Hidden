using System.Collections;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Health))]
    public sealed class WorldMilitaryBaseRegen : MonoBehaviour
    {
        private Health health;
        private Transform portal;
        private GlobalStats scientificStats;
        private float radius;
        private Coroutine routine;

        public void Initialize(Transform portalTransform, GlobalStats progressStats, float baseRadius)
        {
            portal = portalTransform;
            scientificStats = progressStats;
            radius = Mathf.Max(0.1f, baseRadius);
            health = GetComponent<Health>();
            if (routine == null && isActiveAndEnabled) routine = StartCoroutine(Regenerate());
        }

        private void OnEnable()
        {
            if (routine == null && health != null) routine = StartCoroutine(Regenerate());
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator Regenerate()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                if (health == null || portal == null || scientificStats == null || health.IsDead) continue;
                float percent = scientificStats.WarriorBaseRegenPerSecond;
                if (percent <= 0f || Vector2.Distance(transform.position, portal.position) > radius) continue;
                health.RestoreHealth(health.MaxHealth * percent);
            }
        }
    }
}
