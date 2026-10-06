using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.MetaProgression;

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

    [Header("Визуальная реакция")]
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.16f;
    [SerializeField, Min(0f)] private float hitScaleAmount = 0.12f;
    [SerializeField, Min(0.01f)] private float deathDuration = 0.3f;

    // Свойства берут данные из GlobalStats
    private float CurrentRegenAmount => (stats != null ? stats.TotalRegenAmount : 0f) * MilitaryExperience.Multiplier(this);
    private float CurrentRegenDelay => (stats != null ? stats.TotalRegenDelay : 0f) / MilitaryExperience.Multiplier(this);

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private Color _orig = Color.white; 
    private bool _dead;
    private float _lastDamageTime;
    private IEnemyAI _ai; 
    private Coroutine _regenCoroutine;
    private Coroutine _flashCoroutine;
    private readonly Dictionary<MilitaryExperience, float> _contributors = new();
    private MilitaryExperience _lastContributor;

    public float MaxHealth => (stats != null ? stats.TotalMaxHealth : 100f) * MilitaryExperience.Multiplier(this);
    public float CurrentHealth => _cur;
    public GlobalStats Stats => stats;
    public float NormalizedHealth => MaxHealth > 0f ? Mathf.Clamp01(_cur / MaxHealth) : 0f;
    public bool IsDead => _dead;

    /// <summary>Restores health without treating it as a combat hit. Used by authored recovery rules.</summary>
    public bool RestoreHealth(float amount)
    {
        if (_dead || amount <= 0f || _cur >= MaxHealth) return false;
        float previous = _cur;
        _cur = Mathf.Min(MaxHealth, _cur + amount);
        if (_cur <= previous) return false;
        OnHealthChanged?.Invoke(NormalizedHealth);
        return true;
    }

    /// <summary>Applies persisted unit health after its maximum health is configured.</summary>
    public void SetNormalizedHealth(float normalizedHealth)
    {
        if (_dead) return;
        _cur = MaxHealth * Mathf.Clamp01(normalizedHealth);
        OnHealthChanged?.Invoke(NormalizedHealth);
    }

    void Awake() 
    {
        _ai = GetComponent<IEnemyAI>();
        // Runtime units always begin a new spawn at full health. This must not depend on
        // a stats asset being assigned, otherwise a serialized _cur value leaks into battle.
        _cur = MaxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) 
        {
            _orig = targetSprite.color;
            if (_orig.a < 0.1f) _orig.a = 1f;
        }
    }

    void Start() 
    {
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
            MilitaryExperience contributor = attacker != null ? attacker.GetComponentInParent<MilitaryExperience>() : null;
            if (contributor != null)
            {
                _contributors.TryGetValue(contributor, out float dealt);
                _contributors[contributor] = dealt + final;
                _lastContributor = contributor;
            }
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
        Transform visual = targetSprite.transform;
        Vector3 baseScale = visual.localScale;
        bool animateScale = _ai != null;
        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / hitFlashDuration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            if (animateScale) visual.localScale = baseScale * (1f + pulse * hitScaleAmount);
            targetSprite.color = Color.Lerp(c, _orig, normalized);
            yield return null;
        }
        Color resetColor = _orig;
        resetColor.a = currentAlpha;
        targetSprite.color = resetColor;
        if (animateScale) visual.localScale = baseScale;
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
        AwardMilitaryExperience();
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.SetMove(false);
        EnemyAI melee = GetComponent<EnemyAI>();
        if (melee != null) melee.enabled = false;
        EnemyAI_Ranged ranged = GetComponent<EnemyAI_Ranged>();
        if (ranged != null) ranged.enabled = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        StartCoroutine(DeathRoutine());
    }

    private void AwardMilitaryExperience()
    {
        foreach (MilitaryExperience contributor in _contributors.Keys.ToArray())
        {
            if (contributor == null) continue;
            if (contributor == _lastContributor) contributor.AwardKill();
            else contributor.AwardAssist();
        }
        _contributors.Clear();
        _lastContributor = null;
    }

    private IEnumerator DeathRoutine()
    {
        if (targetSprite != null)
        {
            Transform visual = targetSprite.transform;
            Vector3 startScale = visual.localScale;
            Color startColor = targetSprite.color;
            float elapsed = 0f;
            while (elapsed < deathDuration)
            {
                elapsed += Time.deltaTime;
                float t = deathDuration > 0f ? Mathf.Clamp01(elapsed / deathDuration) : 1f;
                float eased = t * t * (3f - 2f * t);
                visual.localScale = Vector3.LerpUnclamped(startScale, startScale * 0.35f, eased);
                Color color = Color.Lerp(startColor, new Color(0.72f, 0.72f, 0.72f, 0f), eased);
                targetSprite.color = color;
                yield return null;
            }
        }
        using (GameFoundation.UI.GameNotifications.BeginAction())
        {
            GameFoundation.UI.GameNotifications.Death(this);
            OnDeath?.Invoke();
        }
        gameObject.SetActive(false);
    }
}
