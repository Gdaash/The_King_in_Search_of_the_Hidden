using System.Collections.Generic;
using UnityEngine;

/// <summary>Live combat targets and homes; queries never search the scene or sort candidates.</summary>
public static class CombatTargetRegistry
{
    private sealed class Entry
    {
        public GameObject gameObject;
        public Transform transform;
        public Health health;
        public Collider2D collider;
    }
    private static readonly Dictionary<GameObject, Entry> targets = new();
    private static readonly HashSet<HomeBase> homes = new();
    private static int searchSequence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { targets.Clear(); homes.Clear(); searchSequence = 0; }

    public static float Stagger => (searchSequence++ % 11) * (.15f / 11);
    public static void CollectHealth(string tag, List<Health> result)
    {
        result.Clear();
        foreach (var entry in targets.Values)
        {
            var health = entry.health;
            // Health registers its root and tagged children; collect each root only once.
            if (health != null && health.gameObject == entry.gameObject && health.isActiveAndEnabled &&
                !health.IsDead && entry.gameObject.CompareTag(tag)) result.Add(health);
        }
    }
    public static void Register(GameObject value)
    {
        if (value == null || (value.CompareTag("Untagged") && !WorldCivilianTarget.Contains(value)) || targets.ContainsKey(value)) return;
        targets.Add(value, new Entry { gameObject = value, transform = value.transform, health = value.GetComponentInParent<Health>(), collider = value.GetComponent<Collider2D>() });
    }
    public static void Unregister(GameObject value) { if (value != null) targets.Remove(value); }
    public static void Register(HomeBase value) => homes.Add(value);
    public static void Unregister(HomeBase value) => homes.Remove(value);

    public static bool IsAlive(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var health = targets.TryGetValue(target.gameObject, out var entry) ? entry.health : target.GetComponentInParent<Health>();
        return health == null || !health.IsDead;
    }

    public static Transform Closest(string tag, Vector2 origin, bool lowestHealth = false)
    {
        Transform best = null;
        float bestDistance = float.PositiveInfinity, bestHealth = float.PositiveInfinity;
        foreach (var entry in targets.Values)
        {
            if (entry.gameObject == null || !entry.gameObject.activeInHierarchy || !entry.gameObject.CompareTag(tag) ||
                (entry.health != null && entry.health.IsDead)) continue;
            float health = lowestHealth && entry.health != null ? entry.health.NormalizedHealth : 1;
            float distance = ((Vector2)entry.transform.position - origin).sqrMagnitude;
            if (lowestHealth && health > bestHealth) continue;
            if ((!lowestHealth || health == bestHealth) && distance >= bestDistance) continue;
            best = entry.transform; bestDistance = distance; bestHealth = health;
        }
        return best;
    }

    public static Transform ClosestHostileTarget(Vector2 origin, float civilianRange)
    {
        Transform best = null; float nearest = float.PositiveInfinity;
        foreach (var entry in targets.Values)
        {
            if (entry.gameObject == null || !entry.gameObject.activeInHierarchy ||
                (entry.health != null && entry.health.IsDead)) continue;
            bool civilian = WorldCivilianTarget.Contains(entry.gameObject);
            if (!civilian && !entry.gameObject.CompareTag("Player")) continue;
            float distance = ((Vector2)entry.transform.position - origin).sqrMagnitude;
            if (civilian && distance > civilianRange * civilianRange) continue;
            if (distance >= nearest) continue;
            nearest = distance; best = entry.transform;
        }
        return best;
    }

    public static Transform ClosestHome(Vector2 origin)
    {
        Transform best = null; float nearest = float.PositiveInfinity;
        foreach (var home in homes)
        {
            if (home == null || !home.isActiveAndEnabled) continue;
            float distance = ((Vector2)home.transform.position - origin).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance; best = home.transform;
        }
        return best;
    }

    public static Collider2D ClosestCollider(string tag, Vector2 origin, float range)
    {
        Collider2D best = null;
        float nearest = range * range;
        foreach (var entry in targets.Values)
        {
            if (entry.gameObject == null || !entry.gameObject.activeInHierarchy || !entry.gameObject.CompareTag(tag) ||
                (entry.health != null && entry.health.IsDead)) continue;
            if (entry.collider == null) entry.collider = entry.gameObject.GetComponent<Collider2D>();
            if (entry.collider == null) continue;
            float distance = (entry.collider.ClosestPoint(origin) - origin).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance; best = entry.collider;
        }
        return best;
    }
}
