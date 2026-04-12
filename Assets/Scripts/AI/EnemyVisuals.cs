using UnityEngine;
using System.Collections;

public class EnemyVisuals : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform spriteParent; 
    [SerializeField] private EnemyAI ai;

    [Header("Прыжки (Движение)")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 12f;

    [Header("Настройки Атаки")]
    [SerializeField] private float jabDist = 0.7f;
    [SerializeField] private float jabSpeed = 15f;
    [SerializeField] private int damage = 10;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    private Vector3 _startPos;
    private bool _isMoving;
    private SpriteRenderer _sr;
    private Color _origCol;

    void Start() 
    { 
        _startPos = spriteParent.localPosition; 
        _sr = spriteParent.GetComponent<SpriteRenderer>();
        if (_sr) _origCol = _sr.color;
    }

    void Update() 
    {
        if (_isMoving) 
        {
            float wave = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
            spriteParent.localPosition = _startPos + Vector3.up * (wave * bounceHeight);
        } 
        else 
        {
            spriteParent.localPosition = Vector3.Lerp(spriteParent.localPosition, _startPos, Time.deltaTime * 5f);
        }
    }

    public void SetMoving(bool state) => _isMoving = state;
    public void StartJab() => StartCoroutine(JabRoutine());

    private IEnumerator JabRoutine() 
    {
        Transform target = ai.GetTarget();
        if (target == null) 
        { 
            ai.FinishAttack(); 
            yield break; 
        }

        if (_sr) _sr.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        
        Vector3 worldDir = (target.position - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(worldDir);
        
        localDir.x *= Mathf.Sign(transform.localScale.x);
        localDir.y *= Mathf.Sign(transform.localScale.y);

        Vector3 targetLocalPos = _startPos + localDir * jabDist;

        if (target.TryGetComponent<Health>(out var h)) h.TakeDamage(damage, damageType);
        
        // Рывок вперед
        float p = 0;
        while (p < 1f) 
        { 
            p += Time.deltaTime * jabSpeed; 
            spriteParent.localPosition = Vector3.Lerp(_startPos, targetLocalPos, p); 
            yield return null; 
        }
        
        // Возврат назад
        p = 0;
        while (p < 1f) 
        { 
            p += Time.deltaTime * jabSpeed; 
            spriteParent.localPosition = Vector3.Lerp(targetLocalPos, _startPos, p); 
            yield return null; 
        }
        
        if (_sr) _sr.color = _origCol;
        ai.FinishAttack();
    }
}