using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Keeps an authored window inside its full-screen Canvas, including ultrawide views.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CanvasWindowFit : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float margin = 20f;
        private RectTransform window;
        private RectTransform canvasRect;

        private void OnEnable() => Fit();
        private void LateUpdate() => Fit();
        public void Fit()
        {
            if (window == null) window = (RectTransform)transform;
            if (canvasRect == null) canvasRect = GetComponentInParent<Canvas>()?.rootCanvas.transform as RectTransform;
            if (canvasRect == null || window.rect.width <= 0 || window.rect.height <= 0) return;
            float factor = Mathf.Clamp(Mathf.Min((canvasRect.rect.width - 2f * margin) / window.rect.width,
                (canvasRect.rect.height - 2f * margin) / window.rect.height), .01f, 1f);
            Vector3 scale = new(factor, factor, 1f);
            if (window.localScale != scale) window.localScale = scale;
        }
    }
}
