using UnityEngine;

namespace GameFoundation.Combat
{
    /// <summary>One constant-velocity intercept calculation per volley; no tracking or allocations.</summary>
    public static class ProjectileAim
    {
        public static Vector2 Predict(Vector2 origin, Vector2 target, Vector2 velocity, float projectileSpeed)
        {
            Vector2 relative = target - origin;
            if (projectileSpeed <= .001f || velocity.sqrMagnitude < .0001f || relative.sqrMagnitude < .0001f)
                return target;

            float a = velocity.sqrMagnitude - projectileSpeed * projectileSpeed;
            float b = 2 * Vector2.Dot(relative, velocity);
            float c = relative.sqrMagnitude;
            float time = -1;
            if (Mathf.Abs(a) < .0001f)
            {
                if (Mathf.Abs(b) > .0001f) time = -c / b;
            }
            else
            {
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float root = Mathf.Sqrt(discriminant);
                    float first = (-b - root) / (2 * a);
                    float second = (-b + root) / (2 * a);
                    if (first > 0) time = first;
                    if (second > 0 && (time < 0 || second < time)) time = second;
                }
            }
            // Unreachable targets get a bounded estimate instead of an extreme aim point.
            if (time < 0) time = Mathf.Sqrt(c) / projectileSpeed;
            // Deliberate slight underlead; turns after the shot can still evade it.
            return target + velocity * (Mathf.Min(time, 4f) * .95f);
        }
    }
}
