using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class ArcherTower : MonoBehaviour
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Enemy1"; 
    [SerializeField] private float attackRange = 10f;

    [Header("Настройки атаки")]
    [SerializeField] private float baseAttackCooldown = 1.5f;
    [SerializeField] private float cooldownVariation = 0.2f;

    [Header("События")]
    public UnityEvent OnAttack;

    private Transform _target;
    private Collider2D _targetCollider;
    private bool _isAttacking = false;
    private float _nextAttackTime;
    private float _currentCooldown;

    void Start() => ResetCooldown();

    void Update()
    {
        // Всегда ищем самого близкого врага
        FindClosestTarget();

        // Если цель есть и мы готовы к атаке
        if (_target != null && !_isAttacking && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            OnAttack?.Invoke();
            _nextAttackTime = Time.time + _currentCooldown;
        }
    }

    private void FindClosestTarget()
    {
        var targets = GameObject.FindGameObjectsWithTag(targetTag);
        
        Collider2D closest = null;
        float minDistance = attackRange;

        foreach (var t in targets)
        {
            if (!t.activeInHierarchy) continue;
            var col = t.GetComponent<Collider2D>();
            if (col == null) continue;

            float dist = Vector2.Distance(transform.position, col.ClosestPoint(transform.position));
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = col;
            }
        }

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

    public void FinishAttack() 
    { 
        _isAttacking = false; 
        ResetCooldown(); 
    }

    public Transform GetTarget() => _target;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}