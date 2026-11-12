using System.Collections.Generic;
using GameFoundation.Combat;
using UnityEngine;

public partial class Health
{
    private struct EchoHit { public float at, damage; public Transform attacker; public Color color; }
    private readonly List<EchoHit> echoes = new();
    private EnemyMovement effectMovement;
    private float stunnedUntil, stunImmuneUntil, chilledUntil;
    private float burnUntil, nextBurnTick, burnTickDamage;
    private Transform burnAttacker;
    private CombatStatusVfx hitVfx;
    private CombatStatusVfx HitVfx => hitVfx != null ? hitVfx : hitVfx = CombatStatusVfx.Attach(this, targetSprite);
    public bool IsStunned => !_dead && Time.time < stunnedUntil;
    public bool IsBurning => !_dead && burnTickDamage > 0 && Time.time <= burnUntil;

    private void ApplyHitEffect(DamageType presentation, float resolvedDamage, Transform attacker, float lightningJumpChance)
    {
        if (effectMovement == null) effectMovement = GetComponent<EnemyMovement>();
        var portal = GameFoundation.MetaProgression.PortalTowerProgression.FromAttack(attacker);
        float controlDuration = portal?.RunMultiplier(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.FrostSeal,
            GameFoundation.MetaProgression.CombatUpgradeCatalog.Stat.Control) ?? 1;
        switch (presentation)
        {
            case DamageType.Physical:
                if (effectMovement != null && Time.time >= stunImmuneUntil &&
                    CombatHitEffects.Procs(Random.value, CombatHitEffects.StunChance))
                {
                    stunnedUntil = Time.time + CombatHitEffects.StunSeconds * controlDuration;
                    stunImmuneUntil = stunnedUntil + CombatHitEffects.StunRecoverySeconds;
                    if (TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
                    HitVfx.ShowStun(stunnedUntil);
                }
                break;
            case DamageType.Fire:
                if (!CombatHitEffects.Procs(Random.value, CombatHitEffects.IgniteChance)) break;
                float tickDamage = resolvedDamage * CombatHitEffects.BurnDamageFraction;
                if (!IsBurning)
                {
                    burnTickDamage = tickDamage;
                    burnAttacker = attacker;
                    nextBurnTick = Time.time + CombatHitEffects.BurnTickSeconds;
                }
                else if (tickDamage > burnTickDamage)
                {
                    burnTickDamage = tickDamage;
                    burnAttacker = attacker;
                }
                // Refresh duration without stacking damage or postponing the pending tick.
                burnUntil = Time.time + CombatHitEffects.BurnSeconds;
                HitVfx.ShowBurn(burnUntil);
                break;
            case DamageType.Ice:
                if (effectMovement == null) break;
                chilledUntil = Time.time + CombatHitEffects.ChillSeconds * controlDuration;
                effectMovement.SetAreaSlow(this, CombatHitEffects.ChillFraction);
                break;
            case DamageType.Electric:
                HitVfx.ElectricBurst();
                CombatChainLightning.Resolve(this, resolvedDamage, attacker, chance: lightningJumpChance);
                break;
            case DamageType.Magic:
                if (CombatHitEffects.Procs(Random.value, CombatHitEffects.EchoChance))
                {
                    HitVfx.ShowEcho();
                    echoes.Add(new EchoHit { at = Time.time + CombatHitEffects.EchoDelaySeconds,
                        damage = resolvedDamage * CombatHitEffects.EchoDamageFraction, attacker = attacker,
                        color = DamagePalette.Magic });
                }
                break;
        }
    }

    private void Update()
    {
        if (_dead || Time.deltaTime <= 0) return;
        if (chilledUntil > 0 && Time.time >= chilledUntil)
        {
            effectMovement?.SetAreaSlow(this, 0);
            chilledUntil = 0;
        }
        // A scheduled final burn tick still runs when a long frame crosses the end time.
        while (burnTickDamage > 0 && nextBurnTick <= burnUntil && Time.time >= nextBurnTick)
        {
            nextBurnTick += CombatHitEffects.BurnTickSeconds;
            ApplyResolvedDamage(burnTickDamage, burnAttacker, DamagePalette.Fire);
            if (_dead) return;
            HitVfx.BurnTick();
        }
        if (burnTickDamage > 0 && Time.time > burnUntil) { burnTickDamage = 0; burnAttacker = null; }
        for (int i = echoes.Count - 1; i >= 0; i--)
        {
            if (Time.time < echoes[i].at) continue;
            var hit = echoes[i];
            echoes.RemoveAt(i);
            ApplyResolvedDamage(hit.damage, hit.attacker, hit.color);
            if (_dead) return;
            HitVfx.EchoBurst();
        }
    }

    private void ResetHitEffects()
    {
        stunnedUntil = stunImmuneUntil = chilledUntil = burnUntil = nextBurnTick = burnTickDamage = 0;
        burnAttacker = null;
        echoes.Clear();
        if (hitVfx != null) hitVfx.Clear();
        effectMovement?.SetAreaSlow(this, 0);
    }
}
