using UnityEngine;

/// <summary>Independent pixel tails appear around the rim and spiral inward at different speeds.</summary>
public sealed class WaveSpawnTelegraph : MonoBehaviour
{
    private const int TailCount = 72;
    private SpriteRenderer[] arms = new SpriteRenderer[TailCount];
    private TailMotion[] motion = new TailMotion[TailCount];
    private static Sprite tentacle;
    private float spriteWidth;
    private static Sprite sealingParticle;
    private bool sealing;
    private float sealingRadius = .2f;
    private float sealingExpansion;

    public void SetSealingProgress(float progress, float hexRadius)
    {
        if (!sealing)
        {
            sealing = true;
            if (sealingParticle == null)
            {
                var texture = new Texture2D(12, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Eye pixel mist" };
                var pixels = new Color[12 * 7];
                // A broken, short wisp with a translucent grey-purple fringe, not a spawn tentacle.
                string[] rows = { "000000000000", "000022200000", "001234432100", "123455432210", "012344321000", "000122100000", "000000000000" };
                for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++)
                {
                    int shade = rows[y][x] - '0';
                    pixels[y * 12 + x] = new Color(.4f + shade * .05f, .27f + shade * .05f, .52f + shade * .065f, shade == 0 ? 0 : .18f + shade * .14f);
                }
                texture.SetPixels(pixels); texture.Apply();
                sealingParticle = Sprite.Create(texture, new Rect(0, 0, 12, 7), Vector2.one * .5f, 32);
            }
            spriteWidth = sealingParticle.bounds.size.x;
            // Only the eye gets a denser cloud; ordinary spawn warnings retain their 72 tails.
            System.Array.Resize(ref arms, TailCount * 3);
            System.Array.Resize(ref motion, arms.Length);
            for (int i = TailCount; i < arms.Length; i++)
            {
                var child = new GameObject("Eye mist particle");
                child.transform.SetParent(transform, false);
                arms[i] = child.AddComponent<SpriteRenderer>();
                motion[i] = NewTail();
                motion[i].age = Between(0, motion[i].lifetime);
            }
            foreach (var arm in arms) if (arm != null) { arm.sprite = sealingParticle; arm.sortingOrder = 120; }
        }
        sealingExpansion = Mathf.Clamp01(progress);
        sealingRadius = Mathf.Lerp(.035f, hexRadius / .92f, Mathf.SmoothStep(0, 1, sealingExpansion));
        for (int i = 0; i < arms.Length; i++) if (arms[i] != null) DrawTail(i);
    }
    // Visual randomness must not consume the gameplay random sequence.
    private System.Random random;

    private struct TailMotion
    {
        public float age, lifetime, angle, spin, radius, phase, wobbleSpeed;
    }
    private float Between(float min, float max) => min + (float)random.NextDouble() * (max - min);
    private TailMotion NewTail() => new TailMotion
    {
        lifetime = Between(1.7f, 4.1f), angle = Between(0, 360), spin = Between(110, 290),
        radius = Between(.8f, .92f), phase = Between(0, Mathf.PI * 2), wobbleSpeed = Between(2, 4.5f)
    };

    private void Awake()
    {
        random = new System.Random(GetEntityId().GetHashCode());
        if (tentacle == null) tentacle = Resources.Load<Sprite>("Effects/Spawn Tentacle");
        spriteWidth = tentacle != null ? Mathf.Max(.01f, tentacle.bounds.size.x) : 1;
        for (int i = 0; i < arms.Length; i++)
        {
            var child = new GameObject("Pixel vortex tail");
            child.transform.SetParent(transform, false);
            var sprite = child.AddComponent<SpriteRenderer>();
            sprite.sprite = tentacle;
            sprite.sortingOrder = 32000 + i;
            arms[i] = sprite;
            motion[i] = NewTail();
            motion[i].age = Between(0, motion[i].lifetime);
            DrawTail(i);
        }
    }

    private void Update()
    {
        for (int i = 0; i < arms.Length; i++)
        {
            motion[i].age += Time.deltaTime;
            if (motion[i].age >= motion[i].lifetime)
            {
                float overflow = motion[i].age - motion[i].lifetime;
                motion[i] = NewTail();
                motion[i].age = Mathf.Repeat(overflow, motion[i].lifetime);
            }
            DrawTail(i);
        }
    }

    private void DrawTail(int i)
    {
        var tail = motion[i];
        float progress = tail.age / tail.lifetime, remaining = 1 - progress;
        float wave = tail.age * tail.wobbleSpeed + tail.phase;
        float envelope = Mathf.Sin(progress * Mathf.PI);
        float radius = tail.radius * remaining + .035f * Mathf.Sin(wave) * envelope;
        float angle = tail.angle - tail.spin * tail.age + 9 * Mathf.Sin(wave);
        float radians = angle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians), cos = Mathf.Cos(radians);
        float radiusMultiplier = sealing ? sealingRadius : 1f;
        arms[i].transform.localPosition = new Vector3(cos * radius * radiusMultiplier, sin * radius * radiusMultiplier, 0);

        // Follow this particle's own curved trajectory, including its slight drift.
        float radialSpeed = -tail.radius / tail.lifetime + .035f * (tail.wobbleSpeed * Mathf.Cos(wave) * envelope +
            Mathf.Sin(wave) * Mathf.PI / tail.lifetime * Mathf.Cos(progress * Mathf.PI));
        float angularSpeed = (-tail.spin + 9 * tail.wobbleSpeed * Mathf.Cos(wave)) * Mathf.Deg2Rad;
        float heading = Mathf.Atan2(radialSpeed * sin + radius * angularSpeed * cos,
            radialSpeed * cos - radius * angularSpeed * sin) * Mathf.Rad2Deg;
        float scale = (sealing ? .12f + .13f * remaining : 2 * (.22f + .35f * remaining)) / spriteWidth;
        if (sealing) scale *= Mathf.Lerp(.12f, 1f, Mathf.Sqrt(sealingExpansion));
        arms[i].transform.localScale = new Vector3(scale, scale * (sealing ? 1f : 2.2f), 1);
        arms[i].transform.localRotation = Quaternion.Euler(0, 0, heading);
        float fade = Mathf.SmoothStep(0, 1, Mathf.Min(progress / .1f, remaining / .16f));
        float pulse = Mathf.Sin(wave);
        arms[i].color = sealing ? new Color(1, 1, 1, .9f * fade)
            : new Color(.9f, .9f, .9f, (.7f + .08f * pulse) * .8f * fade);
    }
}


