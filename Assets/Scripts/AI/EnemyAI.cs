using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 0.2f; 

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 1.5f;
    [SerializeField] private float cooldownVariation = 0.4f; 

    [Header("Настройки защиты (Home)")]
    [SerializeField] private GameObject homeBase;
    [SerializeField] private float homeStoppingDistance = 0.3f;
    [SerializeField] private float positionVariation = 1.5f; // На сколько юниты могут стоять вразнобой

    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private Collider2D _targetCollider; 
    private Collider2D _homeCollider; 
    private bool _isAttacking = false;
    private float _currentCooldown;
    private float _nextAttackTime;
    
    private Vector2 _personalOffset; // Персональное смещение юнита

    void Awake()
    {
        if (homeBase != null) _homeCollider = homeBase.GetComponent<Collider2D>();
        // Генерируем случайный сдвиг, чтобы не толпились в одной точке
        _personalOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(0, positionVariation);
    }

    void Start() => ResetCooldown();

    void Update()
    {
        FindClosestTarget();

        if (_target != null)
        {
            HandleCombat();
        }
        else
        {
            ReturnToHome();
        }
    }

    private void HandleCombat()
    {
        Vector2 closestPoint = _targetCollider.ClosestPoint(transform.position);
        float distanceToEdge = Vector2.Distance(transform.position, closestPoint);

        if (distanceToEdge <= attackRange)
        {
            OnStop?.Invoke();
            if (!_isAttacking && Time.time >= _nextAttackTime)
            {
                _isAttacking = true;
                OnAttack?.Invoke();
                _nextAttackTime = Time.time + _currentCooldown;
            }
        }
        else if (distanceToEdge <= detectionRange && !_isAttacking)
        {
            OnMove?.Invoke();
        }
        else 
        { 
            OnStop?.Invoke(); 
        }
    }

    private void ReturnToHome()
    {
        if (_homeCollider == null) 
        {
            OnStop?.Invoke();
            return;
        }

        // Берем точку на краю, но добавляем персональный оффсет
        Vector2 pointOnEdge = _homeCollider.ClosestPoint((Vector2)transform.position);
        Vector2 targetPosition = pointOnEdge + _personalOffset;
        
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        if (distanceToTarget > homeStoppingDistance)
        {
            OnMove?.Invoke();
        }
        else
        {
            OnStop?.Invoke();
        }
    }

    private void FindClosestTarget()
    {
        var targets = GameObject.FindGameObjectsWithTag(targetTag);
        var closest = targets
            .Select(t => t.GetComponent<Collider2D>())
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .OrderBy(c => Vector2.SqrMagnitude(c.transform.position - transform.position))
            .FirstOrDefault();

        if (closest != null)
        {
            _target = closest.transform;
            _targetCollider = closest;
        }
        else
        {
            _target = null;
            _targetCollider = null;
        }
    }

    private void ResetCooldown() => _currentCooldown = baseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    
    public Transform GetTarget() 
    {
        if (_target != null) return _target;
        if (homeBase != null) return homeBase.transform;
        return null;
    }

    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }
}