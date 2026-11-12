using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using GameFoundation.Audio;
using Weapon = GameFoundation.MetaProgression.PortalTowerBalance.Weapon;

namespace GameFoundation.MetaProgression
{
    public sealed partial class PortalWeaponController
    {
        private float SupportAmount(Weapon weapon) => Def(weapon).supportAmount * progression.Profile(weapon).Support *
            progression.RunMultiplier(weapon, CombatUpgradeCatalog.Stat.Support) +
            progression.RunAdjustment(weapon, CombatUpgradeCatalog.Stat.Armor);
        private readonly List<Health> supportTargets = new();
        private readonly Dictionary<Weapon, float> supportCooldowns = new();
        private Light2D defenseField;
        private Health defenseTarget;
        private FlashlightController defenseBeam;
        private GameObject defenseBeamRoot;
        private ParticleSystem defenseShields;
        private Mesh defenseEmitterMesh, defenseShieldMesh;
        private static readonly Color DefenseLightColor = new(.88f, .94f, 1f);
        private readonly List<Vector2> defenseOutline = new();
        private float defenseEnergyRemainder, defenseOutlineWidth;
        private GameObject healingBeamRoot;
        private FlashlightController healingBeam;
        private Light2D healingSpot;

        public float DefenseFieldArmor(Health target)
        {
            if (!defenseField || !defenseField.isActiveAndEnabled || !IsSupportTarget(target)) return 0;
            Vector2 point = defenseField.transform.InverseTransformPoint(target.transform.position);
            bool inside = false;
            for (int i = 0, previous = defenseOutline.Count - 1; i < defenseOutline.Count; previous = i++)
            {
                var a = defenseOutline[previous];
                var b = defenseOutline[i];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < a.x + (b.x - a.x) * (point.y - a.y) / (b.y - a.y))
                    inside = !inside;
            }
            return inside ? SupportAmount(Weapon.DefenseField) : 0;
        }

        private bool IsSupportTarget(Health target) =>
            target && target.isActiveAndEnabled && !target.IsDead && target.CompareTag("Player") &&
            (!tower || target.transform != tower.transform && !target.transform.IsChildOf(tower.transform) &&
                !tower.transform.IsChildOf(target.transform));

        private void UpdateSupportWeapon()
        {
            if (!progression.UsesEnergyLoadout) return;
            var weapon = progression.ActiveWeapon;
            var definition = Def(weapon);
            if (weapon == Weapon.DefenseField)
            {
                UpdateDefenseField(definition);
                return;
            }
            if (definition == null || !definition.IsSupport ||
                supportCooldowns.TryGetValue(weapon, out float next) && Time.time < next ||
                !progression.CanPayForShot(weapon)) return;

            CombatTargetRegistry.CollectHealth("Player", supportTargets);
            Health chosen = null;
            float best = float.NegativeInfinity;
            float rangeSquared = Range(weapon) * Range(weapon);
            int shieldCandidates = 0;
            foreach (var target in supportTargets)
            {
                if (!IsSupportTarget(target)) continue;
                float distance = ((Vector2)target.transform.position - (Vector2)tower.transform.position).sqrMagnitude;
                if (distance > rangeSquared) continue;
                if (weapon == Weapon.MagicShield)
                {
                    if (target.ShieldHealth <= 0 && Random.Range(0, ++shieldCandidates) == 0) chosen = target;
                    continue;
                }
                if (weapon == Weapon.HealingRay && target.CurrentHealth >= target.MaxHealth) continue;
                float score = weapon == Weapon.HealingRay ? -target.NormalizedHealth : distance;
                if (score > best) { best = score; chosen = target; }
            }
            if (!chosen || !progression.TrySpendShot(weapon)) return;
            supportCooldowns[weapon] = Time.time + Cooldown(weapon);
            progression.ReportReload(weapon, supportCooldowns[weapon]);
            switch (weapon)
            {
                case Weapon.HealingRay:
                    chosen.RestoreHealth(SupportAmount(weapon));
                    StartCoroutine(ShowHealingBeam(chosen));
                    break;
                case Weapon.MagicShield:
                    chosen.GrantShield(SupportAmount(weapon));
                    StartCoroutine(ShowShield(chosen, definition.icon));
                    break;
            }
            GameAudioController.PlayAt(GameAudioCue.MagicAttack, chosen.transform.position);
        }

        private IEnumerator ShowHealingBeam(Health target)
        {
            if (!beamPrefab) yield break;
            if (!healingBeamRoot)
            {
                healingBeamRoot = CreateSupportBeam("Portal healing beam", new Color(.31f, 1f, .49f),
                    new Color(.49f, 1f, .62f), out healingBeam, out healingSpot);
            }
            if (!healingBeam) yield break;
            healingBeamRoot.SetActive(true);
            float end = Time.time + .36f;
            while (IsSupportTarget(target) && Time.time < end && healingBeamRoot.activeSelf)
            {
                healingBeam.transform.position = Source.position;
                healingBeam.SetCrystalTarget(target.HitEffectPosition, true);
                if (healingSpot) healingSpot.intensity = Mathf.Clamp01((end - Time.time) / .14f);
                yield return null;
            }
            HideHealingBeam();
        }

        private GameObject CreateSupportBeam(string name, Color lightColor, Color spriteColor,
            out FlashlightController controller, out Light2D spot)
        {
            var root = Instantiate(beamPrefab);
            root.name = name;
            controller = root.GetComponentInChildren<FlashlightController>(true);
            spot = null;
            foreach (var light in root.GetComponentsInChildren<Light2D>(true))
            {
                light.color = lightColor;
                if (light.lightCookieSprite) spot = light;
            }
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.GetComponentInParent<LogisticFlag>() == null)
                    sprite.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, sprite.color.a);
            return root;
        }

        private void HideHealingBeam()
        {
            if (healingBeamRoot) healingBeamRoot.SetActive(false);
        }

        private IEnumerator ShowShield(Health target, Sprite icon)
        {
            if (!icon) yield break;
            var effect = new GameObject("Portal shield flash", typeof(SpriteRenderer), typeof(Light2D));
            effects.Add(effect);
            var sprite = effect.GetComponent<SpriteRenderer>();
            sprite.sprite = icon;
            sprite.sortingLayerName = "OverLight";
            sprite.sortingOrder = 152;
            var glow = effect.GetComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(.32f, .73f, 1f);
            glow.pointLightOuterRadius = .8f;
            int version = target.SpawnVersion;
            float start = Time.time;
            const float duration = .8f;
            while (IsSupportTarget(target) && target.SpawnVersion == version && Time.time - start < duration)
            {
                float elapsed = Time.time - start;
                float alpha = Mathf.Clamp01(elapsed / .12f) * Mathf.Clamp01((duration - elapsed) / .32f);
                effect.transform.position = target.DamageNumberPosition + Vector3.up * (.12f + elapsed * .25f);
                effect.transform.localScale = Vector3.one * Mathf.Lerp(.28f, .58f, Mathf.Clamp01(elapsed / .18f));
                sprite.color = new Color(1f, 1f, 1f, alpha);
                glow.intensity = alpha * 1.2f;
                yield return null;
            }
            effects.Remove(effect);
            Destroy(effect);
        }

        private void UpdateDefenseField(PortalTowerBalance.WeaponDefinition definition)
        {
            if (definition == null || (definition.energyPerSecond > 0 && progression.Energy <= 0))
            {
                ClearDefenseField();
                return;
            }

            Vector2 center = tower.transform.position;
            float rangeSquared = Range(Weapon.DefenseField) * Range(Weapon.DefenseField);
            if (!IsSupportTarget(defenseTarget) ||
                ((Vector2)defenseTarget.transform.position - center).sqrMagnitude > rangeSquared)
            {
                defenseTarget = null;
                float farthest = -1;
                CombatTargetRegistry.CollectHealth("Player", supportTargets);
                foreach (var ally in supportTargets)
                {
                    if (!IsSupportTarget(ally)) continue;
                    float distance = ((Vector2)ally.transform.position - center).sqrMagnitude;
                    if (distance <= farthest || distance > rangeSquared) continue;
                    defenseTarget = ally;
                    farthest = distance;
                }
            }
            if (!defenseTarget)
            {
                ClearDefenseField();
                return;
            }

            if (!defenseField)
            {
                if (!beamPrefab) return;
                Light2D template = null;
                foreach (var light in beamPrefab.GetComponentsInChildren<Light2D>(true))
                    if (light.lightCookieSprite) { template = light; break; }
                if (!template) return;
                var cookie = template.lightCookieSprite;
                defenseOutline.Clear();
                if (cookie.GetPhysicsShapeCount() == 0 || cookie.GetPhysicsShape(0, defenseOutline) < 3) return;

                float left = float.PositiveInfinity, right = float.NegativeInfinity;
                foreach (var point in defenseOutline)
                {
                    left = Mathf.Min(left, point.x);
                    right = Mathf.Max(right, point.x);
                }
                defenseOutlineWidth = right - left;
                if (defenseOutlineWidth <= 0) return;

                defenseField = Instantiate(template);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(defenseField.gameObject, tower.gameObject.scene);
                defenseField.name = "Portal defense hex";
                defenseField.color = DefenseLightColor;
                defenseField.intensity = 2.35f;
                defenseField.shadowsEnabled = false;
                defenseShields = CreateDefenseShields();
                defenseBeamRoot = CreateSupportBeam("Portal defense beam", DefenseLightColor, DefenseLightColor,
                    out defenseBeam, out var duplicateSpot);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(defenseBeamRoot, tower.gameObject.scene);
                if (duplicateSpot) duplicateSpot.enabled = false;
            }

            defenseField.transform.localScale = Vector3.one * (2 * Radius(Weapon.DefenseField) / defenseOutlineWidth);
            defenseField.transform.position = defenseTarget.transform.position;
            if (!defenseField.gameObject.activeSelf) defenseField.gameObject.SetActive(true);
            if (!defenseShields.isPlaying) defenseShields.Play();
            if (defenseBeam)
            {
                defenseBeamRoot.SetActive(true);
                defenseBeam.transform.position = Source.position;
                defenseBeam.SetCrystalTarget(defenseTarget.transform.position, true);
            }

            defenseEnergyRemainder += progression.Profile(Weapon.DefenseField).Cost(definition.energyPerSecond) *
                progression.RunMultiplier(Weapon.DefenseField, CombatUpgradeCatalog.Stat.Drain) * Time.deltaTime;
            int spend = Mathf.FloorToInt(defenseEnergyRemainder);
            if (spend == 0) return;
            defenseEnergyRemainder -= spend;
            progression.SpendContinuousEnergy(spend);
            if (progression.Energy == 0) ClearDefenseField();
        }

        private ParticleSystem CreateDefenseShields()
        {
            // A triangle fan covers the same outline used to apply armor.
            var vertices = new Vector3[defenseOutline.Count + 1];
            var triangles = new int[defenseOutline.Count * 3];
            for (int i = 0; i < defenseOutline.Count; i++)
            {
                vertices[i + 1] = defenseOutline[i];
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % defenseOutline.Count + 1;
            }
            defenseEmitterMesh = new Mesh { name = "Defense hex emission", vertices = vertices, triangles = triangles };
            defenseEmitterMesh.RecalculateNormals();
            defenseShieldMesh = CreateParticleMesh("Pixel shield", new[] { "#####", "#...#", "#...#", ".#.#.", "..#.." });

            var root = new GameObject("Rising defense shields");
            root.transform.SetParent(defenseField.transform, false);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.2f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .21f);
            main.startColor = new Color(.89f, .96f, 1f, .5f);
            main.maxParticles = 32;
            var emission = particles.emission;
            emission.rateOverTime = 18;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.mesh = defenseEmitterMesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-.12f, .12f);
            velocity.y = new ParticleSystem.MinMaxCurve(.35f, .6f);
            velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) });
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = defenseShieldMesh;
            renderer.sharedMaterial = GameFoundation.Combat.CombatStatusVfx.PixelMaterial;
            renderer.sortingLayerName = "OverLight";
            renderer.sortingOrder = 152;
            renderer.alignment = ParticleSystemRenderSpace.World;
            return particles;
        }

        private void ClearDefenseField()
        {
            defenseTarget = null;
            if (defenseShields) defenseShields.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (defenseField) defenseField.gameObject.SetActive(false);
            if (defenseBeamRoot) defenseBeamRoot.SetActive(false);
        }
    }
}
