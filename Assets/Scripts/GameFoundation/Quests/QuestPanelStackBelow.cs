using UnityEngine;

namespace GameFoundation.Quests
{
    [DefaultExecutionOrder(110)]
    public sealed class QuestPanelStackBelow : MonoBehaviour
    {
        [SerializeField] private QuestPanel primaryPanel;
        [SerializeField, Min(0)] private float gap = 12;
        private void LateUpdate()
        {
            if (primaryPanel == null) return;
            var above = (RectTransform)primaryPanel.transform;
            var rect = (RectTransform)transform;
            rect.anchorMin = above.anchorMin; rect.anchorMax = above.anchorMax; rect.pivot = above.pivot;
            // Keep all quest HUD cards below the same popup layer as the primary card.
            if (rect.parent == above.parent && rect.GetSiblingIndex() != above.GetSiblingIndex() + 1)
                rect.SetSiblingIndex(above.GetSiblingIndex() + 1);
            rect.anchoredPosition = above.anchoredPosition + Vector2.down * (primaryPanel.HasQuest ? above.rect.height + gap : 0);
        }
    }
}
