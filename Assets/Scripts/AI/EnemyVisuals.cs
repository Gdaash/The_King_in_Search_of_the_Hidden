using GameFoundation.Combat;
using UnityEngine;
using System.Collections;
using GameFoundation.MetaProgression;

public class EnemyVisuals : MonoBehaviour
{
    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; 
    public GlobalStats Stats => stats;

    [Header("Ссылки")]
    [SerializeField] private Transform spriteParent; 
    private IEnemyAI _ai;
    private EnemyMovement movement;
    private float AttackRate => movement != null ? (movement.IsStunned ? 0f : movement.AreaSlowMultiplier) : 1f;

    [Header("Прыжки (Движение)")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 12f;

    [Header("Настройки Атаки")]
    [SerializeField] private float jabDist = 0.7f;
    [SerializeField] private float jabSpeed = 15f;
    [SerializeField, Min(0f)] private float windupDuration = 0.16f;
    [SerializeField, Min(0f)] private float impactHoldDuration = 0.06f;
    [SerializeField] private Vector2 windupScale = new(1.08f, 0.9f);

    private Vector3 _baseScale;
    private Vector3 _startPos;
    private bool _isMoving;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;

    void Awake()
    {
        if (spriteParent != null) { _baseScale = spriteParent.localScale; _startPos = spriteParent.localPosition; }
        _ai = GetComponent<IEnemyAI>();
        movement = GetComponent<EnemyMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start() 
    { 
        _startPos = spriteParent.localPosition; 
        _sr = spriteParent.GetComponent<SpriteRenderer>();
    }

    public void ResetForSpawn() { _isMoving = false; }

    void Update() 
    {
        if (_rb != null) _isMoving = _rb.linearVelocity.sqrMagnitude >= .01f && (movement == null || !movement.IsStunned);

        if (_isMoving) 
        {
            float wave = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
            spriteParent.localPosition = _startPos + Vector3.up * (wave * bounceHeight);
        } 
        else 
        {
            spriteParent.localPosition = Vector3.Lerp(spriteParent.localPosition, _startPos, Time.deltaTime * 10f);
        }
    }

    public void BeginTravel()
    {
        StopAllCoroutines();
        _ai?.FinishAttack();
        if (spriteParent != null)
        {
            spriteParent.localPosition = _startPos;
            spriteParent.localScale = new Vector3(Mathf.Abs(_baseScale.x) * Mathf.Sign(spriteParent.localScale.x), _baseScale.y, _baseScale.z);
        }
        _isMoving = true;
    }
    public void SetMoving(bool state) => _isMoving = state;

    public void StartJab() 
    {
        _isMoving = false; 
        StartCoroutine(JabRoutine());
    }

    private IEnumerator JabRoutine() 
    {
        Transform target = _ai.GetTarget();
        if (target == null) { _ai.FinishAttack(); yield break; }

        Vector3 worldDir = (target.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(worldDir);
        localDir.x *= Mathf.Sign(transform.localScale.x);
        localDir.y *= Mathf.Sign(transform.localScale.y);

        Vector3 targetLocalPos = _startPos + localDir * jabDist;

        Vector3 baseScale = _baseScale;
        baseScale.x = Mathf.Abs(baseScale.x) * Mathf.Sign(spriteParent.localScale.x);
        Vector3 preparedScale = new(baseScale.x * windupScale.x, baseScale.y * windupScale.y, baseScale.z);
        yield return AnimatePose(_startPos, _startPos - localDir * jabDist * 0.18f, baseScale, preparedScale, windupDuration);

        while (movement != null && movement.IsStunned) yield return null;
        while (movement != null && movement.IsStunned) yield return null;
        target = _ai.GetTarget();
        if (target == null) { RestorePose(baseScale); _ai.FinishAttack(); yield break; }
        worldDir = (target.position - transform.position).normalized;
        localDir = transform.InverseTransformDirection(worldDir);
        localDir.x *= Mathf.Sign(transform.localScale.x);
        localDir.y *= Mathf.Sign(transform.localScale.y);
        targetLocalPos = _startPos + localDir * jabDist;

        yield return AnimatePose(spriteParent.localPosition, targetLocalPos, spriteParent.localScale,
            new Vector3(baseScale.x * 0.92f, baseScale.y * 1.08f, baseScale.z), 1f / Mathf.Max(0.01f, jabSpeed));

        while (movement != null && movement.IsStunned) yield return null;
        if (target == null) { RestorePose(baseScale); _ai.FinishAttack(); yield break; }
        if (target.TryGetComponent<Health>(out var h) && GetComponent<Combatant>() is Combatant combat && combat.weapon!=null) combat.Hit(h);
        else if (target.TryGetComponent<Health>(out h) && stats != null) 
        {
            h.TakeDamage(stats.UnifiedDamage * MilitaryExperience.Multiplier(this),
                stats.damageSettings.Count > 0 ? stats.damageSettings[0].type : DamageType.Physical, transform);
        }
        float hold = 0;
        while (hold < impactHoldDuration) { hold += Time.deltaTime * AttackRate; yield return null; }
        yield return AnimatePose(spriteParent.localPosition, _startPos, spriteParent.localScale, baseScale,
            1.5f / Mathf.Max(0.01f, jabSpeed));
        RestorePose(baseScale);
        _ai.FinishAttack();
    }

    private IEnumerator AnimatePose(Vector3 fromPosition, Vector3 toPosition, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * AttackRate;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = t * t * (3f - 2f * t);
            spriteParent.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            spriteParent.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            yield return null;
        }
    }

    private void RestorePose(Vector3 scale)
    {
        spriteParent.localPosition = _startPos;
        spriteParent.localScale = scale;
    }
}
