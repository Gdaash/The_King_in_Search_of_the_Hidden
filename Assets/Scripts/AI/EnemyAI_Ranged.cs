using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System.Collections;

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

    [Header("Настройки отбегания (Flee)")]
    [SerializeField] private float fleeDuration = 1.5f;     
    [SerializeField] private float fleeCooldown = 4f;       
    
    [Header("События состояний")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   
    public UnityEvent OnAttack; 

    private Transform _target;
    private bool _isAttacking = false;
    private bool _isFleeing = false;
    private float _nextAttackTime;
    private float _nextFleeAllowedTime;
    private float _currentCooldown;

    void Start() => ResetCooldown();

    void Update()
    {
        FindClosestTarget();
        if (_target == null) { OnStop?.Invoke(); return; }
        if (_isFleeing) return;

        float distance = Vector2.Distance(transform.position, _target.position);

        if (!_isAttacking && distance <= attackRange && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            OnAttack?.Invoke();
            _nextAttackTime = Time.time + _currentCooldown;
        }

        if (!_isAttacking)
        {
            if (distance > stopRange && distance <= detectionRange) OnMove?.Invoke();
            else OnStop?.Invoke();
        }
    }

    public void OnTakeDamage()
    {
        if (Time.time >= _nextFleeAllowedTime && !_isFleeing) StartCoroutine(FleeRoutine());
    }

    private IEnumerator FleeRoutine()
    {
        _isFleeing = true;
        _isAttacking = false; 
        _nextFleeAllowedTime = Time.time + fleeCooldown;
        OnMove?.Invoke(); 
        yield return new WaitForSeconds(fleeDuration);
        _isFleeing = false;
        ResetCooldown();
    }

    public bool IsFleeing() => _isFleeing;
    public Transform GetTarget() => _target;
    public string GetTargetTag() => targetTag;
    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }

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
}