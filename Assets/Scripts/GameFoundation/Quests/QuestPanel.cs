using System.Collections.Generic;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    public sealed class QuestPanel : MonoBehaviour
    {
        [SerializeField] private QuestCatalog catalog;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text status;
        [SerializeField] private RectTransform objectives;
        [SerializeField] private QuestObjectiveRow rowPrefab;
        [SerializeField] private List<QuestObjectiveRow> rows = new();
        [SerializeField] private RectTransform compactObjectives;
        [SerializeField] private QuestObjectiveRow compactRowPrefab;
        [SerializeField] private List<QuestObjectiveRow> compactRows = new();

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            QuestProgress.Changed += Refresh;
            Refresh();
        }
        private void Start() => Refresh();
        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            QuestProgress.Changed -= Refresh;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        public void Refresh()
        {
            var quest = QuestProgress.Current(catalog);
            if (quest == null) { gameObject.SetActive(false); return; }
            var resources = GlobalResourceManager.Instance;
            bool completed = QuestProgress.IsComplete(quest);
            title.text = quest.title;
            description.text = quest.description;
            status.text = completed ? "Задание выполнено" : "Доставьте ресурсы в портал";
            status.color = completed ? new Color(.5f, .85f, .5f) : new Color(.72f, .68f, .77f);
            while (rows.Count < quest.requirements.Count) rows.Add(Instantiate(rowPrefab, objectives, false));
            if (compactRowPrefab != null)
                while (compactRows.Count < quest.requirements.Count) compactRows.Add(Instantiate(compactRowPrefab, compactObjectives, false));
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < quest.requirements.Count);
                if (i < quest.requirements.Count)
                {
                    var goal = quest.requirements[i];
                    rows[i].Present(goal, resources != null && goal.resource != null ? resources.GetResourceAmount(goal.resource) : 0, completed);
                }
            }
            for (int i = 0; i < compactRows.Count; i++)
            {
                compactRows[i].gameObject.SetActive(i < quest.requirements.Count);
                if (i >= quest.requirements.Count) continue;
                var goal = quest.requirements[i];
                compactRows[i].Present(goal, resources != null && goal.resource != null ? resources.GetResourceAmount(goal.resource) : 0, completed);
            }
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
