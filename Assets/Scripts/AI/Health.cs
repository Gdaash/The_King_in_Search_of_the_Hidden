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

    // Свойства берут данные из GlobalStats
    private float CurrentRegenAmount => stats != null ? stats.TotalRegenAmount : 0f;
    private float CurrentRegenDelay => stats != null ? stats.TotalRegenDelay : 0f;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private Color _orig = Color.white; 
    private bool _dead;
    private float _lastDamageTime;
    private IEnemyAI _ai; 
    private Coroutine _regenCoroutine;
    private Coroutine _flashCoroutine;

    public float MaxHealth => stats != null ? stats.TotalMaxHealth : 100f;

    void Awake() 
    {
        if (stats != null) _cur = MaxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) 
        {
            _orig = targetSprite.color;
            if (_orig.a < 0.1f) _orig.a = 1f;
        }
    }

    void Start() 
    {
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
        if (targetSprite) targetSprite.color = _orig;
    }

    private void HandleStatsUpgrade() 
    {
        if (_dead) return;
        if (_cur > MaxHealth) _cur = MaxHealth;
        OnHealthChanged?.Invoke(_cur / MaxHealth);
    }

    public void TakeDamage(float amt, DamageType type, Transform attacker = null) 
    {
        if (_dead) return;
        float multiplier = GetGlobalMultiplier(type);
        float final = amt * multiplier;
        
        if (final > 0) 
        {
            _cur = Mathf.Clamp(_cur - final, 0, MaxHealth);
            _lastDamageTime = Time.time; 
            TriggerFlash(Color.red); 
            SpawnText(final, type);
            OnHealthChanged?.Invoke(_cur / MaxHealth);

            var rangedAI = GetComponent<EnemyAI_Ranged>();
            var meleeAI = GetComponent<EnemyAI>();
            if (rangedAI != null) rangedAI.OnTakeDamage(attacker);
            if (meleeAI != null) meleeAI.OnTakeDamage(attacker);
        }
        if (_cur <= 0) Die();
    }

    private float GetGlobalMultiplier(DamageType t)
    {
        if (stats == null) return 1f;
        var res = stats.resistances.Find(r => r.type == t);
        return res != null ? res.CurrentMult : 1f;
    }

    private IEnumerator RegenTickRoutine() 
    {
        while (!_dead) 
        {
            yield return new WaitForSeconds(1f); 

            // ПРОВЕРКА: Если лечение 0 или задержка 0 — пропускаем цикл
            if (CurrentRegenAmount <= 0 || CurrentRegenDelay <= 0) continue;

            if (_cur < MaxHealth && Time.time >= _lastDamageTime + CurrentRegenDelay) 
            {
                if (IsAIAttacking()) continue;
                
                _cur = Mathf.Min(_cur + CurrentRegenAmount, MaxHealth);
                OnHealthChanged?.Invoke(_cur / MaxHealth);
                
                // Включаем визуал лечения только если реально что-то восстановили
                TriggerFlash(Color.green);
                SpawnText(-CurrentRegenAmount, DamageType.Physical); 
            }
        }
    }

    private void TriggerFlash(Color color) 
    {
        if (_dead || !gameObject.activeInHierarchy) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(Flash(color));
    }

    private IEnumerator Flash(Color c) 
    {
        if (!targetSprite) yield break;
        float currentAlpha = targetSprite.color.a;
        Color flashColor = c;
        flashColor.a = currentAlpha;
        targetSprite.color = flashColor;
        yield return new WaitForSeconds(0.2f);
        Color resetColor = _orig;
        resetColor.a = currentAlpha;
        targetSprite.color = resetColor;
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
        if (targetSprite) targetSprite.color = _orig; 
        OnDeath?.Invoke(); 
        gameObject.SetActive(false); 
    }
}
