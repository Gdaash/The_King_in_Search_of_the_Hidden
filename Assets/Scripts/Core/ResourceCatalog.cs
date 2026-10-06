using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Direct resource references with stable IDs for saved data.</summary>
public static class ResourceCatalog
{
    private static IReadOnlyList<ResourceType> all;
    public static IReadOnlyList<ResourceType> All => all ??= (ProjectReferences.Instance != null ? ProjectReferences.Instance.resources : Array.Empty<ResourceType>()).OrderBy(item => item.Id).ToArray();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => all = null;
    public static ResourceType Find(string id) => All.FirstOrDefault(item => item != null && item.Id == id);
}
