using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldMilitaryDeploymentController
    {
        private WorldFlashlightAvailability militaryLights;
        private readonly Dictionary<GameObject, Vector2> formation = new();
        private float nextFormationCheck;
        private bool escapeRecall;
        private Collider2D towerFootprint;
        private Bounds towerSilhouette;
        private readonly Dictionary<GameObject, Vector2> portalWaitingOffsets = new();
        private readonly Dictionary<GameObject, (Vector2 center, bool ranged, Vector2 point)> defenseSlots = new();
        private readonly HashSet<GameObject> activeFormationUnits = new();
        private const float PreferredSlotSpacing = .65f;
        private void InitializeMilitaryLights()
        {
            towerFootprint = portal != null ? portal.GetComponentsInChildren<Collider2D>().FirstOrDefault(c => c.enabled && !c.isTrigger) : null;
            var towerHealth = portal != null ? portal.GetComponent<Health>() : null;
            var towerSprite = towerHealth != null ? towerHealth.BodyRenderer : null;
            towerSilhouette = towerSprite != null ? towerSprite.bounds : towerFootprint != null ? towerFootprint.bounds : new Bounds();
            militaryLights = FindFirstObjectByType<WorldFlashlightAvailability>();
            if (militaryLights != null) militaryLights.MilitaryPositionsChanged += RebuildFormation;
            RebuildFormation();
        }
        private void Update()
        {
            if (escapeRecall || portal == null || Time.unscaledTime < nextFormationCheck) return;
            nextFormationCheck = Time.unscaledTime + .25f;
            // Deaths and low-health retreats can otherwise leave an uneven split.
            RebuildFormation();
        }
        public void RebuildFormation()
        {
            if (escapeRecall || portal == null) return;
            var points = militaryLights != null ? militaryLights.MilitaryPositions : new List<Vector2>();
            int posts = Mathf.Max(1, points.Count);
            activeFormationUnits.Clear();
            foreach (var type in Controls)
                foreach (var unit in type.deployed)
                    if (unit != null && unit.activeInHierarchy && unit.GetComponent<WorldMilitaryReturner>() == null &&
                        (!unit.TryGetComponent<Health>(out var health) || !health.IsDead)) activeFormationUnits.Add(unit);
            foreach (var unit in formation.Keys.Where(u => !activeFormationUnits.Contains(u)).ToArray()) formation.Remove(unit);
            foreach (var unit in portalWaitingOffsets.Keys.Where(u => !activeFormationUnits.Contains(u)).ToArray()) portalWaitingOffsets.Remove(unit);
            foreach (var unit in defenseSlots.Keys.Where(u => !activeFormationUnits.Contains(u)).ToArray()) defenseSlots.Remove(unit);
            var buckets = new List<GameObject>[posts];
            for (int i = 0; i < posts; i++) buckets[i] = new List<GameObject>();
            foreach (var type in Controls)
            {
                int index = 0;
                int start = 0;
                for (int post = 1; post < posts; post++) if (buckets[post].Count < buckets[start].Count) start = post;
                foreach (var unit in type.deployed)
                {
                    if (!activeFormationUnits.Contains(unit)) continue;
                    if (unit.TryGetComponent<WorldEngineer>(out var engineer))
                    {
                        if (!engineer.HasSite) AssignFormation(unit, PortalWaitingPoint(unit, portal.position), true);
                        continue;
                    }
                    buckets[(start + index++) % posts].Add(unit);
                }
            }
            for (int post = 0; post < posts; post++)
            {
                Vector2 center = points.Count > 0 ? points[post] : (Vector2)portal.position;
                var ranged = buckets[post].Where(u => u.GetComponent<EnemyAI_Ranged>() != null).ToArray();
                var melee = buckets[post].Where(u => u.GetComponent<EnemyAI_Ranged>() == null).ToArray();
                for (int i = 0; i < ranged.Length; i++) AssignFormation(ranged[i], points.Count == 0 ? PortalWaitingPoint(ranged[i], center) : DefenseWaitingPoint(ranged[i], center, true), points.Count == 0);
                for (int i = 0; i < melee.Length; i++) AssignFormation(melee[i], points.Count == 0 ? PortalWaitingPoint(melee[i], center) : DefenseWaitingPoint(melee[i], center, false), points.Count == 0);
            }
            foreach (var dead in formation.Keys.Where(u => u == null).ToArray()) formation.Remove(dead);
            foreach (var dead in portalWaitingOffsets.Keys.Where(u => u == null).ToArray()) portalWaitingOffsets.Remove(dead);
        }
        private Vector2 PortalWaitingPoint(GameObject unit, Vector2 center)
        {
            defenseSlots.Remove(unit);
            if (!portalWaitingOffsets.TryGetValue(unit, out var offset))
            {
                // Reserve once across the whole portal hex. The tower silhouette alone is excluded;
                // the upper portions of the left/right flanks remain available.
                // Rebuilding the formation never makes idle soldiers wander.
                const float maximum = 1.7f;
                offset = KeepOutsideTower(unit, center + Vector2.down * .6f) - center;
                var boundary = portal != null ? portal.GetComponentInParent<PortalMeleeBoundary>() : null;
                float bestScore = float.NegativeInfinity;
                for (int attempt = 0; attempt < 128; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2);
                    float radius = Mathf.Sqrt(Random.Range(0f, maximum * maximum));
                    var candidate = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    candidate = KeepOutsideTower(unit, center + candidate) - center;
                    if (!PlayerHexNavigation.IsWalkable(center + candidate) || (boundary != null && !boundary.IsAtBoundary(center + candidate))) continue;
                    float nearest = float.PositiveInfinity;
                    foreach (var other in portalWaitingOffsets.Values) nearest = Mathf.Min(nearest, (candidate - other).sqrMagnitude);
                    // Use the closest positions with enough spacing. Small jitter varies their angles
                    // and radii between runs. When crowded, use the largest available gap.
                    float score = nearest >= PreferredSlotSpacing * PreferredSlotSpacing
                        ? 10f - candidate.magnitude + Random.Range(0f, .12f)
                        : nearest;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        offset = candidate;
                    }
                }
                portalWaitingOffsets.Add(unit, offset);
            }
            return center + offset;
        }
        private Vector2 DefenseWaitingPoint(GameObject unit, Vector2 center, bool ranged)
        {
            portalWaitingOffsets.Remove(unit);
            if (defenseSlots.TryGetValue(unit, out var slot) && slot.center == center && slot.ranged == ranged)
                return slot.point;

            Vector2 outward = center - (Vector2)portal.position;
            float facing = outward.sqrMagnitude > .01f ? Mathf.Atan2(outward.y, outward.x) : Mathf.PI * .5f;
            Vector2 best = KeepOutsideTower(unit, ranged ? center : center + outward.normalized * 1.15f, false);
            float bestDistance = -1;
            // Deterministic candidates, stable reservations. A crowded hex packs more densely,
            // rather than rejecting troops or pushing their neighbours out of the post.
            for (int attempt = 0; attempt < 128; attempt++)
            {
                float fraction = Mathf.Repeat(attempt * .618034f, 1f);
                float angle = ranged ? attempt * 2.399963f : facing + (fraction - .5f) * Mathf.PI * 2f / 3f;
                float radius = ranged ? (attempt == 0 ? 0 : .85f * Mathf.Sqrt((attempt % 17 + 1f) / 17f))
                    : .95f + .4f * (attempt % 7) / 6f;
                Vector2 candidate = KeepOutsideTower(unit, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, false);
                if ((candidate - center).sqrMagnitude > 1.4f * 1.4f || !PlayerHexNavigation.IsWalkable(candidate)) continue;
                float nearest = float.PositiveInfinity;
                foreach (var pair in defenseSlots)
                    if (pair.Key != unit && pair.Value.center == center)
                        nearest = Mathf.Min(nearest, (candidate - pair.Value.point).sqrMagnitude);
                if (nearest > bestDistance) { bestDistance = nearest; best = candidate; }
                if (nearest >= PreferredSlotSpacing * PreferredSlotSpacing) break;
            }
            defenseSlots[unit] = (center, ranged, best);
            return best;
        }
        public static Vector2 FormationPoint(Vector2 center, Vector2 portalCenter, bool ranged, int index, int count, bool atPortal = false)
        {
            if (atPortal)
            {
                // Waiting slots occupy the lower semicircle: sides and front, never behind the tower.
                float angle = count <= 1 ? Mathf.PI * 1.5f : Mathf.PI + (index + .5f) * Mathf.PI / count;
                return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ranged ? 1.1f : 1.35f);
            }
            if (ranged)
            {
                if (count == 1) return center;
                // Concentric small rings keep the ranged core inside the melee screen.
                float angle = index * 2.399963f;
                float radius = .18f + .4f * Mathf.Sqrt((index + 1f) / count);
                return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            Vector2 outward = center - portalCenter;
            float facing = outward.sqrMagnitude > .01f ? Mathf.Atan2(outward.y, outward.x) : Mathf.PI * .5f;
            float offset = count <= 1 ? 0 : (index / (float)(count - 1) - .5f) * (atPortal ? Mathf.PI * 1.8f : Mathf.PI * 2f / 3f);
            float radiusMelee = atPortal ? 1.1f : 1.05f;
            return center + new Vector2(Mathf.Cos(facing + offset), Mathf.Sin(facing + offset)) * radiusMelee;
        }
        public Vector2 KeepOutsideTower(GameObject unit, Vector2 destination, bool avoidSilhouette = true)
        {
            if (towerFootprint == null) return destination;
            var bounds = towerFootprint.bounds;
            var body = unit != null ? unit.GetComponent<Collider2D>() : null;
            Vector2 size = body != null ? (Vector2)body.bounds.extents : new Vector2(.2f, .2f);
            // Arrival/home stopping tolerance must not let the body overlap the tower.
            Vector2 extent = (Vector2)bounds.extents + size + Vector2.one * .32f;
            Vector2 delta = destination - (Vector2)bounds.center;
            if (Mathf.Abs(delta.x) < extent.x && Mathf.Abs(delta.y) < extent.y)
            {
                if (delta.sqrMagnitude < .001f) delta = Vector2.down;
                float factor = Mathf.Min(extent.x / Mathf.Max(.00001f, Mathf.Abs(delta.x)), extent.y / Mathf.Max(.00001f, Mathf.Abs(delta.y)));
                destination = (Vector2)bounds.center + delta * factor;
            }
            // The visual exclusion belongs to portal waiting slots, not neighbouring military hexes.
            if (!avoidSilhouette) return destination;
            // This excludes idle destinations only; it adds no collision or navigation obstacle.
            float visualHalfWidth = unit != null && unit.TryGetComponent<Health>(out var health) && health.BodyRenderer != null
                ? health.BodyRenderer.bounds.extents.x : size.x;
            // The sprite rectangle contains wide transparent margins. Using its full width
            // incorrectly excludes both flanks. The base collider defines the tower column;
            // the sprite still defines its full height so nobody waits behind it.
            float left = bounds.min.x - visualHalfWidth - .12f;
            float right = bounds.max.x + visualHalfWidth + .12f;
            float bottom = bounds.min.y - size.y - .32f;
            float top = towerSilhouette.max.y + size.y + .32f;
            if (destination.x > left && destination.x < right && destination.y > bottom && destination.y < top)
            {
                float toSide = Mathf.Min(destination.x - left, right - destination.x);
                if (destination.y - bottom <= toSide) destination.y = bottom;
                else destination.x = destination.x < towerSilhouette.center.x ? left : right;
            }
            return destination;
        }
        private void AssignFormation(GameObject unit, Vector2 destination, bool atPortal)
        {
            destination = KeepOutsideTower(unit, destination, atPortal);
            if (formation.TryGetValue(unit, out var previous) && (previous - destination).sqrMagnitude < .001f) return;
            formation[unit] = destination;
            var mover = unit.GetComponent<WorldMilitaryArrivalMover>() ?? unit.AddComponent<WorldMilitaryArrivalMover>();
            mover.Begin(destination);
        }
    }
}
