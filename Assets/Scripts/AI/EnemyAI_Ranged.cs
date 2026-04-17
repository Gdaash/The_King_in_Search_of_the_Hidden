using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI_Ranged : MonoBehaviour, IEnemyAI
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 8f;   
    [SerializeField] private float stopRange = 6f;     

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 2f;
    [SerializeField] private float cooldownVariation = 0.5f; 

    [Header("Настройки защиты (Home)")]
    [SerializeField] private float homeStoppingDistance = 0.5f;
    [SerializeField] private float positionVariation = 3.0f; // Радиус разброса вокруг базы

    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private Transform _homeTransform;
    
    private bool _isAttacking = false;
    private float _nextAttackTime;
    private float _currentCooldown;
    
    private Vector2 _personalOffset;
    private GameObject _offsetAnchor; // Невидимая цель для движения домой

    void Awake()
    {
        // Генерируем уникальное смещение один раз при спавне
        _personalOffset = Random.insideUnitCircle * positionVariation;
        
        // Создаем невидимый объект, позицию которого будем использовать как цель
        _offsetAnchor = new GameObject($"RangedAnchor_{gameObject.name}");
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

        if (!_isAttacking && distanceToTarget <= attackRange && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            OnAttack?.Invoke();
            _nextAttackTime = Time.time + _currentCooldown;
        }

        if (!_isAttacking)
        {
            if (distanceToTarget > stopRange && distanceToTarget <= detectionRange) 
                OnMove?.Invoke();
            else 
                OnStop?.Invoke();
        }
    }

    private void ReturnToHome()
    {
        // Устанавливаем якорь в персональную точку около базы
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

    public void OnTakeDamage(Transform attacker)
    {
        if (attacker != null)
        {
            _target = attacker;
            _isAttacking = false;
        }
    }

    private void ResetCooldown() => _currentCooldown = baseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    
    // EnemyMovement вызывает этот метод, чтобы знать, куда лететь
    public Transform GetTarget() 
    {
        if (_target != null) return _target;
        if (_homeTransform != null) return _offsetAnchor.transform;
        return null;
    }

    public string GetTargetTag() => targetTag;

    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }

    private void OnDestroy() 
    { 
        if (_offsetAnchor != null) Destroy(_offsetAnchor); 
    }
}