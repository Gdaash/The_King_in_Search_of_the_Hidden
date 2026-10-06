using UnityEngine;

[CreateAssetMenu(menuName = "Those UnderHex/Rendering/CRT Settings", fileName = "ThoseUnderHex CRT Settings")]
public sealed class ThoseUnderHexCrtSettings : ScriptableObject
{
    [SerializeField] private bool enabledEffect = true;

    /// <summary>Shared switch for the world filter and the UI overlay.</summary>
    public bool EnabledEffect => enabledEffect;

    public static bool IsEnabled
    {
        get
        {
            ThoseUnderHexCrtSettings settings = ProjectReferences.Instance != null ? ProjectReferences.Instance.crtSettings : null;
            return settings == null || settings.enabledEffect;
        }
    }
}
