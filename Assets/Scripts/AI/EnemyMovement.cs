using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;
    
    [Header("Обход препятствий")]
    [SerializeField] private float obstacleDist = 1.5f; // Увеличили, чтобы замечал раньше
    [SerializeField] private float agentAvoidanceRadius = 0.3f; 
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;
    
    // Переменная для сглаживания обхода
    private Vector2 _lastMoveDir;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<IEnemyAI>(); 
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start() => _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);

    public void SetMove(bool state) 
    {
        _canMove = state;
        if (!state && _rb != null) _rb.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            _lastMoveDir = Vector2.zero;
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = _rb.position;

        if (Vector2.Distance(currentPos, targetPos) < 0.2f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = CalculateAvoidance(targetPos);
        
        // Сглаживаем направление, чтобы ИИ не дребезжал на углах
        _lastMoveDir = Vector2.Lerp(_lastMoveDir, moveDir, Time.fixedDeltaTime * 5f);
        
        _rb.linearVelocity = _lastMoveDir.normalized * _finalSpeed;
        
        if (Mathf.Abs(_lastMoveDir.x) > flipThreshold)
        {
            float newScaleX = _lastMoveDir.x > 0 ? _initialScaleX : -_initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    private Vector2 CalculateAvoidance(Vector2 targetPos)
    {
        Vector2 currentPos = _rb.position;
        Vector2 desiredDir = (targetPos - currentPos).normalized;

        // Если путь свободен — идем прямо
        if (!IsPathBlocked(desiredDir)) return desiredDir;

        // Если путь закрыт, проверяем веер лучей (от узких к широким)
        // Добавили больше углов для более плавного обхода
        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f, 130f, -130f };
        
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!IsPathBlocked(checkDir))
            {
                // Чтобы обход был более уверенным, подталкиваем его чуть сильнее в сторону
                return checkDir;
            }
        }
        
        return desiredDir;
    }

    private bool IsPathBlocked(Vector2 dir) 
    {
        // CircleCast имитирует ширину моба
        RaycastHit2D hit = Physics2D.CircleCast(_rb.position, agentAvoidanceRadius, dir, obstacleDist, obstacleLayer);
        
        // Проверяем, что попали не в себя и не в триггер (если здания — триггеры)
        if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
        {
            return true;
        }
        return false;
    }
}