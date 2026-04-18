using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 0.5f; 

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 1.5f;
    [SerializeField] private float cooldownVariation = 0.4f; 

    [Header("Настройки защиты (Home)")]
    [SerializeField] private float homeStoppingDistance = 0.3f;
    [SerializeField] private float positionVariation = 3.0f; // Радиус случайного разброса вокруг базы

    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private Transform _homeTransform;

    private bool _isAttacking = false;
    private float _currentCooldown;
    private float _nextAttackTime;
    
    // Персональная точка юнита относительно центра базы
    private Vector2 _personalOffset;
    // Якорь-пустышка для EnemyMovement
    private GameObject _offsetAnchor; 

    void Awake()
    {
        // Каждый юнит выбирает случайную точку в круге при рождении
        _personalOffset = Random.insideUnitCircle * positionVariation;
        
        // Создаем невидимую цель для перемещения
        _offsetAnchor = new GameObject($"Anchor_{gameObject.name}");
    }

    void Start() => ResetCooldown();

    void Update()
    {
        FindClosestTarget();

        if (_target == null)
        {
            ValidateOrFindHome();
        }

        if (_target != null)
        {
            HandleCombat();
        }
        else if (_homeTransform != null)
        {
            ReturnToHome();
        }
        else
        {
            OnStop?.Invoke();
        }
    }

    private void ValidateOrFindHome()
    {
        if (_homeTransform == null || !_homeTransform.gameObject.activeInHierarchy)
        {
            var home = Object.FindObjectsByType<HomeBase>(FindObjectsSortMode.None)
                .Where(h => h.gameObject.activeInHierarchy)
                .OrderBy(h => Vector2.SqrMagnitude(h.transform.position - transform.position))
                .FirstOrDefault();

            if (home != null) _homeTransform = home.transform;
        }
    }

    private void HandleCombat()
    {
        float distanceToTarget = Vector2.Distance(transform.position, _target.position);

        if (distanceToTarget <= attackRange)
        {
            OnStop?.Invoke();
            if (!_isAttacking && Time.time >= _nextAttackTime)
            {
                _isAttacking = true;
                OnAttack?.Invoke();
                _nextAttackTime = Time.time + _currentCooldown;
            }
        }
        else if (distanceToTarget <= detectionRange && !_isAttacking)
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
        // Целевая точка = Центр базы + Индивидуальный сдвиг
        Vector3 targetPoint = _homeTransform.position + (Vector3)_personalOffset;
        _offsetAnchor.transform.position = targetPoint;

        float distanceToPoint = Vector2.Distance(transform.position, targetPoint);

        if (distanceToPoint > homeStoppingDistance)
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
        
        _target = targets
            .Where(t => t.activeInHierarchy)
            .OrderBy(t => Vector2.SqrMagnitude(t.transform.position - transform.position))
            .Select(t => t.transform)
            .FirstOrDefault();
    }

    public void OnTakeDamage(Transform attacker = null)
    {
        if (attacker != null)
        {
            _target = attacker;
            _isAttacking = false;
        }
    }

    private void ResetCooldown() => _currentCooldown = baseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    
    // EnemyMovement берет эту цель. Если врага нет — берет персональную точку у дома
    public Transform GetTarget() 
    {
        if (_target != null) return _target;
        if (_homeTransform != null) return _offsetAnchor.transform;
        return null;
    }

    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }

    private void OnDestroy() 
    { 
        if (_offsetAnchor != null) Destroy(_offsetAnchor); 
    }
    public bool GetIsAttacking() => _isAttacking;
}