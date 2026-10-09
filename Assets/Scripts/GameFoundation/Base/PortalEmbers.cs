using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.Base
{
    public sealed class PortalEmbers : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer portal;
        [SerializeField] private Sprite[] fragments;
        [SerializeField] private Light2D portalLight;
        [Header("Vortex")]
        [SerializeField, Min(0)] private float rate = 36;
        [SerializeField] private Vector2 lifetime = new(2.8f, 3.6f);
        [SerializeField, Range(16, 256)] private int maximumParticles = 128;
        [SerializeField, Range(1, 6)] private int spiralArms = 3;
        [SerializeField, Tooltip("Degrees per second. Negative values reverse the vortex.")]
        private float orbitSpeed = 120;
        [SerializeField, Range(.1f, 1)] private float innerRadius = .68f;
        [SerializeField, Range(0, .15f)] private float turbulence = .035f;
        private sealed class Ember
        {
            public SpriteRenderer sprite;
            public float age, life, angle, speed, phase, radialOffset;
        }
        private Ember[] pool;
        private float elapsed, pending;
        private int nextParticle, nextArm;
        private bool warmed;

        private void Awake()
        {
            pool = new Ember[Mathf.Clamp(maximumParticles, 16, 256)];
            for (int i = 0; i < pool.Length; i++)
            {
                var child = new GameObject("Portal vortex particle"); child.transform.SetParent(transform, false);
                child.layer = gameObject.layer;
                var sr = child.AddComponent<SpriteRenderer>(); sr.enabled = false;
                if (portal != null) { sr.sortingLayerID = portal.sortingLayerID; sr.sortingOrder = portal.sortingOrder + 1; }
                pool[i] = new Ember { sprite = sr };
            }
        }

        private void OnEnable() { elapsed = pending = 0; nextParticle = nextArm = 0; warmed = false; }
        private void OnDisable() => Clear();
        private void Clear()
        {
            if (pool != null) foreach (var p in pool) p.sprite.enabled = false;
            warmed = false;
            pending = 0;
        }

        // Read the light after its Update flicker so both effects share this frame's centre and radius.
        private void LateUpdate()
        {
            if (pool == null) return;
            if (portal == null || !portal.enabled || !portal.gameObject.activeInHierarchy || portalLight == null || !portalLight.isActiveAndEnabled ||
                fragments == null || fragments.Length == 0)
            {
                if (warmed) Clear();
                return;
            }
            float dt = Time.deltaTime;
            elapsed += dt;
            if (!warmed)
            {
                // Populate every part of the spiral immediately, including after the prefab is re-enabled.
                int count = Mathf.Min(pool.Length, Mathf.CeilToInt(rate * (lifetime.x + lifetime.y) * .5f));
                for (int i = 0; i < count; i++) Spawn(pool[i], true);
                nextParticle = count % pool.Length;
                warmed = true;
            }
            foreach (var p in pool)
            {
                if (!p.sprite.enabled) continue;
                p.age += dt;
                if (p.age >= p.life) { p.sprite.enabled = false; continue; }
                Draw(p);
            }
            pending = Mathf.Min(pool.Length, pending + Mathf.Max(0, rate) * dt);
            int births = Mathf.FloorToInt(pending);
            pending -= births;
            for (int i = 0; i < pool.Length && births > 0; i++)
            {
                var p = pool[nextParticle];
                nextParticle = (nextParticle + 1) % pool.Length;
                if (p.sprite.enabled) continue;
                Spawn(p, false);
                births--;
            }
        }

        private void Spawn(Ember p, bool prewarm)
        {
            p.life = Random.Range(Mathf.Max(.1f, lifetime.x), Mathf.Max(.1f, lifetime.y));
            p.age = prewarm ? Random.value * p.life : 0;
            p.angle = (elapsed - p.age) * orbitSpeed * .45f + nextArm * (360f / Mathf.Max(1, spiralArms)) + Random.Range(-7f, 7f);
            nextArm = (nextArm + 1) % Mathf.Max(1, spiralArms);
            p.speed = orbitSpeed * Random.Range(.92f, 1.08f);
            p.phase = Random.value * Mathf.PI * 2;
            p.radialOffset = Random.Range(-.025f, .025f);
            p.sprite.sprite = fragments[Random.Range(0, fragments.Length)];
            p.sprite.enabled = p.sprite.sprite != null;
            Draw(p);
        }

        private void Draw(Ember p)
        {
            float progress = p.age / p.life;
            float angle = (p.angle + p.age * p.speed) * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(1, innerRadius, progress) + p.radialOffset + Mathf.Sin(p.age * 3 + p.phase) * turbulence;
            radius = Mathf.Clamp(radius, .1f, 1) * portalLight.pointLightOuterRadius;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            p.sprite.transform.position = portalLight.transform.TransformPoint(offset);
            p.sprite.sortingOrder = portal.sortingOrder + (offset.y > 0 ? -1 : 1);
            float fade = Mathf.SmoothStep(0, 1, progress / .12f) * Mathf.SmoothStep(0, 1, (1 - progress) / .22f);
            float shimmer = .8f + .2f * Mathf.Sin(p.age * 7 + p.phase);
            p.sprite.color = new Color(1, 1, 1, fade * shimmer);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (portalLight == null) return;
            Gizmos.matrix = portalLight.transform.localToWorldMatrix;
            Gizmos.color = new Color(1, .8f, .25f, .6f);
            Gizmos.DrawWireSphere(Vector3.zero, portalLight.pointLightOuterRadius);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
