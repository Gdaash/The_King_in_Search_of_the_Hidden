using System.Collections.Generic;
using UnityEngine;

/// <summary>Same-side combat units can cross paths. Never writes velocity or position.</summary>
public sealed class UnitPassThrough : MonoBehaviour
{
    private static readonly HashSet<UnitPassThrough> units = new();
    private Collider2D[] bodies;
    private bool friendly;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => units.Clear();

    private void Awake()
    {
        bodies = GetComponentsInChildren<Collider2D>(true);
        friendly = CompareTag("Player");
    }

    private void OnEnable()
    {
        units.RemoveWhere(unit => unit == null);
        foreach (var other in units)
            if (other.friendly == friendly) SetPassing(other, true);
        units.Add(this);
    }

    private void OnDisable()
    {
        units.Remove(this);
        foreach (var other in units)
            if (other != null && other.friendly == friendly) SetPassing(other, false);
    }

    private void SetPassing(UnitPassThrough other, bool passing)
    {
        foreach (var own in bodies)
            if (own != null && !own.isTrigger)
                foreach (var theirs in other.bodies)
                    if (theirs != null && !theirs.isTrigger)
                        Physics2D.IgnoreCollision(own, theirs, passing);
    }
}
