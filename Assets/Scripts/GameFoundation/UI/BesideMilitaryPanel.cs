using UnityEngine;

namespace GameFoundation.UI
{
    [DefaultExecutionOrder(100)]
    public sealed class BesideMilitaryPanel : MonoBehaviour
    {
        [SerializeField] private VerticalMilitaryPanel panel;
        [SerializeField] private float gap = 16;
        [SerializeField] private bool preserveVerticalPosition;
        private void LateUpdate() => Align();
        public void Align()
        {
            if (panel == null) return;
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(-panel.Width - gap, preserveVerticalPosition ? rect.anchoredPosition.y : 24);
        }
    }
}
