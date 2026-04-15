using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;
    
    [Header("Обход препятствий")]
    [SerializeField] private float obstacleDist = 1.5f; // Чуть больше, чтобы заранее огибать
    [SerializeField] private float agentAvoidanceRadius = 0.3f; 
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;
    
    private Vector2 _avoidanceOffset; // Плавное смещение для обхода

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<IEnemyAI>(); 
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start() => _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);

    public void SetMove(bool state) 
    {
        _canMove = state;
        if (!state) {
            _rb.linearVelocity = Vector2.zero;
            _avoidanceOffset = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = _rb.position;

        if (Vector2.Distance(currentPos, targetPos) < 0.2f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Основное направление к цели
        Vector2 desiredDir = (targetPos - currentPos).normalized;
        
        // Вычисляем обход
        Vector2 avoidanceDir = CalculateAvoidance(desiredDir);
        
        // Плавно смешиваем текущий обход с предыдущим, чтобы не было дрожи
        _avoidanceOffset = Vector2.Lerp(_avoidanceOffset, avoidanceDir, Time.fixedDeltaTime * 8f);

        _rb.linearVelocity = _avoidanceOffset * _finalSpeed;
        
        // Разворот через Scale
        if (Mathf.Abs(_rb.linearVelocity.x) > flipThreshold)
        {
            float newScaleX = _rb.linearVelocity.x > 0 ? _initialScaleX : -_initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    private Vector2 CalculateAvoidance(Vector2 desiredDir)
    {
        // Если путь свободен — идем прямо
        if (!IsPathBlocked(desiredDir)) return desiredDir;

        // Веер лучей для поиска свободного прохода
        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f };
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!IsPathBlocked(checkDir)) return checkDir;
        }
        
        return desiredDir;
    }

    private bool IsPathBlocked(Vector2 dir) 
    {
        RaycastHit2D hit = Physics2D.CircleCast(_rb.position, agentAvoidanceRadius, dir, obstacleDist, obstacleLayer);
        return hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger;
    }
}