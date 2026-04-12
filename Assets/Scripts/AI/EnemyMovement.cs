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
    [SerializeField] private float flipThreshold = 0.2f; 

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
    }

    void Start()
    {
        _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        if (_ai == null) Debug.LogWarning($"На объекте {name} не найден компонент ИИ!");
    }

    public void SetMove(bool state) => _canMove = state;

    void FixedUpdate()
    {
        // Проверка: можно ли двигаться и есть ли цель
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = transform.position;
        Vector2 moveDir;

        // ПРОВЕРКА НА ОТБЕГАНИЕ (FLEE)
        // Пытаемся привести интерфейс к типу дальнобойного ИИ, чтобы узнать, убегает ли он
        if (_ai is EnemyAI_Ranged rangedAI && rangedAI.IsFleeing())
        {
            // Направление СТРОГО ОТ цели
            moveDir = (currentPos - targetPos).normalized;
        }
        else
        {
            // Обычное движение К цели с обходом препятствий
            moveDir = CalculateAvoidance(targetPos);
        }

        _rb.linearVelocity = moveDir * _finalSpeed;
        
        // Логика разворота спрайта (Flip) с порогом
        if (Mathf.Abs(moveDir.x) > flipThreshold)
        {
            float targetScaleX = moveDir.x > 0 ? _initialScaleX : -_initialScaleX;
            
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

        // Если путь свободен, идем прямо
        if (!HitWall(desiredDir)) return desiredDir;

        // Если впереди стена, ищем свободный угол
        float[] angles = { 45f, -45f, 90f, -90f };
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!HitWall(checkDir)) return checkDir;
        }
        
        return desiredDir;
    }

    private bool HitWall(Vector2 dir) 
    {
        return Physics2D.Raycast(transform.position, dir, obstacleDist, obstacleLayer);
    }
}