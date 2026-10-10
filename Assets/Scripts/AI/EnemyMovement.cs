using GameFoundation.Combat;
using UnityEngine;
using GameFoundation.MetaProgression;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; // Ссылка на ScriptableObject

    [Header("Настройки движения")]
    [SerializeField] private float speedVariation = 0.7f;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Combatant combat;
    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;
    private float _individualVariation; // Персональное отклонение скорости
    public GlobalStats Stats => stats;
    public float SpeedVariation => speedVariation;
    // FixedUpdate currently uses this speed; the UI must describe actual movement.
    public float CombatSpeed => _finalSpeed > 0f ? _finalSpeed : (combat!=null?combat.SpeedBeforeLevel:(stats != null ? stats.TotalSpeed : 3f)) + _individualVariation;

    public float CurrentSpeed
    {
        get
        {
            if (_finalSpeed <= 0f) UpdateSpeed();
            return Mathf.Max(0.1f, _finalSpeed * MilitaryExperience.Multiplier(this));
        }
    }

    void Awake()
    {
        combat=GetComponent<Combatant>();
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<IEnemyAI>(); 
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Генерируем вариативность один раз при рождении
        _individualVariation = Random.Range(-speedVariation, speedVariation);
    }

    void Start() 
    {
        UpdateSpeed();
    }

    private void OnEnable() 
    {
        // Подписываемся на обновление, чтобы скорость менялась мгновенно при покупке улучшения
        if (stats != null) stats.OnStatsUpdated += UpdateSpeed;
    }

    private void OnDisable() 
    {
        if (stats != null) stats.OnStatsUpdated -= UpdateSpeed;
    }

    private void UpdateSpeed()
    {
        if(combat!=null) _finalSpeed=combat.SpeedBeforeLevel+_individualVariation;
        else if (stats != null)
        {
            // Итоговая скорость = Глобальная (База + Бонус) + личная вариативность
            _finalSpeed = stats.TotalSpeed + _individualVariation;
        }
        else
        {
            _finalSpeed = 3f + _individualVariation; // Дефолт, если забыли подкинуть SO
        }
    }

    public void SetMove(bool state) 
    {
        _canMove = state;
        if (!state) 
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = _rb.position;

        Vector2 direction = (targetPos - currentPos);
        float distance = direction.magnitude;

        if (distance < 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDirection = direction.normalized;
        if (CompareTag("Player"))
            moveDirection = PlayerHexNavigation.ResolveDirection(currentPos, moveDirection);
        _rb.linearVelocity = moveDirection * _finalSpeed;
        HandleFlip();
    }

    private void HandleFlip()
    {
        if (Mathf.Abs(_rb.linearVelocity.x) > flipThreshold)
        {
            float newScaleX = _rb.linearVelocity.x > 0 ? _initialScaleX : -_initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }
}
