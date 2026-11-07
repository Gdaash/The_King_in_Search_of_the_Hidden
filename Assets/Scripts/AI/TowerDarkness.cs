using UnityEngine;

/// <summary>Independent cloud lifetimes keep the tower silenced until the last cloud expires.</summary>
public sealed class TowerDarkness : MonoBehaviour
{
    private int clouds;
    public bool IsBlocked => clouds > 0;
    public void Attach() => clouds++;
    public void Detach() => clouds = Mathf.Max(0, clouds - 1);
    public static bool Blocks(Transform source) => source != null && source.GetComponentInParent<TowerDarkness>() is TowerDarkness darkness && darkness.IsBlocked;
}
