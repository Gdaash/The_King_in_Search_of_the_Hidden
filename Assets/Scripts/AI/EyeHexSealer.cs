using UnityEngine;

/// <summary>Flying-eye movement and ten-second capture run independently of its ranged attacks.</summary>
[RequireComponent(typeof(EnemyAI_Ranged), typeof(EnemyMovement), typeof(Rigidbody2D))]
public sealed class EyeHexSealer : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float captureDuration = 10f;
    private WorldHexFog target;
    private Rigidbody2D body;
    private EnemyMovement movement;
    private Health health;
    private WaveSpawnTelegraph vortex;
    private float elapsed, nextSearch;
    public bool ControlsMovement => isActiveAndEnabled;
    public bool IsCapturing => target != null && target.Owner == this && target.IsCapturing;
    public float CaptureProgress => Mathf.Clamp01(elapsed / captureDuration);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<Health>();
    }
    private void OnEnable() { if (movement != null) movement.enabled = false; }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime)
    {
        if (health != null && health.IsDead) { Release(); return; }
        if (deltaTime <= 0 || (health != null && health.IsStunned)) return;
        if (target != null && target.IsClosed)
        {
            elapsed += deltaTime;
            if (elapsed >= captureDuration + .5f) Release();
            return;
        }
        if (target == null || !target.IsOpen || target.Owner != this)
        {
            Release();
            if (Time.time < nextSearch) return;
            nextSearch = Time.time + .25f;
            WorldHexFog best = null; float nearest = float.PositiveInfinity;
            foreach (var hex in WorldHexFog.Tiles)
            {
                if (hex == null || !hex.IsOpen || hex.Owner != null || hex.gameObject.scene != gameObject.scene) continue;
                float distance = (hex.Center - body.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; best = hex; }
            }
            if (best == null || !best.TryClaim(this)) return;
            target = best;
        }
        if (!IsCapturing)
        {
            if ((target.Center - body.position).sqrMagnitude > .12f * .12f) return;
            target.BeginCapture(this);
            var effect = new GameObject("Eye sealing vortex");
            effect.transform.position = target.Center;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
            vortex = effect.AddComponent<WaveSpawnTelegraph>();
        }
        elapsed += deltaTime;
        vortex?.SetSealingProgress(CaptureProgress, WorldHexFog.HexRadius);
        if (elapsed >= captureDuration) target.Seal(this);
    }
    private void FixedUpdate()
    {
        if (body == null) return;
        if (target == null || target.IsClosed || IsCapturing || (health != null && (health.IsDead || health.IsStunned)))
        { body.linearVelocity = Vector2.zero; return; }
        Vector2 delta = target.Center - body.position;
        body.linearVelocity = delta.sqrMagnitude <= .12f * .12f ? Vector2.zero : delta.normalized * movement.CurrentSpeed;
    }
    private void Release()
    {
        if (target != null) target.CancelCapture(this);
        target = null; elapsed = 0;
        if (vortex != null) Destroy(vortex.gameObject);
        vortex = null;
    }
    private void OnDisable()
    {
        Release();
        if (body != null) body.linearVelocity = Vector2.zero;
        if (movement != null && health != null && !health.IsDead) movement.enabled = true;
    }
}
