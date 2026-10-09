using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Small pixel stars in one UI mesh: no per-frame GameObjects or particle allocations.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RisingLabelStars : MaskableGraphic
    {
        [SerializeField] private RectTransform label;
        [SerializeField] private Sprite starSprite;
        [SerializeField, Min(0)] private float emissionRate = 5;
        [SerializeField, Min(.1f)] private float lifetime = 1.2f;
        private struct Star { public Vector2 start; public float age, drift; public bool alive; }
        private readonly Star[] particles = new Star[16];
        private float next;
        public bool Emit { get; set; }
        public Sprite StarSprite => starSprite;
        public override Texture mainTexture => starSprite != null ? starSprite.texture : Texture2D.whiteTexture;
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
        protected override void OnDisable() { System.Array.Clear(particles, 0, particles.Length); next = 0; base.OnDisable(); }
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i].alive) { particles[i].age += dt; if (particles[i].age >= lifetime) particles[i].alive = false; }
            next -= dt;
            if (Emit && label != null && next <= 0)
            {
                next = 1 / Mathf.Max(.1f, emissionRate);
                for (int i = 0; i < particles.Length; i++) if (!particles[i].alive)
                {
                    var p = label.TransformPoint(new Vector3(Random.Range(label.rect.xMin, label.rect.xMax), Random.Range(-8f, 8f)));
                    particles[i] = new Star { alive = true, start = rectTransform.InverseTransformPoint(p), drift = Random.Range(-10f, 10f) };
                    break;
                }
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (starSprite == null) return;
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(starSprite);
            Vector2 halfSize = starSprite.rect.size; // Full Canvas size is exactly 2x source pixels.
            foreach (var star in particles)
            {
                if (!star.alive) continue;
                float t = star.age / lifetime;
                Vector2 p = star.start + new Vector2(star.drift * t, 40 * t);
                p = new Vector2(Mathf.Round(p.x / 2) * 2, Mathf.Round(p.y / 2) * 2);
                Color c = color; c.a *= Mathf.Sin(t * Mathf.PI);
                // Use the same pixel silhouette as soldier levels, with the label's green tint.
                int start = vh.currentVertCount;
                vh.AddVert(p - halfSize, c, new Vector2(uv.x, uv.y));
                vh.AddVert(p + new Vector2(-halfSize.x, halfSize.y), c, new Vector2(uv.x, uv.w));
                vh.AddVert(p + halfSize, c, new Vector2(uv.z, uv.w));
                vh.AddVert(p + new Vector2(halfSize.x, -halfSize.y), c, new Vector2(uv.z, uv.y));
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
