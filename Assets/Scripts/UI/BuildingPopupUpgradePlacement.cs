using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Attaches the upgrade footer to the actual popup bounds, including runtime window resizing.</summary>
    [ExecuteAlways]
    public sealed class BuildingPopupUpgradePlacement : MonoBehaviour
    {
        [SerializeField] private RectTransform frame;
        [SerializeField, Min(0)] private float gap = 12;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (frame == null) return;
            var rect = (RectTransform)transform;
            Vector3 edge = frame.TransformPoint(new Vector3(frame.rect.center.x, frame.rect.yMin, 0));
            Vector3 local = rect.parent.InverseTransformPoint(edge);
            local.y -= gap + rect.rect.height * .5f;
            rect.localPosition = local;
        }
    }
}
