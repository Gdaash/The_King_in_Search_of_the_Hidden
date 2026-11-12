using GameFoundation.Combat;
using UnityEngine;
using UnityEngine.Events;
using GameFoundation.MetaProgression;
using GameFoundation.Base;

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

    private Combatant combat;
    private EnemyMovement movement;
    private EyeHexSealer hexSealer;
    public float AttackSpeedMultiplier
    {
        get { if (movement == null) movement = GetComponent<EnemyMovement>(); return movement != null ? movement.AreaSlowMultiplier : 1f; }
    }
    private float nextTargetSearch, nextHomeSearch;
    private Transform _target;
    private Transform _homeTransform;
    
    private bool _isAttacking = false;
    private float _nextAttackTime;
    private float _currentCooldown;
    
    private Vector2 _personalOffset;
    private GameObject _offsetAnchor;
    private bool _hasAssignedHomePoint;
    private Vector3 _assignedHomePoint;
    private bool retreatRequested;
    private bool tacticalRetreat;
    private float nextThreatCheck;
    private readonly System.Collections.Generic.List<Health> nearbyEnemies = new();
    private bool IsFriendlyRanged => CompareTag("Player") && combat != null;
    public bool IsTacticalRetreat => tacticalRetreat;

    public bool HasAssignedHomePoint => _hasAssignedHomePoint;
    public Vector3 AssignedHomePoint => _assignedHomePoint;
    public float CooldownVariation => cooldownVariation;
    public float DetectionRange => detectionRange;

    // Свойства для удобного доступа к статам
    public float CurrentAttackRange => (combat!=null?combat.RangeBeforeLevel:(stats != null ? stats.TotalAttackRange : defaultAttackRange)) * MilitaryExperience.Multiplier(this);
    public float CurrentStopRange => (combat!=null?combat.RangeBeforeLevel*.75f:(stats != null ? (stats.TotalAttackRange * 0.75f) : defaultStopRange)) * MilitaryExperience.Multiplier(this);
    public float CurrentCooldown => (combat!=null?combat.IntervalBeforeLevel:(stats != null ? stats.TotalCooldown : 2f)) / MilitaryExperience.Multiplier(this);

    void Awake()
    {
        combat=GetComponent<Combatant>();
        movement=GetComponent<EnemyMovement>();
        hexSealer=GetComponent<EyeHexSealer>();
        _personalOffset = Random.insideUnitCircle * positionVariation;
        _offsetAnchor = new GameObject($"RangedAnchor_{gameObject.name}");
    }

    private void OnEnable()
    {
        GameFoundation.Audio.GameAudioController.Register(this);
        nextTargetSearch = Time.time + CombatTargetRegistry.Stagger;
    }

    public void ResetForSpawn()
    {
        _target = null; _homeTransform = null; _isAttacking = false; _nextAttackTime = 0;
        _hasAssignedHomePoint = false; nextHomeSearch = 0;
        retreatRequested = tacticalRetreat = false; nextThreatCheck = 0;
        nextTargetSearch = Time.time + CombatTargetRegistry.Stagger;
        ResetCooldown();
    }

    void Start() => ResetCooldown();

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (TryGetComponent<DarkMageAbility>(out var darkMage) && darkMage.IsCasting) { OnStop?.Invoke(); return; }
        if (movement != null && movement.IsStunned)
        { _nextAttackTime += Time.deltaTime; OnStop?.Invoke(); return; }
        // Advance the existing attack cooldown at the slowed rate, including mid-cooldown entry/exit.
        if (_nextAttackTime > Time.time) _nextAttackTime += Time.deltaTime * (1f - AttackSpeedMultiplier);
        FindClosestTarget();

        if (UpdateTacticalRetreat()) return;

        if (hexSealer != null && hexSealer.ControlsMovement)
        {
            if (_target != null && Vector2.Distance(transform.position, _target.position) <= CurrentAttackRange) HandleCombat(false);
            return;
        }

        if (_target == null) ValidateOrFindHome();

        if (_target != null) HandleCombat();
        else if (_homeTransform != null || _hasAssignedHomePoint) ReturnToHome();
        else OnStop?.Invoke();
    }

    private void HandleCombat(bool controlMovement = true)
    {
        float distanceToTarget = Vector2.Distance(transform.position, _target.position);

        // Movement events retain their previous state until explicitly stopped.
        // Stop at firing range, including windup, recovery and cooldown.
        if (controlMovement && (_isAttacking || distanceToTarget <= CurrentAttackRange)) OnStop?.Invoke();
        if (distanceToTarget <= CurrentAttackRange && TryGetComponent<DarkMageAbility>(out var darkMage) && darkMage.TryBeginFirstCast(_target)) return;

        // Используем CurrentAttackRange из глобальных статов
        if (!_isAttacking && distanceToTarget <= CurrentAttackRange && Time.time >= _nextAttackTime)
        {
            if (CompareTag("Enemy1") && ResourceWorkforce.EvacuateExposedTarget(_target))
            { _target = null; return; }
            _isAttacking = true;
            OnAttack?.Invoke();
            
            // Рассчитываем время следующей атаки на основе CurrentCooldown
            _nextAttackTime = Time.time + CurrentCooldown + Random.Range(-cooldownVariation, cooldownVariation);
        }

        if (controlMovement && !_isAttacking)
        {
            // Pursue only while the target is beyond firing range.
            if (distanceToTarget > CurrentAttackRange && distanceToTarget <= detectionRange)
                OnMove?.Invoke();
            else 
                OnStop?.Invoke();
        }
    }

    public GlobalStats GetStats() => stats;

    private void ReturnToHome()
    {
        Vector3 targetPoint = _hasAssignedHomePoint
            ? _assignedHomePoint
            : _homeTransform.position + (Vector3)_personalOffset;
        _offsetAnchor.transform.position = targetPoint;
        float distanceToPoint = Vector2.Distance(transform.position, targetPoint);

        if (distanceToPoint > (_hasAssignedHomePoint ? .08f : homeStoppingDistance)) OnMove?.Invoke();
        else OnStop?.Invoke();
    }

    private void FindClosestTarget()
    {
        if (_target != null && (!CombatTargetRegistry.IsAlive(_target) || (_hasAssignedHomePoint && !CompareTag("Enemy1") && ((Vector2)(_target.position - _assignedHomePoint)).sqrMagnitude > detectionRange * detectionRange))) _target = null;
        if (Time.time < nextTargetSearch) return;
        nextTargetSearch = Time.time + .15f;
        _target = CompareTag("Enemy1") && targetTag == "Player"
            ? CombatTargetRegistry.ClosestHostileTarget(transform.position, hexSealer != null ? CurrentAttackRange : detectionRange)
            : CombatTargetRegistry.Closest(targetTag, transform.position, targetTag == "Enemy1" && RoyalDecreeService.IsEnabled(RoyalDecreeService.FinishOffEnemies), _hasAssignedHomePoint ? (Vector2?)_assignedHomePoint : null, detectionRange);
        if (hexSealer != null && _target != null && Vector2.Distance(transform.position, _target.position) > CurrentAttackRange) _target = null;
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (IsFriendlyRanged) retreatRequested = true;
        if (attacker != null) { _target = attacker; _isAttacking = false; }
    }

    private bool UpdateTacticalRetreat()
    {
        if (!IsFriendlyRanged) return false;
        ValidateOrFindHome();
        if (!_hasAssignedHomePoint && _homeTransform == null) return false;
        // Assigned formation slots are inside the military hex (or outside the portal footprint).
        Vector3 home = _hasAssignedHomePoint ? _assignedHomePoint : _homeTransform.position + (Vector3)_personalOffset;
        if (Vector2.Distance(transform.position, home) <= homeStoppingDistance)
        {
            bool wasRetreating = tacticalRetreat;
            retreatRequested = tacticalRetreat = false;
            if (wasRetreating) OnStop?.Invoke();
            return false;
        }

        if (!retreatRequested && !_isAttacking && Time.time < _nextAttackTime && Time.time >= nextThreatCheck)
        {
            nextThreatCheck = Time.time + .15f;
            CombatTargetRegistry.CollectHealth("Enemy1", nearbyEnemies);
            foreach (var enemy in nearbyEnemies)
            {
                // Use the nearby attacker's actual reach, not the archer's shooting range.
                var melee = enemy.GetComponent<EnemyAI>();
                var ranged = enemy.GetComponent<EnemyAI_Ranged>();
                float reach = melee != null ? melee.BaseAttackRange * MilitaryExperience.Multiplier(melee)
                    : ranged != null ? ranged.CurrentAttackRange : 0f;
                if (reach > 0f && Vector2.Distance(transform.position, enemy.transform.position) <= reach + .15f)
                { retreatRequested = true; break; }
            }
        }
        if (!retreatRequested) return false;
        if (!tacticalRetreat)
        {
            // Cancel a damaged archer's windup before movement can change its aiming target.
            GetComponent<EnemyVisuals_Ranged>()?.BeginTravel();
            _isAttacking = false;
            tacticalRetreat = true;
        }
        _offsetAnchor.transform.position = home;
        OnMove?.Invoke();
        return true;
    }

    private void ResetCooldown() 
    {
        _currentCooldown = CurrentCooldown + Random.Range(-cooldownVariation, cooldownVariation);
    }
    
    public Transform GetTarget() 
    {
        if (tacticalRetreat) return _offsetAnchor.transform;
        if (_target != null) return _target;
        if (_hasAssignedHomePoint || _homeTransform != null) return _offsetAnchor.transform;
        return null;
    }

    public string GetTargetTag() => targetTag;
    public void FinishAttack() { _isAttacking = false; ResetCooldown(); }
    public bool GetIsAttacking() => _isAttacking;

    private void ValidateOrFindHome()
    {
        if (_hasAssignedHomePoint)
        {
            _offsetAnchor.transform.position = _assignedHomePoint;
            return;
        }
        if (_homeTransform == null || !_homeTransform.gameObject.activeInHierarchy)
        {
            if (Time.time < nextHomeSearch) return;
            nextHomeSearch = Time.time + .5f;
            _homeTransform = CombatTargetRegistry.ClosestHome(transform.position);
        }
    }

    private void OnDestroy() 
    { 
        if (_offsetAnchor != null) Destroy(_offsetAnchor); 
    }

    public void SetHomePoint(Vector3 worldPosition)
    {
        retreatRequested = tacticalRetreat = false;
        _target = null; _isAttacking = false; nextTargetSearch = 0;
        _hasAssignedHomePoint = true;
        _assignedHomePoint = worldPosition;
        if (_offsetAnchor != null) _offsetAnchor.transform.position = worldPosition;
    }
}
