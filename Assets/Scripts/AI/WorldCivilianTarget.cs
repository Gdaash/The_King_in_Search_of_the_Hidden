using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Registers civilians as hostile targets and accounts for population losses once.</summary>
public sealed class WorldCivilianTarget : MonoBehaviour
{
    private static readonly HashSet<GameObject> targets = new();
    private Health health;
    private ResourceType population;
    private bool lossRecorded;
    public static bool Contains(GameObject value) => targets.Contains(value);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => targets.Clear();
    public static void Ensure(GameObject target, ResourceType humanType)
    {
        var marker = target.GetComponent<WorldCivilianTarget>();
        if (marker == null) marker = target.AddComponent<WorldCivilianTarget>();
        marker.population = humanType;
        marker.health = target.GetComponent<Health>();
        if (marker.health == null) { marker.health = target.AddComponent<Health>(); marker.health.ConfigureFallbackHealth(40); }
        marker.health.OnHealthChanged ??= new UnityEvent<float>();
        marker.health.OnHealthChanged.RemoveListener(marker.OnHealthChanged);
        marker.health.OnHealthChanged.AddListener(marker.OnHealthChanged);
        CombatTargetRegistry.Unregister(target);
        CombatTargetRegistry.Register(target);
    }
    private void OnEnable() { targets.Add(gameObject); CombatTargetRegistry.Register(gameObject); }
    private void OnDisable() { targets.Remove(gameObject); CombatTargetRegistry.Unregister(gameObject); }
    private void OnHealthChanged(float fraction)
    {
        if (fraction > 0 || lossRecorded) return;
        lossRecorded = true;
        GameFoundation.MetaProgression.WorldCasualties.Death(population,gameObject);
        if (population != null) GlobalResourceManager.Instance?.TrySpendResource(population, 1);
    }
}
