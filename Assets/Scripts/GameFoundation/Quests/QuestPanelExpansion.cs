using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    public sealed class QuestPanelExpansion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform detailsContent;
        [SerializeField] private LayoutElement detailsLayout;
        [SerializeField] private CanvasGroup detailsGroup;
        [SerializeField, Min(.01f)] private float duration = .25f;
        private bool hovered;
        private bool keepExpanded;
        private float progress;
        public void OnPointerEnter(PointerEventData _) => SetExpanded(true);
        public void OnPointerExit(PointerEventData _) => SetExpanded(false);
        public void SetExpanded(bool expanded, bool immediately = false)
        {
            if (keepExpanded) expanded = true;
            hovered = expanded;
            if (immediately) { progress = expanded ? 1 : 0; Apply(); }
        }
        public void KeepExpanded(bool value)
        {
            keepExpanded = value;
            if (value) SetExpanded(true, true);
        }
        private void OnDisable() { hovered = false; progress = 0; Apply(); }
        private void OnEnable() { hovered = false; progress = 0; Apply(); }
        private void LateUpdate()
        {
            progress = Mathf.MoveTowards(progress, hovered ? 1 : 0, Time.unscaledDeltaTime / duration);
            Apply();
        }
        private void Apply()
        {
            if (detailsContent == null || detailsLayout == null || detailsGroup == null) return;
            float eased = progress * progress * (3 - 2 * progress);
            float height = LayoutUtility.GetPreferredHeight(detailsContent) * eased;
            if (!Mathf.Approximately(detailsLayout.preferredHeight, height)) detailsLayout.preferredHeight = height;
            detailsGroup.alpha = eased;
        }
    }
}
