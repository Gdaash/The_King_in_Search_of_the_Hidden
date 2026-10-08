using UnityEngine;

/// <summary>Small sprite fragments emitted only while the linked production timer advances.</summary>
public sealed class ProductionFragments : MonoBehaviour
{
    [System.Serializable]
    public class Surface
    {
        public SpriteRenderer renderer;
        public Vector2[] points;
    }

    [SerializeField] private TimerController timer;
    [SerializeField] private Sprite[] fragments;
    [SerializeField] private Sprite[] dustSprites;
    [SerializeField, Min(0)] private float dustPerSecond = 3f;
    [SerializeField] private Surface[] surfaces;
    [SerializeField, Min(0.1f)] private float particlesPerSecond = 5f;
    [SerializeField] private Vector2 lifetime = new Vector2(0.45f, 0.8f);
    [SerializeField] private Vector2 upwardSpeed = new Vector2(0.45f, 0.9f);
    [SerializeField, Min(0)] private float sidewaysSpeed = 0.5f;
    [SerializeField, Min(1)] private int capacity = 12;

    private struct Fragment
    {
        public SpriteRenderer renderer;
        public Vector3 velocity;
        public float age, life, spin;
        public bool isDust;
    }
    private Fragment[] pool;
    private float untilNext;
    private float untilDust;
    public bool IsEmitting => timer != null && timer.isActiveAndEnabled && timer.IsRunning &&
                              timer.TimeRemaining > 0 && GameSpeedControls.SimulationSpeed > 0;

    private void OnEnable()
    {
        untilNext = Random.Range(0.04f, 0.16f);
        untilDust = Random.Range(0.08f, 0.2f);
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime * GameSpeedControls.SimulationSpeed;
        if (dt <= 0) return;
        if (IsEmitting && fragments != null && fragments.Length > 0 && surfaces != null)
        {
            if (pool == null) CreatePool();
            untilNext -= dt;
            if (untilNext <= 0)
            {
                Emit(false);
                untilNext = Random.Range(0.65f, 1.35f) / Mathf.Max(0.1f, particlesPerSecond);
            }
            if (dustPerSecond > 0 && dustSprites != null && dustSprites.Length > 0)
            {
                untilDust -= dt;
                if (untilDust <= 0)
                {
                    Emit(true);
                    untilDust = Random.Range(0.7f, 1.3f) / dustPerSecond;
                }
            }
        }
        else { untilNext = 0; untilDust = 0; }
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i++)
        {
            ref var p = ref pool[i];
            if (!p.renderer.enabled) continue;
            p.age += dt;
            if (p.age >= p.life) { p.renderer.enabled = false; continue; }
            if (p.isDust) p.velocity *= Mathf.Exp(-1.5f * dt);
            else p.velocity.y -= 1.1f * dt;
            p.renderer.transform.position += p.velocity * dt;
            p.renderer.transform.Rotate(0, 0, p.spin * dt);
            p.renderer.color = new Color(1, 1, 1, (p.isDust ? 0.8f : 1f) *
                (1 - Mathf.InverseLerp(p.isDust ? 0.2f : 0.65f, 1, p.age / p.life)));
        }
    }

    private void CreatePool()
    {
        pool = new Fragment[Mathf.Clamp(capacity, 1, 64)];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("Production fragment");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            pool[i].renderer = renderer;
        }
    }

    private void Emit(bool dust)
    {
        // Reservoir selection avoids allocations and includes only the visible stage.
        Surface chosen = null;
        int count = 0;
        foreach (var surface in surfaces)
            if (surface.renderer != null && surface.renderer.enabled && surface.renderer.gameObject.activeInHierarchy &&
                surface.points != null && surface.points.Length > 0 && Random.Range(0, ++count) == 0)
                chosen = surface;
        if (chosen == null) return;
        for (int i = 0; i < pool.Length; i++)
        {
            ref var p = ref pool[i];
            if (p.renderer.enabled) continue;
            var point = chosen.points[Random.Range(0, chosen.points.Length)];
            if (chosen.renderer.flipX) point.x = -point.x;
            if (chosen.renderer.flipY) point.y = -point.y;
            p.renderer.transform.position = chosen.renderer.transform.TransformPoint(point);
            p.renderer.transform.rotation = dust ? Quaternion.identity : Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            var sprites = dust ? dustSprites : fragments;
            p.renderer.sprite = sprites[Random.Range(0, sprites.Length)];
            p.renderer.sortingLayerID = chosen.renderer.sortingLayerID;
            p.renderer.sortingOrder = chosen.renderer.sortingOrder + 2;
            p.renderer.color = new Color(1, 1, 1, dust ? 0.8f : 1f);
            p.renderer.enabled = true;
            p.age = 0;
            p.isDust = dust;
            p.life = Random.Range(lifetime.x, lifetime.y);
            p.velocity = new Vector3(Random.Range(-sidewaysSpeed, sidewaysSpeed), Random.Range(upwardSpeed.x, upwardSpeed.y));
            if (dust) p.velocity *= 0.55f;
            p.spin = dust ? 0 : Random.Range(-160f, 160f);
            return;
        }
    }

    private void OnDisable()
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i++)
            if (pool[i].renderer != null) pool[i].renderer.enabled = false;
    }
}
