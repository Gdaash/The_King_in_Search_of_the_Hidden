using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;

namespace GameFoundation.UI
{
    [ExecuteAlways]
    public sealed class VerticalMilitaryPanel : MonoBehaviour
    {
        public RectTransform title;
        public RectTransform content;
        public Text emptyLabel;
        public WorldMilitaryRosterItemView itemTemplate;
        [SerializeField] private Vector2 cellSize = new(72, 90);
        [SerializeField] private Vector2 cellSpacing = new(8, 8);
        [SerializeField] private Vector2 padding = new(16, 16);
        [SerializeField] private float headerHeight = 56;
        [SerializeField] private float minimumWidth = 120;
        public float Width => ((RectTransform)transform).rect.width;

        private void LateUpdate()
        {
            if (content == null) return;
            var panel = (RectTransform)transform;
            int count = 0;
            foreach (Transform child in content)
                if (child.gameObject.activeSelf && child.TryGetComponent<WorldMilitaryRosterItemView>(out var item) && item != itemTemplate) count++;
            Vector2 step = cellSize + cellSpacing;
            int rows = Mathf.Max(1, Mathf.FloorToInt((panel.rect.height - headerHeight - padding.y * 2 + cellSpacing.y) / Mathf.Max(1, step.y)));
            int columns = Mathf.Max(1, Mathf.CeilToInt(count / (float)rows));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, count==0 ? 0 : Mathf.Max(minimumWidth, columns * step.x - cellSpacing.x + padding.x * 2));
            var visibility=GetComponent<CanvasGroup>();
            if(visibility){visibility.alpha=count>0?1:0;visibility.blocksRaycasts=visibility.interactable=count>0;}
            int index = 0;
            foreach (Transform child in content)
            {
                if (!child.gameObject.activeSelf || !child.TryGetComponent<WorldMilitaryRosterItemView>(out var item) || item == itemTemplate) continue;
                var rect = (RectTransform)child;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = cellSize;
                rect.anchoredPosition = new Vector2((columns - 1) * step.x * .5f - (index / rows) * step.x, -(index % rows) * step.y);
                index++;
            }
        }
    }
}
