using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>Editable world-space building collider, bound to its existing UI control.</summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class WorldBuildingHitArea : MonoBehaviour
    {
        public WorldBuildingButton Owner { get; set; }
    }
}
