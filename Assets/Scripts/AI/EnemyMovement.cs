using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;
    [SerializeField] private float obstacleDist = 1.2f; 
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.2f; // Порог: не разворачивать, если движение почти вертикальное

    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<IEnemyAI>(); 
        
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (_ai == null) Debug.LogWarning($"На объекте {name} не найден компонент ИИ!");
    }

    public void SetMove(bool state) => _canMove = state;

    void FixedUpdate()
    {
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 dir = CalculateAvoidance(targetPos);
        
        _rb.linearVelocity = dir * _finalSpeed;
        
        // Убираем дерганье: разворот только при уверенном движении влево или вправо
        if (Mathf.Abs(dir.x) > flipThreshold)
        {
            float targetScaleX = dir.x > 0 ? _initialScaleX : -_initialScaleX;
            
            // Проверка, чтобы не обновлять scale каждый кадр без нужды
            if (!Mathf.Approximately(transform.localScale.x, targetScaleX))
            {
                transform.localScale = new Vector3(targetScaleX, transform.localScale.y, 1);
            }
        }
    }

    private Vector2 CalculateAvoidance(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        Vector2 desiredDir = (targetPos - currentPos).normalized;

        if (!HitWall(desiredDir)) return desiredDir;

        float[] angles = { 45f, -45f, 90f, -90f };
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!HitWall(checkDir)) return checkDir;
        }
        return desiredDir;
    }

    private bool HitWall(Vector2 dir) => Physics2D.Raycast(transform.position, dir, obstacleDist, obstacleLayer);
}