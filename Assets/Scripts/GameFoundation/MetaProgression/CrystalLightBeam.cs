using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.MetaProgression
{
    /// <summary>Flashlight visuals and delivery flag for one independently assigned light.</summary>
    public sealed class CrystalLightBeam : MonoBehaviour
    {
        [SerializeField] private LogisticFlag flag;
        [SerializeField] private FlashlightController controller;
        [SerializeField] private Light2D groundLight;
        [SerializeField] private FlashlightActivationButton activationButton;
        [SerializeField] private Collider2D markerCollider;
        private Transform dragAreaTransform;
        private Vector2[] dragOutline;
        public LogisticFlag Flag => flag;
        public FlashlightActivationButton ActivationButton => activationButton;
        public Vector2 MarkerPosition => flag != null ? (Vector2)flag.transform.position : Vector2.zero;
        public bool MarkerVisible => flag != null && flag.gameObject.activeInHierarchy;
        public bool ContainsMarkerPoint(Vector2 point)
        {
            if (!MarkerVisible) return false;
            if (dragOutline == null || dragOutline.Length < 3 || dragAreaTransform == null)
                return markerCollider != null && markerCollider.OverlapPoint(point);
            Vector2 local = dragAreaTransform.InverseTransformPoint(point);
            for (int i = 0; i < dragOutline.Length; i++)
                if (Cross(dragOutline[i], dragOutline[(i + 1) % dragOutline.Length], local) < -.0001f) return false;
            return true;
        }
        private void CacheDragOutline()
        {
            // Enclose the separated frame corners and their empty interior without a collider.
            var frame = flag != null ? flag.FrameRenderer : null;
            Sprite sprite = frame != null ? frame.sprite : groundLight != null ? groundLight.lightCookieSprite : null;
            dragAreaTransform = frame != null ? frame.transform : groundLight != null ? groundLight.transform : null;
            if (sprite == null) return;
            var points = new List<Vector2>();
            var path = new List<Vector2>();
            for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
            {
                sprite.GetPhysicsShape(i, path);
                foreach (var p in path) if (!points.Contains(p)) points.Add(p);
            }
            if (points.Count < 3) return;
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var hull = new List<Vector2>();
            foreach (var p in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            int lowerCount = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--)
            {
                while (hull.Count > lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }
            hull.RemoveAt(hull.Count - 1);
            dragOutline = hull.ToArray();
        }
        private static float Cross(Vector2 a, Vector2 b, Vector2 p) =>
            (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        public void Initialize()
        {
            if (flag == null) flag = GetComponentInChildren<LogisticFlag>(true);
            if (controller == null) controller = GetComponentInChildren<FlashlightController>(true);
            if (groundLight == null && flag != null) groundLight = flag.GetComponentInChildren<Light2D>(true);
            if (markerCollider == null && flag != null) markerCollider = flag.GetComponent<Collider2D>();
            CacheDragOutline();
            if (flag != null)
            {
                var drag = flag.GetComponent<DragObj>();
                if (drag != null) { drag.CanDrag = false; drag.enabled = false; }
                flag.SetCrystalTarget(null);
            }
            foreach (var button in GetComponentsInChildren<FlashlightActivationButton>(true)) button.gameObject.SetActive(false);
            Clear();
        }
        public void ShowIdle(Vector2 position) => Show(position, null, true);
        public void Show(Vector2 position, ResourceRequester requester, bool lit, bool openingHex = false)
        {
            if (flag != null)
            {
                flag.transform.position = position;
                flag.transform.rotation = Quaternion.identity;
                flag.SetCrystalTarget(requester);
                flag.SetCrystalOpening(openingHex);
                flag.gameObject.SetActive(true);
                flag.enabled = true;
            }
            if (controller != null)
            {
                controller.SetCrystalTarget(position, lit);
                controller.gameObject.SetActive(true);
            }
            if (groundLight != null) { groundLight.gameObject.SetActive(lit); groundLight.enabled = lit; }
        }
        public void Clear()
        {
            if (controller != null)
            {
                controller.SetCrystalTarget(flag != null ? (Vector2)flag.transform.position : Vector2.zero, false);
                controller.gameObject.SetActive(false);
            }
            if (groundLight != null) groundLight.gameObject.SetActive(false);
            if (flag != null) { flag.SetCrystalTarget(null); flag.gameObject.SetActive(false); }
        }
    }
}
