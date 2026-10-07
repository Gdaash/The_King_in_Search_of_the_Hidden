using UnityEngine;

/// <summary>Scene dependency: guarantees that the catalog is loaded before gameplay starts.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class ProjectReferencesBootstrap : MonoBehaviour
{
    [SerializeField] private ProjectReferences catalog;

    private void OnEnable()
    {
        if (catalog != null) catalog.Activate();
        else Debug.LogError("Project reference catalog is not assigned.", this);
    }
}
