using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>One-shot construction shake and material fragments for the world building sprites.</summary>
    public sealed class BuildingConstructionEffect : MonoBehaviour
    {
        private sealed class Particle
        {
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float age, delay, lifetime, spin;
            public bool dust;
        }

        [SerializeField] private BuildingConstructionEffectLibrary settings;
        [SerializeField] private SpriteRenderer[] buildingSprites;
        [SerializeField] private ResourceType[] constructionMaterials;

        private readonly List<Particle> particles = new List<Particle>(24);
        private SpriteRenderer[] shakeTargets;
        private Vector3[] originalPositions;
        private Quaternion[] originalRotations;
        private float shakeElapsed;
        private float shakeSeed;
        private bool shaking;

        public void Play()
        {
            if (settings == null) return;
            ClearParticles();
            RestoreBuildingPose();
            shaking = false;
            shakeElapsed = 0f;
            CacheShakeTargets();
            if (shakeTargets.Length == 0) return;

            shaking = true;
            shakeSeed = Random.Range(0f, 100f);
            if (constructionMaterials != null)
                foreach (var resource in constructionMaterials)
                    EmitFragments(resource, settings.FragmentsPerMaterial);
            for (int i = 0; i < settings.DustParticleCount; i++) EmitDust();
        }

        private void Update()
        {
            if (settings == null) return;
            float dt = Time.unscaledDeltaTime;
            if (shaking)
            {
                shakeElapsed += dt;
                float progress = Mathf.Clamp01(shakeElapsed / Mathf.Max(0.05f, settings.ShakeDuration));
                if (progress >= 1f)
                {
                    RestoreBuildingPose();
                    shaking = false;
                }
                else
                {
                    float envelope = (1f - progress) * (1f - progress);
                    float x = (Mathf.PerlinNoise(shakeSeed, shakeElapsed * 42f) * 2f - 1f) * settings.ShakeDistance * envelope;
                    float y = (Mathf.PerlinNoise(shakeSeed + 11.7f, shakeElapsed * 49f) * 2f - 1f) * settings.ShakeDistance * envelope;
                    float angle = (Mathf.PerlinNoise(shakeSeed + 23.4f, shakeElapsed * 46f) * 2f - 1f) * settings.ShakeAngle * envelope;
                    var offset = new Vector3(x, y, 0f);
                    for (int i = 0; i < shakeTargets.Length; i++)
                    {
                        if (shakeTargets[i] == null) continue;
                        shakeTargets[i].transform.position = originalPositions[i] + offset;
                        shakeTargets[i].transform.rotation = originalRotations[i] * Quaternion.Euler(0f, 0f, angle);
                    }
                }
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                if (particle.renderer == null)
                {
                    particles.RemoveAt(i);
                    continue;
                }
                particle.age += dt;
                float elapsed = particle.age - particle.delay;
                if (elapsed < 0f) continue;
                particle.renderer.enabled = true;
                if (elapsed >= particle.lifetime)
                {
                    Destroy(particle.renderer.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }

                if (particle.dust)
                    particle.velocity *= Mathf.Exp(-settings.DustDrag * dt);
                else
                    particle.velocity.y -= settings.Gravity * dt;
                particle.renderer.transform.position += (Vector3)(particle.velocity * dt);
                particle.renderer.transform.Rotate(0f, 0f, particle.spin * dt, Space.Self);
                float fadeStart = particle.dust ? 0.28f : 0.62f;
                float fade = 1f - Mathf.InverseLerp(particle.lifetime * fadeStart, particle.lifetime, elapsed);
                var color = particle.renderer.color;
                color.a = (particle.dust ? 0.72f : 1f) * fade;
                particle.renderer.color = color;
            }
        }

        private void CacheShakeTargets()
        {
            int count = 0;
            if (buildingSprites != null)
                foreach (var sprite in buildingSprites)
                    if (sprite != null) count++;
            shakeTargets = new SpriteRenderer[count];
            originalPositions = new Vector3[count];
            originalRotations = new Quaternion[count];
            int index = 0;
            if (buildingSprites == null) return;
            foreach (var sprite in buildingSprites)
            {
                if (sprite == null) continue;
                shakeTargets[index] = sprite;
                originalPositions[index] = sprite.transform.position;
                originalRotations[index] = sprite.transform.rotation;
                index++;
            }
        }

        private void EmitFragments(ResourceType resource, int count)
        {
            if (settings == null || resource == null) return;
            Sprite[] fragments = settings.FragmentsFor(resource);
            if (fragments == null || fragments.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer source = RandomSource();
                if (source == null) return;
                Sprite sprite = fragments[Random.Range(0, fragments.Length)];
                if (sprite != null) CreateParticle(source, sprite, false);
            }
        }

        private void EmitDust()
        {
            SpriteRenderer source = RandomSource();
            if (source == null) return;
            Sprite sprite = settings.RandomDust();
            if (sprite != null) CreateParticle(source, sprite, true);
        }

        private SpriteRenderer RandomSource()
        {
            if (buildingSprites == null) return null;
            int validCount = 0;
            foreach (var sprite in buildingSprites)
                if (sprite != null && sprite.enabled && sprite.sprite != null && sprite.gameObject.activeInHierarchy) validCount++;
            if (validCount == 0) return null;
            int selected = Random.Range(0, validCount);
            foreach (var sprite in buildingSprites)
                if (sprite != null && sprite.enabled && sprite.sprite != null && sprite.gameObject.activeInHierarchy && selected-- == 0)
                    return sprite;
            return null;
        }

        private void CreateParticle(SpriteRenderer source, Sprite sprite, bool dust)
        {
            var particleObject = new GameObject(dust ? "Construction dust" : "Construction fragment", typeof(SpriteRenderer));
            var particleTransform = particleObject.transform;
            particleTransform.SetParent(source.transform, false);
            Bounds bounds = source.sprite.bounds;
            // Sprite bounds are already expressed relative to the sprite pivot.
            // Adding bounds.center again offsets particles away from sprites whose pivot is off-center.
            float x = Random.Range(0.12f, 0.88f);
            float y = Random.Range(0.12f, 0.88f);
            x = Mathf.Lerp(bounds.min.x, bounds.max.x, x);
            y = Mathf.Lerp(bounds.min.y, bounds.max.y, y);
            if (source.flipX) x = -x;
            if (source.flipY) y = -y;
            particleTransform.position = source.transform.TransformPoint(new Vector3(x, y, 0f));
            var renderer = particleObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + settings.SortingOrderOffset;
            renderer.color = Color.white;
            renderer.enabled = false;
            particles.Add(new Particle
            {
                renderer = renderer,
                velocity = dust
                    ? new Vector2(Random.Range(-settings.SidewaysSpeed * 0.55f, settings.SidewaysSpeed * 0.55f), Random.Range(0.28f, 0.55f))
                    : new Vector2(Random.Range(-settings.SidewaysSpeed, settings.SidewaysSpeed), Random.Range(settings.FragmentUpSpeed.x, settings.FragmentUpSpeed.y)),
                age = 0f,
                delay = Random.Range(0f, dust ? 0.38f : 0.3f),
                lifetime = dust ? Random.Range(settings.DustLifetime.x, settings.DustLifetime.y) : Random.Range(settings.FragmentLifetime.x, settings.FragmentLifetime.y),
                spin = dust ? 0f : Random.Range(-220f, 220f),
                dust = dust
            });
        }

        private void RestoreBuildingPose()
        {
            if (shakeTargets == null || originalPositions == null || originalRotations == null) return;
            int count = Mathf.Min(shakeTargets.Length, Mathf.Min(originalPositions.Length, originalRotations.Length));
            for (int i = 0; i < count; i++)
            {
                if (shakeTargets[i] == null) continue;
                shakeTargets[i].transform.position = originalPositions[i];
                shakeTargets[i].transform.rotation = originalRotations[i];
            }
        }

        private void ClearParticles()
        {
            foreach (var particle in particles)
                if (particle.renderer != null) Destroy(particle.renderer.gameObject);
            particles.Clear();
        }

        private void OnDisable()
        {
            RestoreBuildingPose();
            shaking = false;
            ClearParticles();
        }
    }
}
