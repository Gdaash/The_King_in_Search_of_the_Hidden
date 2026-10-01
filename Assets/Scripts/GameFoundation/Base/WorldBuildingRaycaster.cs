using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.Base
{
    /// <summary>Collider picking in the same pixel-perfect viewport used by the Base artwork.</summary>
    public sealed class WorldBuildingRaycaster : BaseRaycaster
    {
        [SerializeField] private Vector2 documentSize = new(640, 480);
        [SerializeField] private float pixelsPerUnit = 32;
        public override Camera eventCamera => Camera.main;
        // Modal dimmers, tooltips and upgrade controls always take precedence.
        public override int sortOrderPriority => -100;
        public override int renderOrderPriority => -100;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            float scale = Mathf.Min(Screen.width / documentSize.x, Screen.height / documentSize.y);
            Vector2 pixel = (eventData.position - (new Vector2(Screen.width, Screen.height) - documentSize * scale) * .5f) / scale;
            if (pixel.x < 0 || pixel.y < 0 || pixel.x > documentSize.x || pixel.y > documentSize.y) return;
            Vector3 world = transform.TransformPoint((pixel - documentSize * .5f) / pixelsPerUnit);
            WorldBuildingHitArea best = null;
            int order = int.MinValue;
            foreach (var collider in Physics2D.OverlapPointAll(world, 1 << 2))
            {
                if (!collider.TryGetComponent<WorldBuildingHitArea>(out var hit) || hit.Owner == null || !hit.Owner.isActiveAndEnabled) continue;
                var sprite = hit.GetComponent<SpriteRenderer>();
                int candidate = sprite != null ? sprite.sortingOrder : 0;
                if (best == null || candidate > order) { best = hit; order = candidate; }
            }
            if (best == null) return;
            results.Add(new RaycastResult { gameObject = best.Owner.PointerTarget, module = this,
                distance = 0, index = results.Count, screenPosition = eventData.position,
                worldPosition = world, worldNormal = Vector3.back });
        }
    }
}
