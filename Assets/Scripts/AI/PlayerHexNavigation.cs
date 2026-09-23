using UnityEngine;

public static class PlayerHexNavigation
{
    private const float ProbeDistance = 0.32f;
    private static readonly float[] AvoidanceAngles = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f };

    public static Vector2 ResolveDirection(Vector2 position, Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f) return Vector2.zero;
        desiredDirection.Normalize();
        if (IsWalkable(position + desiredDirection * ProbeDistance)) return desiredDirection;

        foreach (float angle in AvoidanceAngles)
        {
            Vector2 candidate = Quaternion.Euler(0f, 0f, angle) * desiredDirection;
            if (IsWalkable(position + candidate * ProbeDistance)) return candidate;
        }
        return Vector2.zero;
    }

    public static bool IsWalkable(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            HexBlocker blocker = hit.GetComponent<HexBlocker>() ?? hit.GetComponentInParent<HexBlocker>();
            if (blocker != null && blocker.IsBlocked) return false;
        }
        return true;
    }
}
