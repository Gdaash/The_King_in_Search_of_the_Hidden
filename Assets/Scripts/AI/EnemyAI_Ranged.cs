using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class EnemyAI_Ranged : MonoBehaviour
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 8f;   
    [SerializeField] private float stopRange = 6f;     

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 2f;
    [SerializeField] private float cooldownVariation = 0.5f; 

    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private bool _isAttacking = false;
    private float _nextAttackTime;
    private float _currentCooldown;

    void Start() => ResetCooldown();

    void Update()
    {
        if (_isAttacking) return;
        FindClosestTarget();

        if (_target == null) { OnStop?.Invoke(); return; }

        float distance = Vector2.Distance(transform.position, _target.position);

        if (distance <= attackRange && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            OnAttack?.Invoke();
            _nextAttackTime = Time.time + _currentCooldown;
        }

        if (distance > stopRange && distance <= detectionRange)
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
            .Where(obj => obj.activeInHierarchy)
            .OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position))
            .FirstOrDefault()?.transform;
    }

    private void ResetCooldown() => _currentCooldown = baseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    
    // ЭТОТ МЕТОД НУЖЕН ДЛЯ ИСПРАВЛЕНИЯ ОШИБКИ
    public string GetTargetTag() => targetTag;

    public Transform GetTarget() => _target;
    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }
}