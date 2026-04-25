using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Health : MonoBehaviour
{
    [System.Serializable]
    public struct Resistance { public DamageType type; [Range(0, 2)] public float mult; }

    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats stats; 

    [Header("Визуал и Префабы")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private GameObject damageTextPrefab;

    [Header("Состояние")]
    [SerializeField] private float _cur; 

    [Header("Настройки регенерации")]
    [SerializeField] private float regenAmount = 5f;      
    [SerializeField] private float regenDelay = 5f;       

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private Color _orig;
    private bool _dead;
    private float _lastDamageTime;
    private IEnemyAI _ai; 
    private Coroutine _regenCoroutine;
    private Coroutine _flashCoroutine;

    // Максимальное здоровье теперь всегда из глобального файла
    public float MaxHealth => stats != null ? stats.TotalMaxHealth : 100f;

    void Awake() 
    {
        if (stats != null) _cur = MaxHealth;
    }

    void Start() 
    {
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) _orig = targetSprite.color;

        _ai = GetComponent<IEnemyAI>();
        _regenCoroutine = StartCoroutine(RegenTickRoutine());
    }

    private void OnEnable() 
    {
        if (stats != null) stats.OnStatsUpdated += HandleStatsUpgrade;
    }

    private void OnDisable() 
    {
        if (stats != null) stats.OnStatsUpdated -= HandleStatsUpgrade;
    }

    // Срабатывает, когда в GlobalStats что-то изменили (HP, Резисты и т.д.)
    private void HandleStatsUpgrade() 
    {
        if (_dead) return;
        
        // Если подняли MaxHP, немного подлечиваем текущее (опционально)
        if (_cur > MaxHealth) _cur = MaxHealth;

        TriggerFlash(Color.blue); 
        OnHealthChanged?.Invoke(_cur / MaxHealth);
    }

    public void TakeDamage(float amt, DamageType type, Transform attacker = null) 
    {
        if (_dead) return;

        // Множитель урона теперь берется из GlobalStats
        float multiplier = GetGlobalMultiplier(type);
        float final = amt * multiplier;
        
        if (final > 0) 
        {
            _cur = Mathf.Clamp(_cur - final, 0, MaxHealth);
            _lastDamageTime = Time.time; 
            
            TriggerFlash(Color.red);
            SpawnText(final, type);
            OnHealthChanged?.Invoke(_cur / MaxHealth);

            // Оповещаем ИИ об атаке
            var rangedAI = GetComponent<EnemyAI_Ranged>();
            var meleeAI = GetComponent<EnemyAI>();
            if (rangedAI != null) rangedAI.OnTakeDamage(attacker);
            if (meleeAI != null) meleeAI.OnTakeDamage(attacker);
        }

        if (_cur <= 0) Die();
    }

    // Новая логика получения резиста из глобального файла
    private float GetGlobalMultiplier(DamageType t)
    {
        if (stats == null) return 1f;

        // Ищем резист нужного типа в GlobalStats
        var res = stats.resistances.Find(r => r.type == t);
        
        // Если в списке GlobalStats нет такого типа урона, возвращаем 1 (полный урон)
        return res != null ? res.CurrentMult : 1f;
    }

    private IEnumerator RegenTickRoutine() 
    {
        while (!_dead) 
        {
            yield return new WaitForSeconds(1f); 

            if (_cur < MaxHealth && Time.time >= _lastDamageTime + regenDelay) 
            {
                if (IsAIAttacking()) continue;

                _cur = Mathf.Min(_cur + regenAmount, MaxHealth);
                OnHealthChanged?.Invoke(_cur / MaxHealth);
                
                TriggerFlash(Color.green);
                SpawnText(-regenAmount, DamageType.Physical); 
            }
        }
    }

    private void TriggerFlash(Color color) 
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        if (gameObject.activeInHierarchy) _flashCoroutine = StartCoroutine(Flash(color));
    }

    private IEnumerator Flash(Color c) 
    {
        if (!targetSprite) yield break;
        targetSprite.color = c;
        yield return new WaitForSeconds(0.2f);
        targetSprite.color = _orig;
    }

    private bool IsAIAttacking() 
    {
        if (_ai == null) return false;
        if (_ai is EnemyAI melee) return melee.GetIsAttacking();
        if (_ai is EnemyAI_Ranged ranged) return ranged.GetIsAttacking();
        return false;
    }

    private void SpawnText(float val, DamageType t) 
    {
        if (!damageTextPrefab || !gameObject.activeInHierarchy) return;
        var go = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        go.GetComponent<DamageText>()?.Setup(Mathf.Abs(val).ToString("F0"), val > 0 ? Color.red : Color.green);
    }

    private void Die() 
    { 
        if (_dead) return;
        _dead = true; 
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        OnDeath?.Invoke(); 
        gameObject.SetActive(false); 
    }
}
