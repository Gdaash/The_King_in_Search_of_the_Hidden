using GameFoundation.Combat;
using UnityEngine;
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
    public float NextAttackTime => _nextAttackTime;
    private float _currentCooldown;

    // Свойства для получения данных из GlobalStats
    public float CurrentRange => PortalProgression != null
        ? PortalProgression.WeaponRange(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Bolts, stats?.bonusAttackRange ?? 0)
        : (GetComponent<Combatant>()?.RangeBeforeLevel ?? (stats != null ? stats.TotalAttackRange : defaultAttackRange));
    public float CurrentCooldownBase => PortalProgression != null
        ? PortalProgression.AttackInterval(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Bolts, stats?.bonusAttackSpeed ?? 0)
        : (GetComponent<Combatant>()?.IntervalBeforeLevel ?? (stats != null ? stats.TotalCooldown : 1.5f));
    public GameFoundation.MetaProgression.PortalTowerProgression PortalProgression =>
        stats != null && stats.IsPortalTower ? GameFoundation.MetaProgression.PortalTowerProgression.Instance : null;

    private void OnEnable() => GameFoundation.Audio.GameAudioController.Register(this);

    void Start() => ResetCooldown();

    void Update()
    {
        if (TowerDarkness.Blocks(transform)) return;
        if (Time.timeScale <= 0 || (TryGetComponent<Health>(out var health) && health.IsStunned)) return;
        if (stats != null && !stats.CanAttack) return;
        FindClosestTarget();

        if (_target != null && !_isAttacking && Time.time >= _nextAttackTime)
        {
            _isAttacking = true;
            _nextAttackTime = Time.time + (PortalProgression != null ? CurrentCooldownBase : _currentCooldown);
            OnAttack?.Invoke();
        }
    }

    private void FindClosestTarget()
    {
        _targetCollider = CombatTargetRegistry.ClosestCollider(targetTag, transform.position, CurrentRange);
        _target = _targetCollider != null ? _targetCollider.transform : null;
    }

    // Добавляем метод для визуального скрипта TowerVisuals
    public GlobalStats GetStats() => stats;

    private void ResetCooldown() 
    {
        _currentCooldown = PortalProgression != null ? CurrentCooldownBase : Mathf.Max(.05f, CurrentCooldownBase + Random.Range(-cooldownVariation, cooldownVariation));
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
