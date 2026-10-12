using GameFoundation.Quests;
using UnityEngine;

/// <summary>Anchors the quest stack directly above the fixed five-line journal area.</summary>
[DefaultExecutionOrder(150)]
[ExecuteAlways]
public sealed class JournalHudLayout : MonoBehaviour
{
    [SerializeField] private RectTransform log;
    [SerializeField] private QuestPanel[] quests;
    [SerializeField] private UnityEngine.UI.Button toggleButton;
    [SerializeField] private RectTransform arrow;
    [SerializeField] private bool expanded;
    [SerializeField] private GameFoundation.UI.EnemyRosterView enemies;
    private CanvasGroup logGroup;
    public bool Expanded => expanded;

    private void OnEnable() { if (toggleButton) toggleButton.onClick.AddListener(Toggle); }
    private void OnDisable() { if (toggleButton) toggleButton.onClick.RemoveListener(Toggle); }
    public void Toggle() => expanded = !expanded;
    public void SetExpanded(bool value) => expanded = value;

    private void LateUpdate()
    {
        if (!log || quests == null) return;
        if (!logGroup) logGroup = log.GetComponent<CanvasGroup>();
        if (logGroup) logGroup.alpha = expanded ? 1 : 0;
        if (arrow) arrow.localEulerAngles = new Vector3(0, 0, expanded ? 0 : 180);
        // Details animate before this pass; use the updated layout heights for the whole stack.
        foreach(var quest in quests) if(quest && quest.HasQuest) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)quest.transform);
        float bottom = log.anchoredPosition.y + (expanded ? log.rect.height : 0);
        foreach (var quest in quests)
        {
            if (!quest) continue;
            var rect = (RectTransform)quest.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            // Grow upwards when a quest expands, keeping its lower edge against the journal.
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(log.anchoredPosition.x, bottom);
            if (!Application.isPlaying || quest.HasQuest) bottom += rect.rect.height;
        }
        if(enemies)
        {
            float amount = 0;
            foreach(var q in quests) if(q && q.HasQuest) amount=Mathf.Max(amount,q.GetComponent<QuestPanelExpansion>().ExpansionAmount);
            if(Application.isPlaying) enemies.SetCompactAmount(amount);
            var rect = (RectTransform)enemies.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
            rect.anchoredPosition=new Vector2(log.anchoredPosition.x,bottom);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,log.rect.width);
        }
    }
}
