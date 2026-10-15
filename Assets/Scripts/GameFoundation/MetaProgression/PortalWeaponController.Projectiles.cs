using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class PortalWeaponController
    {
        private readonly Dictionary<PortalTowerBalance.Weapon, float> projectileTimes = new();
        private readonly List<EnemyProjectile> portalProjectiles = new();

        private void UpdatePortalProjectiles()
        {
            portalProjectiles.RemoveAll(p => p == null);
            FireProjectiles(PortalTowerBalance.Weapon.FireShot);
            FireProjectiles(PortalTowerBalance.Weapon.ElectricOrb);
        }

        private void FireProjectiles(PortalTowerBalance.Weapon weapon)
        {
            if (!progression.HasWeapon(weapon)) return;
            if (projectileTimes.TryGetValue(weapon, out float next) && Time.time < next) return;
            var definition = Def(weapon);
            if (definition.projectilePrefab == null) return;
            Health target = null;
            float nearest = Range(weapon) * Range(weapon);
            foreach (var enemy in enemies)
            {
                if (!Enemy(enemy)) continue;
                float distance = ((Vector2)enemy.transform.position - (Vector2)tower.transform.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; target = enemy; }
            }
            if (target == null) return;
            projectileTimes[weapon] = Time.time + Cooldown(weapon);
            Vector3 origin = Source != null ? Source.position : tower.transform.position;
            Vector3 direction = (target.transform.position - origin).normalized;
            int count = progression.Count(weapon);
            float speed = definition.projectileSpeedMultiplier * progression.Multiplier(weapon, PortalTowerBalance.Effect.ExpansionSpeed);
            for (int i = 0; i < count; i++)
            {
                // Extra pellets stay inside the same narrow fan; zero-spread orbs fly in parallel.
                float angle = count <= 1 ? 0 : definition.spreadAngle * (i / (float)(count - 1) - .5f);
                Vector3 shotDirection = Quaternion.Euler(0, 0, angle) * direction;
                Vector3 offset = definition.spreadAngle > 0 ? Vector3.zero : new Vector3(-direction.y, direction.x) * ((i - (count - 1) * .5f) * .14f);
                var root = Instantiate(definition.projectilePrefab, origin + offset, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, tower.gameObject.scene);
                root.transform.localScale *= definition.projectileScale;
                if (root.TryGetComponent<EnemyProjectile>(out var projectile))
                {
                    float lifetime = (Range(weapon) + Vector2.Distance(origin, tower.transform.position) + 1) / Mathf.Max(.01f, projectile.BaseSpeed * speed);
                    projectile.SetupHit(shotDirection, "Enemy1", Damage(weapon), definition.damageType, tower.transform, speed, lifetime);
                    portalProjectiles.Add(projectile);
                }
                else Destroy(root);
            }
        }

        private void ClearPortalProjectiles()
        {
            foreach (var projectile in portalProjectiles) if (projectile != null) Destroy(projectile.gameObject);
            portalProjectiles.Clear();
            projectileTimes.Clear();
        }
    }
}
