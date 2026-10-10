using GameFoundation.Combat;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class ArcherTower : MonoBehaviour
{
    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; // Ссылка на ScriptableObject башни

    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Enemy1"; 
    // Если stats не назначен, будет использоваться это значение
    [SerializeField] private float defaultAttackRange = 10f;

    [Header("Настройки атаки")]
    [SerializeField] private float cooldownVariation = 0.2f;

    [Header("События")]
    public UnityEvent OnAttack;

    private Transform _target;
    private Collider2D _targetCollider;
    private bool _isAttacking = false;
    private float _nextAttackTime;
    private float _currentCooldown;

    // Свойства для получения данных из GlobalStats
    public float CurrentRange => (PortalProgression != null ? Mathf.Max(0,(PortalProgression.Balance.FindWeapon(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Bolts)?.range ?? 0)+(stats?.bonusAttackRange??0)) : (GetComponent<Combatant>()?.RangeBeforeLevel ?? (stats != null ? stats.TotalAttackRange : defaultAttackRange))) *
        (PortalProgression != null ? PortalProgression.RangeMultiplier : 1f);
    public float CurrentCooldownBase => (PortalProgression != null ? Mathf.Max(.05f,(PortalProgression.Balance.FindWeapon(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Bolts)?.cooldown ?? 1.5f)-(stats?.bonusAttackSpeed??0)) : (GetComponent<Combatant>()?.IntervalBeforeLevel ?? (stats != null ? stats.TotalCooldown : 1.5f))) /
        (PortalProgression != null ? PortalProgression.AttackSpeedMultiplier : 1f);
    public GameFoundation.MetaProgression.PortalTowerProgression PortalProgression =>
        stats != null && stats.IsPortalTower ? GameFoundation.MetaProgression.PortalTowerProgression.Instance : null;

    void Start() => ResetCooldown();

    void Update()
    {
        if (Time.timeScale <= 0) return;
        if (stats != null && !stats.CanAttack) return;
        FindClosestTarget();

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
        float minDistance = CurrentRange; // Используем динамическую дальность

        foreach (var t in targets)
        {
            if (!t.activeInHierarchy) continue;
            var health = t.GetComponentInParent<Health>();
            if (health != null && health.IsDead) continue;
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

    // Добавляем метод для визуального скрипта TowerVisuals
    public GlobalStats GetStats() => stats;

    private void ResetCooldown() 
    {
        _currentCooldown = Mathf.Max(.05f, CurrentCooldownBase + Random.Range(-cooldownVariation, cooldownVariation));
    }

    public void FinishAttack() 
    { 
        _isAttacking = false; 
        ResetCooldown(); 
    }

    public Transform GetTarget() => _target;
    private void OnDisable() { _isAttacking = false; _target = null; _targetCollider = null; }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, CurrentRange);
    }
}
