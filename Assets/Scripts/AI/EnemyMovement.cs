using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;
    
    [Header("Обход препятствий")]
    [SerializeField] private float obstacleDist = 1.2f; 
    [SerializeField] private float agentAvoidanceRadius = 0.25f; 
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;

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
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = transform.position;

        if (Vector2.Distance(currentPos, targetPos) < 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = CalculateAvoidance(targetPos);
        _rb.linearVelocity = moveDir * _finalSpeed;
        
        if (Mathf.Abs(moveDir.x) > flipThreshold)
        {
            float newScaleX = moveDir.x > 0 ? _initialScaleX : -_initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    private Vector2 CalculateAvoidance(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        Vector2 desiredDir = (targetPos - currentPos).normalized;

        if (!IsPathBlocked(desiredDir)) return desiredDir;

        float[] angles = { 35f, -35f, 75f, -75f };
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!IsPathBlocked(checkDir)) return checkDir;
        }
        return desiredDir;
    }

    private bool IsPathBlocked(Vector2 dir) 
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, agentAvoidanceRadius, dir, obstacleDist, obstacleLayer);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }
}