using System.Collections.Generic;
using UnityEngine;

// The tower remains the damage target; only melee reach uses the ground hex.
public sealed class PortalMeleeBoundary : MonoBehaviour
{
    [SerializeField] private Sprite hexSprite;
    [SerializeField] private Vector2 hexScale = Vector2.one;
    private readonly List<Vector2> outline = new();

    private void Awake()
    {
        if (hexSprite != null && hexSprite.GetPhysicsShapeCount() > 0)
            hexSprite.GetPhysicsShape(0, outline);
    }

    public bool IsAtBoundary(Vector2 worldPosition)
    {
        if (outline.Count < 3) return false;
        Vector2 point = transform.InverseTransformPoint(worldPosition);
        point /= hexScale;
        bool inside = false;
        float distanceSquared = float.PositiveInfinity;
        for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
        {
            Vector2 a = outline[j], b = outline[i], edge = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            distanceSquared = Mathf.Min(distanceSquared, (point - a - edge * t).sqrMagnitude);
            if ((a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        // Small movement tolerance, independent of enemy level / attack range.
        return inside || distanceSquared <= 0.08f * 0.08f;
    }
}
