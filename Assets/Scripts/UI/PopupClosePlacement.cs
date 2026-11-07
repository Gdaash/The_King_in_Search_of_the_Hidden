using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Places the common close control relative to the visible frame, even after fitting.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(800)]
    public sealed class PopupClosePlacement : MonoBehaviour
    {
        public RectTransform frame;
        public const float Inset = 14f;
        private void OnEnable()
        {
            if (Application.isPlaying && TryGetComponent<Button>(out var button) &&
                TryGetComponent<Image>(out var hitArea))
            {
                hitArea.raycastTarget = true;
                if (!button.targetGraphic || !button.targetGraphic.isActiveAndEnabled)
                    button.targetGraphic = hitArea;
                transform.SetAsLastSibling();
            }
            Refresh();
        }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (frame == null || transform.parent == null) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(40, 40);
            var corner = new Vector3(frame.rect.xMax - Inset, frame.rect.yMax - Inset);
            if (frame == rect.parent)
                rect.localPosition = corner;
            else if (frame.parent == rect.parent)
                rect.localPosition = frame.localPosition + frame.localRotation * Vector3.Scale(corner, frame.localScale);
            else
                rect.position = frame.TransformPoint(corner);
        }
    }
}
