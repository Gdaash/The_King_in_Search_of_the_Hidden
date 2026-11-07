using System.Collections.Generic;
using GameFoundation.Audio;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class PortalWeaponController
    {
        [Header("Морозная печать")]
        public Sprite frostSealSprite;
        [SerializeField, Range(.1f, 1)] private float frostSealArtWidth = 1;
        private float frostTime;
        private sealed class FrostZone
        {
            public SpriteRenderer renderer;
            public ParticleSystem snow;
            public float snowRadius;
            public Vector2 center;
            public float age, nextTick;
            public HashSet<EnemyMovement> slowed = new(), inside = new();
        }
        private readonly List<FrostZone> frostSeals = new();
        public int ActiveFrostSealCount => frostSeals.Count;
        public float FrostRadius => Radius(PortalTowerBalance.Weapon.FrostSeal);

        public static bool ContainsFrostPoint(Vector2 point, Vector2 center, float radius, float heightRatio = 1)
        {
            Vector2 offset = point - center;
            float x = Mathf.Abs(offset.x) / Mathf.Max(.01f, radius);
            float y = Mathf.Abs(offset.y) / Mathf.Max(.01f, radius * heightRatio);
            return y <= 1 && x + .5f * y <= 1;
        }

        private void UpdateFrostSeals()
        {
            const PortalTowerBalance.Weapon weapon = PortalTowerBalance.Weapon.FrostSeal;
            if (!progression.HasWeapon(weapon)) { ClearFrostSeals(); return; }
            var definition = Def(weapon);
            float radius = FrostRadius;
            float ratio = frostSealSprite != null ? frostSealSprite.bounds.size.y / frostSealSprite.bounds.size.x : 1;
            if (Time.time >= frostTime && frostSealSprite != null)
            {
                Health target = null;
                float nearest = Range(weapon) * Range(weapon);
                foreach (var enemy in enemies)
                {
                    if (!Enemy(enemy)) continue;
                    float distance = ((Vector2)enemy.transform.position - (Vector2)tower.transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; target = enemy; }
                }
                if (target != null && progression.TrySpendShot(weapon))
                {
                    Vector2 velocity = target.TryGetComponent<Rigidbody2D>(out var body) ? body.linearVelocity : Vector2.zero;
                    Vector2 direction = velocity.sqrMagnitude > .01f ? velocity.normalized : ((Vector2)tower.transform.position - (Vector2)target.transform.position).normalized;
                    float lead = definition.radius * definition.placementRadiusFraction;
                    SpawnFrostSeal((Vector2)target.transform.position + direction * lead);
                    frostTime = Time.time + Cooldown(weapon);
                    progression.ReportReload(weapon, frostTime);
                }
            }
            float lifetime = definition.duration * progression.Multiplier(weapon, PortalTowerBalance.Effect.Duration);
            float slow = Mathf.Min(.8f, definition.slowFraction * progression.Multiplier(weapon, PortalTowerBalance.Effect.SlowStrength));
            float tick = progression.TickInterval(PortalTowerBalance.Weapon.FrostSeal);
            for (int i = frostSeals.Count - 1; i >= 0; i--)
            {
                var zone = frostSeals[i];
                if (zone.snow != null && !Mathf.Approximately(zone.snowRadius, radius))
                {
                    var shape=zone.snow.shape;shape.scale=new Vector3(radius,radius*ratio,1);zone.snowRadius=radius;
                }
                zone.age += Time.deltaTime;
                if (zone.age >= lifetime) { RemoveFrostSeal(i); continue; }
                bool hit = Time.time >= zone.nextTick;
                zone.inside.Clear();
                foreach (var enemy in enemies)
                {
                    if (!Enemy(enemy) || !ContainsFrostPoint(enemy.transform.position, zone.center, radius, ratio)) continue;
                    if (enemy.TryGetComponent<EnemyMovement>(out var movement))
                    {
                        movement.SetAreaSlow(zone, slow);
                        zone.inside.Add(movement);
                    }
                    if (hit) enemy.TakeDamage(Damage(weapon), definition.damageType, tower.transform);
                }
                foreach (var movement in zone.slowed)
                    if (movement != null && !zone.inside.Contains(movement)) movement.SetAreaSlow(zone, 0);
                (zone.slowed, zone.inside) = (zone.inside, zone.slowed);
                if (hit) zone.nextTick = Time.time + tick;
                if (zone.renderer != null)
                {
                    float scale = radius * 2 / Mathf.Max(.01f, frostSealSprite.bounds.size.x * frostSealArtWidth);
                    zone.renderer.transform.localScale = Vector3.one * scale;
                    float alpha = Mathf.Min(1, zone.age / .1f) * Mathf.Clamp01((lifetime - zone.age) / .35f);
                    zone.renderer.color = new Color(1, 1, 1, .5f * alpha);
                }
            }
        }

        private void SpawnFrostSeal(Vector2 center)
        {
            var go = new GameObject("Portal Frost Seal", typeof(SpriteRenderer));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, tower.gameObject.scene);
            go.transform.position = center;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = frostSealSprite;
            renderer.color = new Color(1, 1, 1, .5f);
            renderer.sharedMaterial = DamagePalette.MaterialFor(DamageType.Ice) ?? effectMaterial;
            renderer.sortingOrder = 3;
            float ratio=frostSealSprite.bounds.size.y/frostSealSprite.bounds.size.x;
            frostSeals.Add(new FrostZone { renderer = renderer, center = center, nextTick = Time.time,
                snow=CreateFrostSnow(center,FrostRadius,ratio),snowRadius=FrostRadius });
            GameAudioController.PlayAt(GameAudioCue.MagicAttack, center, .35f, 1.2f, 1.3f);
        }

        private void RemoveFrostSeal(int index)
        {
            var zone = frostSeals[index];
            foreach (var movement in zone.slowed) if (movement != null) movement.SetAreaSlow(zone, 0);
            if (zone.renderer != null) Destroy(zone.renderer.gameObject);
            if (zone.snow != null) { zone.snow.Stop(true,ParticleSystemStopBehavior.StopEmitting);Destroy(zone.snow.gameObject,1.2f); }
            frostSeals.RemoveAt(index);
        }
        private void ClearFrostSeals() { while (frostSeals.Count > 0) RemoveFrostSeal(frostSeals.Count - 1); }
        private void OnDisable() { ClearFrostSeals(); ClearPortalProjectiles(); ClearDefenseField(); HideHealingBeam(); }
    }
}
