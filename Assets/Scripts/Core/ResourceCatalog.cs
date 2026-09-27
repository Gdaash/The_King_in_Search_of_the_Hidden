using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Single runtime lookup for ResourceType assets stored under Resources/ResourceTypes.</summary>
public static class ResourceCatalog
{
    private static IReadOnlyList<ResourceType> all;
    public static IReadOnlyList<ResourceType> All => all ??= Resources.LoadAll<ResourceType>("ResourceTypes").OrderBy(item => item.name).ToArray();
    public static ResourceType Find(string id) => All.FirstOrDefault(item => item != null && (item.name == id || item.resourceName == id));
}
