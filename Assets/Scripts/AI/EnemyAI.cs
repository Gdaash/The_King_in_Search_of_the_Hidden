using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 1.2f;

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 1.5f;
    [SerializeField] private float cooldownVariation = 0.4f; 

    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private bool _isAttacking = false;
    private float _currentCooldown;
    private float _nextAttackTime;

    void Start() => ResetCooldown();

    void Update()
    {
        FindClosestTarget();

        if (_target == null) 
        { 
            OnStop?.Invoke(); 
            return; 
        }

        float distance = Vector2.Distance(transform.position, _target.position);

        if (distance <= attackRange)
        {
            OnStop?.Invoke(); // Останавливаемся перед ударом
            if (!_isAttacking && Time.time >= _nextAttackTime)
            {
                _isAttacking = true;
                OnAttack?.Invoke();
                _nextAttackTime = Time.time + _currentCooldown;
            }
        }
        else if (distance <= detectionRange && !_isAttacking)
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
            .Select(t => t.transform)
            .Where(t => t.gameObject.activeInHierarchy)
            .OrderBy(t => Vector2.SqrMagnitude(t.position - transform.position))
            .FirstOrDefault();
    }

    private void ResetCooldown() => _currentCooldown = baseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    
    public Transform GetTarget() => _target; // Реализация интерфейса
    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }
}