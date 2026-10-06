using System;
using System.Linq;
using GameFoundation.Audio;
using GameFoundation.Base;
using GameFoundation.MetaProgression;
using UnityEngine;

/// <summary>Preloaded asset references; asset names and folders are not runtime lookup keys.</summary>
[CreateAssetMenu(menuName = "Those UnderHex/Project References")]
public sealed class ProjectReferences : ScriptableObject
{
    public ResourceType[] resources = Array.Empty<ResourceType>();
    public PortalLocationDefinition[] portalLocations = Array.Empty<PortalLocationDefinition>();
    public GameAudioLibrary audioLibrary;
    public BuildingUpgradeCatalog buildingUpgrades;
    public ThoseUnderHexCrtSettings crtSettings;
    public Material crtOverlayMaterial;
    private static ProjectReferences instance;
    public static ProjectReferences Instance
    {
        get
        {
            if (instance == null) instance = Resources.FindObjectsOfTypeAll<ProjectReferences>().FirstOrDefault();
            return instance;
        }
    }
    private void OnEnable() => instance = this;
}
