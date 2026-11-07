using GameFoundation.Combat;
using UnityEngine;
using UnityEngine.Events;
using GameFoundation.MetaProgression;

public class EnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Настройки поиска")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool preferWorkers;
    public bool PrefersWorkers => preferWorkers;
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

    private Combatant combat;
    private EnemyMovement movement;
    private SpearmanTactics spearman;
    public float AttackSpeedMultiplier
    {
        get { if (movement == null) movement = GetComponent<EnemyMovement>(); return movement != null ? movement.AreaSlowMultiplier : 1f; }
    }
    private float nextTargetSearch, nextHomeSearch;
    private Transform _target;
    private Transform _boundaryTarget;
    private PortalMeleeBoundary _portalBoundary;
    private Transform _homeTransform;

    private bool _isAttacking = false;
    private float _currentCooldown;
    private float _nextAttackTime;
    
    // Персональная точка юнита относительно центра базы
    private Vector2 _personalOffset;
    // Якорь-пустышка для EnemyMovement
    private GameObject _offsetAnchor; 
    private bool _hasAssignedHomePoint;
    private Vector3 _assignedHomePoint;

    public bool HasAssignedHomePoint => _hasAssignedHomePoint;
    public Vector3 AssignedHomePoint => _assignedHomePoint;
    public float BaseAttackRange => combat!=null?combat.RangeBeforeLevel:attackRange;
    public float BaseAttackCooldown => combat!=null?combat.IntervalBeforeLevel:baseAttackCooldown;
    public float CooldownVariation => cooldownVariation;
    public float DetectionRange => detectionRange;

    void Awake()
    {
        combat=GetComponent<Combatant>();
        movement=GetComponent<EnemyMovement>();
        spearman=GetComponent<SpearmanTactics>();
        // Каждый юнит выбирает случайную точку в круге при рождении
        _personalOffset = Random.insideUnitCircle * positionVariation;
        
        // Создаем невидимую цель для перемещения
        _offsetAnchor = new GameObject($"Anchor_{gameObject.name}");
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
        nextTargetSearch = Time.time + CombatTargetRegistry.Stagger;
        ResetCooldown();
        _boundaryTarget = null; _portalBoundary = null;
    }

    void Start() => ResetCooldown();

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (movement != null && movement.IsStunned)
        { _nextAttackTime += Time.deltaTime; OnStop?.Invoke(); return; }
        // Advance the existing attack cooldown at the slowed rate, including mid-cooldown entry/exit.
        if (_nextAttackTime > Time.time) _nextAttackTime += Time.deltaTime * (1f - AttackSpeedMultiplier);
        FindClosestTarget();

        if (_target == null)
        {
            ValidateOrFindHome();
        }

        if (_target != null)
        {
            HandleCombat();
        }
        else if (_homeTransform != null || _hasAssignedHomePoint)
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

    private void HandleCombat()
    {
        if (spearman != null && spearman.RetreatFrom(_target)) { OnMove?.Invoke(); return; }
        float distanceToTarget = Vector2.Distance(transform.position, _target.position);

        if (_boundaryTarget != _target)
        {
            _boundaryTarget = _target;
            _portalBoundary = CompareTag("Enemy1") ? _target.GetComponentInParent<PortalMeleeBoundary>() : null;
        }
        bool canReach = _portalBoundary != null
            ? _portalBoundary.IsAtBoundary(transform.position)
            : distanceToTarget <= BaseAttackRange * MilitaryExperience.Multiplier(this);

        if (canReach)
        {
            OnStop?.Invoke();
            if (!_isAttacking && Time.time >= _nextAttackTime)
            {
                if (CompareTag("Enemy1") && ResourceWorkforce.EvacuateExposedTarget(_target))
                { _target = null; return; }
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
        Vector3 targetPoint = _hasAssignedHomePoint
            ? _assignedHomePoint
            : _homeTransform.position + (Vector3)_personalOffset;
        _offsetAnchor.transform.position = targetPoint;

        float distanceToPoint = Vector2.Distance(transform.position, targetPoint);

        if (distanceToPoint > (_hasAssignedHomePoint ? .08f : homeStoppingDistance))
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
        if (_target != null && (!CombatTargetRegistry.IsAlive(_target) ||
            (_hasAssignedHomePoint && !CompareTag("Enemy1") &&
             ((Vector2)(_target.position - _assignedHomePoint)).sqrMagnitude > detectionRange * detectionRange))) _target = null;
        if (Time.time < nextTargetSearch) return;
        nextTargetSearch = Time.time + .15f;
        _target = CompareTag("Enemy1") && targetTag == "Player"
            ? CombatTargetRegistry.ClosestHostileTarget(transform.position, detectionRange, preferWorkers)
            : CombatTargetRegistry.Closest(targetTag, transform.position, false, _hasAssignedHomePoint ? (Vector2?)_assignedHomePoint : null, detectionRange);
    }

    public void OnTakeDamage(Transform attacker = null)
    {
        if (attacker != null)
        {
            if (preferWorkers && CompareTag("Enemy1") && targetTag == "Player")
            {
                nextTargetSearch = 0;
                FindClosestTarget();
                return;
            }
            _target = attacker;
            _isAttacking = false;
        }
    }

    private void ResetCooldown() => _currentCooldown = Mathf.Max(.05f, (BaseAttackCooldown + Random.Range(-cooldownVariation, cooldownVariation)) / MilitaryExperience.Multiplier(this));
    
    // EnemyMovement берет эту цель. Если врага нет — берет персональную точку у дома
    public Transform GetTarget() 
    {
        if (_target != null) return _target;
        if (_hasAssignedHomePoint || _homeTransform != null) return _offsetAnchor.transform;
        return null;
    }

    public void FinishAttack() { _isAttacking = false; ResetCooldown(); spearman?.AttackFinished(); }

    private void OnDestroy() 
    { 
        if (_offsetAnchor != null) Destroy(_offsetAnchor); 
    }
    public bool GetIsAttacking() => _isAttacking;

    public void SetHomePoint(Vector3 worldPosition)
    {
        _target = null; _isAttacking = false; nextTargetSearch = 0;
        _hasAssignedHomePoint = true;
        _assignedHomePoint = worldPosition;
        if (_offsetAnchor != null) _offsetAnchor.transform.position = worldPosition;
    }
}
