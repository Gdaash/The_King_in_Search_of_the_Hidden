using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Combat
{
    /// <summary>Presentation of the already resolved hit geometry. Never applies damage.</summary>
    public sealed class AttackFootprintVfx : MonoBehaviour
    {
        [SerializeField, Min(.05f)] private float duration = .28f;
        [SerializeField] private Color swordColor = new(1f, .9f, .65f, .9f);
        [SerializeField] private Color circularColor = new(.68f, .3f, .9f, .85f);
        private SpriteRenderer footprint, wave;
        private float age;
        private Color tint;
        private bool circular;
        private static AttackFootprintVfx prefab;
        private static readonly Stack<AttackFootprintVfx> pool = new();
        private static readonly Dictionary<string, Sprite> sprites = new();
        public float Radius { get; private set; }
        public float Arc { get; private set; }
        public static AttackFootprintVfx LastSpawned { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (var sprite in sprites.Values)
                if (sprite != null) { Destroy(sprite.texture); Destroy(sprite); }
            sprites.Clear(); pool.Clear(); prefab = null; LastSpawned = null;
        }
        public static AttackFootprintVfx Melee(Combatant attacker, Vector2 forward, float radius, float arc)
        {
            var effect = Rent(attacker.gameObject.scene);
            effect.Radius = radius; effect.Arc = arc; effect.circular = arc >= 359.9f;
            effect.transform.SetPositionAndRotation(attacker.transform.position,
                Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg));
            effect.footprint.sprite = Shape(radius * 2, radius * 2, arc, false);
            effect.wave.sprite = effect.footprint.sprite;
            effect.tint = effect.circular ? effect.circularColor : effect.swordColor;
            var body = attacker.GetComponent<Health>()?.BodyRenderer;
            effect.SetSorting(body);
            effect.Present(); return effect;
        }
        public static void Contact(Collider2D collider, float expansion, DamageType theme)
        {
            if (!(collider is BoxCollider2D) && !(collider is CircleCollider2D)) return;
            var effect = Rent(collider.gameObject.scene);
            var scale = collider.transform.lossyScale;
            Vector2 localSize = collider is BoxCollider2D box ? box.size : Vector2.one * (((CircleCollider2D)collider).radius * 2);
            Vector2 size = new(Mathf.Abs(localSize.x * scale.x) * expansion, Mathf.Abs(localSize.y * scale.y) * expansion);
            effect.transform.SetPositionAndRotation(collider.transform.TransformPoint(collider.offset), collider.transform.rotation);
            effect.footprint.sprite = Shape(size.x, size.y, 360, collider is BoxCollider2D);
            effect.wave.sprite = null; effect.circular = false; effect.Radius = 0; effect.Arc = 0;
            effect.tint = DamagePalette.For(theme); effect.tint.a = .85f;
            effect.SetSorting(collider.GetComponentInChildren<SpriteRenderer>());
            effect.Present();
        }
        public static void ImpactCircle(Transform source, Vector2 center, float radius, DamageType theme)
        {
            var effect = Rent(source.gameObject.scene);
            effect.transform.SetPositionAndRotation(center, Quaternion.identity);
            effect.Radius = radius;
            effect.Arc = 360;
            effect.circular = true;
            effect.footprint.sprite = Shape(radius * 2, radius * 2, 360, false);
            effect.wave.sprite = effect.footprint.sprite;
            effect.tint = DamagePalette.For(theme);
            effect.tint.a = .85f;
            effect.SetSorting(source.GetComponentInChildren<SpriteRenderer>());
            effect.Present();
        }

        private static AttackFootprintVfx Rent(Scene scene)
        {
            AttackFootprintVfx result = null;
            while (pool.Count > 0 && result == null) result = pool.Pop();
            if (result == null)
            {
                if (prefab == null) prefab = Resources.Load<AttackFootprintVfx>("Effects/Attack Footprint");
                result = prefab != null ? Instantiate(prefab) : new GameObject("Attack Footprint").AddComponent<AttackFootprintVfx>();
                result.EnsureRenderers();
            }
            SceneManager.MoveGameObjectToScene(result.gameObject, scene);
            result.transform.localScale = Vector3.one; result.gameObject.SetActive(true);
            LastSpawned = result; return result;
        }
        private void EnsureRenderers()
        {
            footprint = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            var child = transform.Find("Wave");
            if (child == null) { child = new GameObject("Wave", typeof(SpriteRenderer)).transform; child.SetParent(transform, false); }
            wave = child.GetComponent<SpriteRenderer>();
        }
        private void SetSorting(SpriteRenderer body)
        {
            foreach (var renderer in new[] {footprint, wave})
            {
                renderer.sortingLayerID = body != null ? body.sortingLayerID : 0;
                renderer.sortingOrder = body != null ? body.sortingOrder + 2 : 20;
            }
        }
        private void Present()
        {
            age = 0; footprint.enabled = true; footprint.color = tint;
            wave.enabled = circular; wave.transform.localScale = Vector3.one * .15f;
            wave.color = tint;
        }
        private void Update()
        {
            if (Time.timeScale <= 0) return;
            // Keep brief attacks readable at x6 without allowing the effect to deal extra damage.
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / duration);
            footprint.color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - t));
            if (circular)
            {
                wave.transform.localScale = Vector3.one * Mathf.Lerp(.15f, 1, Mathf.Clamp01(t * 2));
                wave.color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - t));
            }
            if (t >= 1)
            {
                gameObject.SetActive(false);
                if (pool.Count < 128) pool.Push(this); else Destroy(gameObject);
            }
        }
        private static Sprite Shape(float width, float height, float arc, bool rectangle)
        {
            // Rasterize at the game's 32 pixels per world unit, with an explicit outer footprint.
            string key = $"{width:F4}:{height:F4}:{arc:F2}:{rectangle}";
            if (sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            int w = Mathf.Clamp(Mathf.CeilToInt(width * 32) + 2, 4, 512), h = Mathf.Clamp(Mathf.CeilToInt(height * 32) + 2, 4, 512);
            float pixels = Mathf.Min(32, (w - 2) / Mathf.Max(.001f, width), (h - 2) / Mathf.Max(.001f, height));
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) {name="Pixel attack footprint",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var colors = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float px = (x + .5f - w * .5f) / pixels, py = (y + .5f - h * .5f) / pixels;
                float nx = px / (width * .5f), ny = py / (height * .5f);
                float r = rectangle ? Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny)) : Mathf.Sqrt(nx * nx + ny * ny);
                float angle = Mathf.Atan2(py, px) * Mathf.Rad2Deg;
                if (r > 1 || (!rectangle && Mathf.Abs(angle) > arc * .5f)) continue;
                float edge = r > .8f ? .75f : .12f;
                if (!rectangle && arc < 360) edge *= Mathf.Lerp(.3f, 1, Mathf.InverseLerp(-arc * .5f, arc * .5f, angle));
                colors[y * w + x] = new Color(1,1,1,edge);
            }
            texture.SetPixels32(colors); texture.Apply(false, true);
            var sprite = Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),pixels,0,SpriteMeshType.FullRect);
            sprite.name="Attack geometry"; sprites[key]=sprite;return sprite;
        }
    }
}
