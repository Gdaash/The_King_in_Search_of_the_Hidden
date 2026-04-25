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

        yield return new WaitForSeconds(0.2f);
        
        Vector3 worldDir = (target.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(worldDir);
        localDir.x *= Mathf.Sign(transform.localScale.x);
        localDir.y *= Mathf.Sign(transform.localScale.y);

        Vector3 targetLocalPos = _startPos + localDir * jabDist;

        // ПРИМЕНЕНИЕ УРОНА (ИЗ СПИСКА)
        if (target.TryGetComponent<Health>(out var h) && stats != null) 
        {
            foreach (var dmgInfo in stats.damageSettings)
            {
                h.TakeDamage(dmgInfo.TotalDamage, dmgInfo.type, transform);
            }
        }
        
        // Анимация выпада
        float p = 0;
        while (p < 1f) 
        { 
            p += Time.deltaTime * jabSpeed; 
            spriteParent.localPosition = Vector3.Lerp(_startPos, targetLocalPos, p); 
            yield return null; 
        }
        p = 0;
        while (p < 1f) 
        { 
            p += Time.deltaTime * jabSpeed; 
            spriteParent.localPosition = Vector3.Lerp(targetLocalPos, _startPos, p); 
            yield return null; 
        }
        
        _ai.FinishAttack();
    }
}
