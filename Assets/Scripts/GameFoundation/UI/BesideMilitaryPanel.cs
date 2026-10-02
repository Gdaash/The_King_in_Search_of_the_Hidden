using UnityEngine;

namespace GameFoundation.UI
{
    public sealed class BesideMilitaryPanel : MonoBehaviour
    {
        [SerializeField] private VerticalMilitaryPanel panel;
        [SerializeField] private float gap = 16;
        private void LateUpdate()
        {
            if (panel != null) ((RectTransform)transform).anchoredPosition = new Vector2(-panel.Width - gap, 24);
        }
    }
}
