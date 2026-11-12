using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    internal sealed class WorldCombatFeedback : MonoBehaviour
    {
        private struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector3 Origin;
            public Vector2 Drift;
            public float Size;
            public float Age;
            public float Lifetime;
            public bool Soul;
        }

        private readonly List<Particle> particles = new();
        private readonly Stack<SpriteRenderer> freeRenderers = new();
        private Camera worldCamera;
        private Camera shakenCamera;
        private Health portal;
        private Sprite stone, spirit, vignetteSprite;
        private Image vignette;
        private float shakeStrength, shakeRemaining, vignetteAge = 1f;

        public void Configure(Health tower)
        {
            portal = tower;
            worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
            if (shakenCamera != null) shakenCamera.ResetWorldToCameraMatrix();
            shakenCamera = null;
        }

        public void PortalHit(Transform attacker, float criticalThreshold, float warningThreshold)
        {
            if (portal == null) return;
            float hp = portal.NormalizedHealth;
            float strength = hp < criticalThreshold ? 5f : hp < warningThreshold ? 2.75f : 1.25f;
            shakeStrength = Mathf.Max(strength, shakeStrength * (shakeRemaining / .24f));
            shakeRemaining = .24f;

            Vector3 impact = portal.HitEffectPosition;
            Vector2 direction = attacker == null ? Vector2.up : (Vector2)(attacker.position - impact);
            if (portal.BodyRenderer != null && direction.sqrMagnitude > .0001f)
                impact = portal.BodyRenderer.bounds.ClosestPoint(attacker != null ? attacker.position : impact);
            if (stone == null) stone = CreateSprite(new[] { ".ll.", "lmmm", "mmdm", ".dd." }, false);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drift = Random.insideUnitCircle * .65f + direction.normalized * .35f;
                Spawn(stone, impact, drift, Random.Range(.75f, 1.25f), Random.Range(.32f, .54f), false);
            }
        }

        public void AllyDied(Vector3 position, float unitHeight)
        {
            if (vignette == null) CreateVignette();
            vignetteAge = 0f;
            if (spirit == null)
                spirit = CreateSprite(new[] { "..llll..", ".llllll.", "ll.ll.ll", "llllllll", "lll..lll", ".llllll.", "..llll..", "...ll...", "...l...." }, true);
            for (int i = 0; i < 19; i++)
            {
                float phase = i * 2.399963f;
                float radius = Mathf.Sqrt(i / 19f);
                Vector2 drift = new(Mathf.Cos(phase) * radius, Mathf.Sin(phase) * radius);
                Spawn(spirit, position, drift, Mathf.Max(.25f, unitHeight), Random.Range(.68f, .94f), true);
            }
        }

        private void Spawn(Sprite sprite, Vector3 position, Vector2 drift, float size, float lifetime, bool soul)
        {
            SpriteRenderer renderer;
            if (freeRenderers.Count > 0) renderer = freeRenderers.Pop();
            else
            {
                var visual = new GameObject("Combat feedback particle");
                SceneManager.MoveGameObjectToScene(visual, gameObject.scene);
                renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 100;
            }
            renderer.sprite = sprite;
            renderer.transform.SetPositionAndRotation(position, Quaternion.identity);
            renderer.transform.localScale = Vector3.one * (soul ? size * .65f : size);
            renderer.color = Color.clear;
            renderer.enabled = true;
            particles.Add(new Particle { Renderer = renderer, Origin = position, Drift = drift, Size = size, Lifetime = lifetime, Soul = soul });
        }

        private void Update()
        {
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            if (vignette != null && vignetteAge < 1f)
            {
                vignetteAge = Mathf.Min(1f, vignetteAge + Time.unscaledDeltaTime);
                float opacity = vignetteAge < .2f ? vignetteAge / .2f : (1f - vignetteAge) / .8f;
                vignette.color = new Color(.7f, .04f, .07f, opacity * .39f);
            }
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                particle.Age += Time.deltaTime;
                float t = Mathf.Clamp01(particle.Age / particle.Lifetime);
                var renderer = particle.Renderer;
                if (t >= 1f)
                {
                    renderer.enabled = false;
                    freeRenderers.Push(renderer);
                    particles[i] = particles[particles.Count - 1];
                    particles.RemoveAt(particles.Count - 1);
                    continue;
                }
                if (particle.Soul)
                {
                    float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * t), .85f);
                    Vector2 drift = particle.Drift;
                    float flutter = Mathf.Sin(t * 15f + drift.y * 12f) * .045f * envelope;
                    float taper = Mathf.Sqrt((drift.y + 1f) * .5f);
                    Vector2 offset = new(drift.x * .77f * taper * envelope + flutter, 1.8f * t + drift.y * .62f * envelope);
                    renderer.transform.position = particle.Origin + (Vector3)(offset * particle.Size);
                    renderer.color = new Color(1f, .99f, .95f, .65f * Mathf.Min(1f, t * 9f) * (1f - t * t));
                }
                else
                {
                    Vector2 offset = particle.Drift * t + Vector2.up * (1.35f * t - 1.65f * t * t);
                    renderer.transform.SetPositionAndRotation(particle.Origin + (Vector3)offset, Quaternion.Euler(0, 0, t * 300f * particle.Drift.x));
                    renderer.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - t) * 4f));
                }
                particles[i] = particle;
            }
        }

        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (shakeRemaining <= 0f) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (camera != worldCamera || !camera.orthographic) return;
            float pixelSize = 2f * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight);
            float strength = shakeStrength * (shakeRemaining / .24f) * pixelSize;
            float time = Time.unscaledTime * 91f;
            Vector3 offset = new(Mathf.Sin(time) * strength, Mathf.Cos(time * 1.23f) * strength, 0f);
            camera.worldToCameraMatrix = Matrix4x4.Translate(offset) * camera.worldToCameraMatrix;
            shakenCamera = camera;
        }

        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != shakenCamera) return;
            camera.ResetWorldToCameraMatrix();
            shakenCamera = null;
        }

        private void CreateVignette()
        {
            var parent = GetComponentInParent<Canvas>()?.rootCanvas.transform;
            if (parent == null) return;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float edge = Mathf.Max(Mathf.Abs(2f * x / (size - 1) - 1f), Mathf.Abs(2f * y / (size - 1) - 1f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (edge - .79f) / .21f)));
            }
            texture.Apply();
            vignetteSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
            var overlay = new GameObject("Ally loss vignette", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            vignette = overlay.GetComponent<Image>();
            vignette.sprite = vignetteSprite;
            vignette.raycastTarget = false;
            vignette.color = Color.clear;
            vignette.rectTransform.anchorMax = Vector2.one;
            vignette.rectTransform.anchorMin = Vector2.zero;
            vignette.rectTransform.offsetMax = vignette.rectTransform.offsetMin = Vector2.zero;
        }

        private static Sprite CreateSprite(string[] rows, bool soul)
        {
            int width = rows[0].Length;
            var texture = new Texture2D(width, rows.Length, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, rows.Length - 1 - y, rows[y][x] switch
                {
                    'l' => soul ? Color.white : new Color32(157, 146, 146, 255),
                    'm' => new Color32(112, 101, 106, 255),
                    'd' => new Color32(78, 67, 76, 255),
                    _ => Color.clear
                });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, rows.Length), Vector2.one * .5f, 32f);
        }

        private void OnDestroy()
        {
            foreach (var particle in particles)
                if (particle.Renderer != null) Destroy(particle.Renderer.gameObject);
            foreach (var renderer in freeRenderers)
                if (renderer != null) Destroy(renderer.gameObject);
            if (vignette != null) Destroy(vignette.gameObject);
            DestroySprite(stone);
            DestroySprite(spirit);
            DestroySprite(vignetteSprite);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
