using GameFoundation.Combat;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.MetaProgression;

public partial class Health : MonoBehaviour
{
    [System.Serializable]
    public struct Resistance { public DamageType type; [Range(0, 2)] public float mult; }

    [Header("Р“Р»РѕР±Р°Р»СЊРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё")]
    [SerializeField] private GlobalStats stats; 

    [Header("Р’РёР·СѓР°Р» Рё РџСЂРµС„Р°Р±С‹")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private GameObject damageTextPrefab;

    [Header("РЎРѕСЃС‚РѕСЏРЅРёРµ")]
    [SerializeField] private float _cur; 
    [SerializeField, Min(1f)] private float fallbackMaximumHealth = 100f;
    public void ConfigureFallbackHealth(float maximum)
    {
        if (stats != null || GetComponent<Combatant>() != null) return;
        fallbackMaximumHealth = Mathf.Max(1, maximum);
        _cur = fallbackMaximumHealth;
    }

    [Header("Р’РёР·СѓР°Р»СЊРЅР°СЏ СЂРµР°РєС†РёСЏ")]
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.16f;
    [SerializeField, Min(0f)] private float hitScaleAmount = 0.12f;
    [SerializeField, Min(0.01f)] private float deathDuration = 0.3f;

    // РЎРІРѕР№СЃС‚РІР° Р±РµСЂСѓС‚ РґР°РЅРЅС‹Рµ РёР· GlobalStats
    private float CurrentRegenAmount => (stats != null ? stats.TotalRegenAmount : 0f) * MilitaryExperience.Multiplier(this);
    private float CurrentRegenDelay => (stats != null ? stats.TotalRegenDelay : 0f) / MilitaryExperience.Multiplier(this);

    public UnityEvent<float> OnHealthChanged;
    internal event System.Action<Transform> Damaged;
    public UnityEvent OnDeath;

    private Vector3 hitBaseScale = Vector3.one;
    private bool hitScaleActive;
    private Color _orig = Color.white; 
    private bool _dead;
    private float _lastDamageTime;
    private IEnemyAI _ai; 
    private Coroutine _regenCoroutine;
    private Coroutine _flashCoroutine;
    private Transform[] combatTargets;
    private GameFoundation.Bestiary.EnemyIdentity bestiaryIdentity;
    private readonly Dictionary<MilitaryExperience, float> _contributors = new();
    private MilitaryExperience _lastContributor;

    public float Armor => (GetComponent<Combatant>()?.Armor ?? 0) +
        (GameFoundation.MetaProgression.PortalWeaponController.Instance?.DefenseFieldArmor(this) ?? 0);
    public float ShieldHealth { get; private set; }
    public float ShieldCapacity { get; private set; }

    public bool GrantShield(float amount)
    {
        if (_dead || !isActiveAndEnabled || amount <= 0 || ShieldHealth > 0) return false;
        ShieldHealth = ShieldCapacity = amount;
        OnHealthChanged?.Invoke(NormalizedHealth);
        return true;
    }
    public float MaxHealth => (GetComponent<Combatant>()?.HealthBeforeLevel ?? (stats != null ? stats.TotalMaxHealth : fallbackMaximumHealth)) * MilitaryExperience.Multiplier(this);
    public float CurrentHealth => _cur;
    public GlobalStats Stats => stats;
    public SpriteRenderer BodyRenderer => targetSprite;
    public float NormalizedHealth => MaxHealth > 0f ? Mathf.Clamp01(_cur / MaxHealth) : 0f;
    public bool IsDead => _dead;
    public int SpawnVersion { get; private set; }
    public Vector3 HitEffectPosition => targetSprite != null ? targetSprite.bounds.center : transform.position;
    public Vector3 DamageNumberPosition => targetSprite != null
        ? new Vector3(targetSprite.bounds.center.x, targetSprite.bounds.max.y + .12f, transform.position.z)
        : transform.position + Vector3.up * .7f;
    private int damageNumberSequence;

    /// <summary>Restores health without treating it as a combat hit. Used by authored recovery rules.</summary>
    public bool RestoreHealth(float amount)
    {
        if (_dead || amount <= 0f || _cur >= MaxHealth) return false;
        float previous = _cur;
        _cur = Mathf.Min(MaxHealth, _cur + amount);
        if (_cur <= previous) return false;
        OnHealthChanged?.Invoke(NormalizedHealth);
        SpawnText(-(_cur - previous), DamageType.Physical);
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
        if (CompareTag("Enemy1")) bestiaryIdentity = GetComponent<GameFoundation.Bestiary.EnemyIdentity>();
        combatTargets = GetComponentsInChildren<Transform>(true);
        _ai = GetComponent<IEnemyAI>();
        // Runtime units always begin a new spawn at full health. This must not depend on
        // a stats asset being assigned, otherwise a serialized _cur value leaks into battle.
        _cur = MaxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite)
        {
            hitBaseScale = targetSprite.transform.localScale;
            _orig = targetSprite.color;
            if (_orig.a < 0.1f) _orig.a = 1f;
        }
    }

    void Start() 
    {
        if (_regenCoroutine == null) _regenCoroutine = StartCoroutine(RegenTickRoutine());
    }

    private void OnEnable() 
    {
        GameFoundation.Audio.GameAudioController.Register(this);
        foreach (var target in combatTargets) if (target != null) CombatTargetRegistry.Register(target.gameObject);
        if (_regenCoroutine == null && !_dead) _regenCoroutine = StartCoroutine(RegenTickRoutine());
        if (stats != null) stats.OnStatsUpdated += HandleStatsUpgrade;
    }

    private void OnDisable() 
    {
        foreach (var target in combatTargets) if (target != null) CombatTargetRegistry.Unregister(target.gameObject);
        if (stats != null) stats.OnStatsUpdated -= HandleStatsUpgrade;
        if (targetSprite)
        {
            targetSprite.color = _orig;
            if (hitScaleActive) targetSprite.transform.localScale = hitBaseScale;
        }
        ResetHitEffects();
        StopAllCoroutines(); _regenCoroutine = null;
        hitScaleActive = false;
        if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }
    }

    public void ResetForSpawn()
    {
        SpawnVersion++;
        ShieldHealth = ShieldCapacity = 0;
        ResetHitEffects();
        _dead = false; _cur = MaxHealth; _lastDamageTime = Time.time;
        _contributors.Clear(); _lastContributor = null;
        _regenCoroutine = null; _flashCoroutine = null; hitScaleActive = false;
        if (targetSprite != null) { targetSprite.color = _orig; targetSprite.transform.localScale = hitBaseScale; }
        OnHealthChanged?.Invoke(1f);
    }

    private void HandleStatsUpgrade() 
    {
        if (_dead) return;
        if (_cur > MaxHealth) _cur = MaxHealth;
        OnHealthChanged?.Invoke(_cur / MaxHealth);
    }

    public void TakeDamage(float amount, DamageType presentation, Transform attacker = null) =>
        TakeDamage(amount, presentation, attacker, GameFoundation.Combat.CombatHitEffects.LightningJumpChance);

    internal void TakeDamage(float amount, DamageType presentation, Transform attacker, float lightningJumpChance)
    {
        if (TowerDarkness.Blocks(attacker)) return;
        if (_dead || amount <= 0 || !isActiveAndEnabled) return;
        float final = CombatDamage.Calculate(amount, Armor);
        if (bestiaryIdentity != null) final *= GameFoundation.Bestiary.BestiaryService.DamageMultiplier(bestiaryIdentity);
        float chainDamage = final;
        var portal = GameFoundation.MetaProgression.PortalTowerProgression.FromAttack(attacker);
        if (presentation == DamageType.Electric && portal != null)
            final *= portal.RunMultiplier(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Lightning,
                GameFoundation.MetaProgression.CombatUpgradeCatalog.Stat.FirstHit);
        ApplyResolvedDamage(final, attacker, Color.red);
        if (final > 0 && (!_dead || presentation == DamageType.Electric))
            ApplyHitEffect(presentation, presentation == DamageType.Electric ? chainDamage : final, attacker, lightningJumpChance);
    }

    internal void TakeChainLightningDamage(float damage, Transform attacker)
    {
        if (_dead || !isActiveAndEnabled) return;
        float resolved = CombatDamage.Calculate(damage, Armor);
        if (bestiaryIdentity != null) resolved *= GameFoundation.Bestiary.BestiaryService.DamageMultiplier(bestiaryIdentity);
        ApplyResolvedDamage(resolved, attacker, DamagePalette.Electric);
    }

    // Secondary effects use the damage already resolved against armor. They cannot trigger new effects.
    private void ApplyResolvedDamage(float final, Transform attacker, Color flashColor)
    {
        if (TowerDarkness.Blocks(attacker)) return;
        if (_dead || final <= 0 || !isActiveAndEnabled) return;
        MilitaryExperience contributor = attacker != null ? attacker.GetComponentInParent<MilitaryExperience>() : null;
        if (contributor != null)
        {
            _contributors.TryGetValue(contributor, out float dealt);
            _contributors[contributor] = dealt + final;
            _lastContributor = contributor;
        }
        float shieldAbsorbed = Mathf.Min(ShieldHealth, final);
        ShieldHealth -= shieldAbsorbed;
        float previousHealth = _cur;
        _cur = Mathf.Clamp(_cur - (final - shieldAbsorbed), 0, MaxHealth);
        _lastDamageTime = Time.time;
        TriggerFlash(flashColor);
        if (_cur < previousHealth) Damaged?.Invoke(attacker);
        SpawnText(previousHealth - _cur + shieldAbsorbed, DamageType.Physical);
        OnHealthChanged?.Invoke(NormalizedHealth);
        var rangedAI = GetComponent<EnemyAI_Ranged>();
        var meleeAI = GetComponent<EnemyAI>();
        if (rangedAI != null) rangedAI.OnTakeDamage(attacker);
        if (meleeAI != null) meleeAI.OnTakeDamage(attacker);
        if (rangedAI == null && meleeAI == null) _ai?.OnTakeDamage(attacker);
        if (_cur <= 0) Die();
    }

    // Compatibility for older inspectors and validation scripts; there are no resistance categories now.
    public float ResistanceMultiplier(DamageType unused) => 1f;

    private IEnumerator RegenTickRoutine() 
    {
        while (!_dead) 
        {
            yield return new WaitForSeconds(1f); 

            // РџР РћР’Р•Р РљРђ: Р•СЃР»Рё Р»РµС‡РµРЅРёРµ 0 РёР»Рё Р·Р°РґРµСЂР¶РєР° 0 вЂ” РїСЂРѕРїСѓСЃРєР°РµРј С†РёРєР»
            if (CurrentRegenAmount <= 0 || CurrentRegenDelay <= 0) continue;

            if (_cur < MaxHealth && Time.time >= _lastDamageTime + CurrentRegenDelay) 
            {
                if (IsAIAttacking()) continue;

                if (RestoreHealth(CurrentRegenAmount)) TriggerFlash(Color.green);
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
        Vector3 baseScale = hitBaseScale;
        baseScale.x = Mathf.Abs(baseScale.x) * Mathf.Sign(visual.localScale.x);
        // Ranged windup owns this transform's scale. A hit must not capture its
        // temporary stretch and restore it after the shooting animation ends.
        bool animateScale = _ai != null && !TryGetComponent<EnemyVisuals_Ranged>(out _);
        hitScaleActive = animateScale;
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
        hitScaleActive = false;
        _flashCoroutine = null;
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
        if (!gameObject.activeInHierarchy || Mathf.Approximately(val, 0)) return;
        DamageText.Show(this, val, damageNumberSequence++, damageTextPrefab);
    }

    private void Die() 
    {
        if (_dead) return;
        _dead = true; 
        ResetHitEffects();
        GetComponent<GameFoundation.MetaProgression.WorldLossIdentity>()?.RecordDeath();
        if (bestiaryIdentity != null) GameFoundation.Bestiary.BestiaryService.RegisterKill(bestiaryIdentity);
        if (CompareTag("Enemy1") && bestiaryIdentity != null && GameFoundation.MetaProgression.PortalTowerProgression.Instance?.UsesArsenal == true)
            GameFoundation.MetaProgression.PortalArsenalProgress.RecordKill(bestiaryIdentity.Id);
        if (CompareTag("Enemy1")) GameFoundation.MetaProgression.PortalTowerProgression.Instance?.AwardEnemyExperience(transform.position);
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
