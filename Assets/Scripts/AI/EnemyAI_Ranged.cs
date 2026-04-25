using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI_Ranged : MonoBehaviour, IEnemyAI
{
    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; 

    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 12f;
    // Поля attackRange и stopRange теперь вспомогательные, если stats не назначен
    [SerializeField] private float defaultAttackRange = 8f;   
    [SerializeField] private float defaultStopRange = 6f;     

    [Header("Настройки атаки (Рандом)")]
    [SerializeField] private float cooldownVariation = 0.2f; 

    [Header("Настройки защиты (Home)")]
    [SerializeField] private float homeStoppingDistance = 0.5f;
    [SerializeField] private float positionVariation = 3.0f;

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
    private GameObject _offsetAnchor;

    // Свойства для удобного доступа к статам
    public float CurrentAttackRange => stats != null ? stats.TotalAttackRange : defaultAttackRange;
    public float CurrentStopRange => stats != null ? (stats.TotalAttackRange * 0.75f) : defaultStopRange;
    public float CurrentCooldown => stats != null ? stats.TotalCooldown : 2f;

    void Awake()
    {
        _personalOffset = Random.insideUnitCircle * positionVariation;
        _offsetAnchor = new GameObject($"RangedAnchor_{gameObject.name}");
    }

    void Start() => ResetCooldown();

    void Update()
    {
        FindClosestTarget();

        if (_target == null) ValidateOrFindHome();

        if (_target != null) HandleCombat();
        else if (_homeTransform != null) ReturnToHome();
        else OnStop?.Invoke();
    }

    private void HandleCombat()
    {
        float distanceToTarget = Vector2.Distance(transform.position, _target.position);

        // Используем CurrentAttackRange из глобальных статов
        if (!_isAttacking && distanceToTarget <= CurrentAttackRange && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            OnAttack?.Invoke();
            
            // Рассчитываем время следующей атаки на основе CurrentCooldown
            _nextAttackTime = Time.time + CurrentCooldown + Random.Range(-cooldownVariation, cooldownVariation);
        }

        if (!_isAttacking)
        {
            // Используем CurrentStopRange (обычно чуть меньше дальности атаки, чтобы не дергаться)
            if (distanceToTarget > CurrentStopRange && distanceToTarget <= detectionRange) 
                OnMove?.Invoke();
            else 
                OnStop?.Invoke();
        }
    }

    public GlobalStats GetStats() => stats;

    private void ReturnToHome()
    {
        Vector3 targetPoint = _homeTransform.position + (Vector3)_personalOffset;
        _offsetAnchor.transform.position = targetPoint;
        float distanceToPoint = Vector2.Distance(transform.position, targetPoint);

        if (distanceToPoint > homeStoppingDistance) OnMove?.Invoke();
        else OnStop?.Invoke();
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
        if (attacker != null) { _target = attacker; _isAttacking = false; }
    }

    private void ResetCooldown() 
    {
        _currentCooldown = CurrentCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    }
    
    public Transform GetTarget() 
    {
        if (_target != null) return _target;
        if (_homeTransform != null) return _offsetAnchor.transform;
        return null;
    }

    public string GetTargetTag() => targetTag;
    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }
    public bool GetIsAttacking() => _isAttacking;

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

    private void OnDestroy() 
    { 
        if (_offsetAnchor != null) Destroy(_offsetAnchor); 
    }
}
