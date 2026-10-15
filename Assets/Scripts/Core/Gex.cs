using UnityEngine;

/// <summary>Invisible perimeter tile counted by the existing HexBlocker neighbour query.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
[AddComponentMenu("Map/Gex Placeholder")]
public sealed class Gex : MonoBehaviour
{
    private static readonly Vector2[] Outline =
    {
        new(1.8125f, 0), new(.875f, 1.5625f), new(-.875f, 1.5625f),
        new(-1.8125f, 0), new(-.875f, -1.5625f), new(.875f, -1.5625f)
    };

    private void Reset() => Configure();
    private void Awake() => Configure();

    private void Configure()
    {
        int layer = LayerMask.NameToLayer("obstacle");
        if (layer >= 0) gameObject.layer = layer;
        var shape = GetComponent<PolygonCollider2D>();
        shape.isTrigger = false;
        shape.pathCount = 1;
        shape.SetPath(0, Outline);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.45f, .7f, 1, .8f);
        for (int i = 0; i < Outline.Length; i++)
            Gizmos.DrawLine(transform.TransformPoint(Outline[i]), transform.TransformPoint(Outline[(i + 1) % Outline.Length]));
    }
#endif
}
