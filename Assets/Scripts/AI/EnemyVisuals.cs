using UnityEngine;
using System.Collections;

public class EnemyVisuals : MonoBehaviour
{
    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; 

    [Header("Ссылки")]
    [SerializeField] private Transform spriteParent; 
    private IEnemyAI _ai;

    [Header("Прыжки (Движение)")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 12f;

    [Header("Настройки Атаки")]
    [SerializeField] private float jabDist = 0.7f;
    [SerializeField] private float jabSpeed = 15f;
    [SerializeField, Min(0f)] private float windupDuration = 0.16f;
    [SerializeField, Min(0f)] private float impactHoldDuration = 0.06f;
    [SerializeField] private Vector2 windupScale = new(1.08f, 0.9f);

    private Vector3 _startPos;
    private bool _isMoving;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;

    void Awake()
    {
        _ai = GetComponent<IEnemyAI>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start() 
    { 
        _startPos = spriteParent.localPosition; 
        _sr = spriteParent.GetComponent<SpriteRenderer>();
    }

    void Update() 
    {
        if (_rb != null && _rb.linearVelocity.magnitude < 0.1f) _isMoving = false;

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

        Vector3 baseScale = spriteParent.localScale;
        Vector3 preparedScale = new(baseScale.x * windupScale.x, baseScale.y * windupScale.y, baseScale.z);
        yield return AnimatePose(_startPos, _startPos - localDir * jabDist * 0.18f, baseScale, preparedScale, windupDuration);

        target = _ai.GetTarget();
        if (target == null) { RestorePose(baseScale); _ai.FinishAttack(); yield break; }
        worldDir = (target.position - transform.position).normalized;
        localDir = transform.InverseTransformDirection(worldDir);
        localDir.x *= Mathf.Sign(transform.localScale.x);
        localDir.y *= Mathf.Sign(transform.localScale.y);
        targetLocalPos = _startPos + localDir * jabDist;

        yield return AnimatePose(spriteParent.localPosition, targetLocalPos, spriteParent.localScale,
            new Vector3(baseScale.x * 0.92f, baseScale.y * 1.08f, baseScale.z), 1f / Mathf.Max(0.01f, jabSpeed));

        if (target.TryGetComponent<Health>(out var h) && stats != null) 
        {
            foreach (var dmgInfo in stats.damageSettings)
            {
                h.TakeDamage(dmgInfo.TotalDamage, dmgInfo.type, transform);
            }
        }
        CombatImpactBurst.Spawn(target.position, new Color(1f, 0.82f, 0.48f, 1f));
        if (impactHoldDuration > 0f) yield return new WaitForSeconds(impactHoldDuration);
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
            elapsed += Time.deltaTime;
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
