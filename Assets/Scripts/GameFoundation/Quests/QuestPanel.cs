using System.Collections.Generic;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    public sealed class QuestPanel : MonoBehaviour
    {
        [SerializeField] private QuestCatalog catalog;
        [SerializeField] private bool showParallelQuest;
        [SerializeField] private Text deadlineLabel;
        [SerializeField] private QuestObjectiveRow penaltyRow;
        private GameFoundation.MetaProgression.DayCycleService observedDay;
        public bool HasQuest => QuestProgress.Current(catalog, showParallelQuest) != null;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text status;
        [SerializeField] private RectTransform objectives;
        [SerializeField] private QuestObjectiveRow rowPrefab;
        [SerializeField] private List<QuestObjectiveRow> rows = new();
        [SerializeField] private RectTransform compactObjectives;
        [SerializeField] private QuestObjectiveRow compactRowPrefab;
        [SerializeField] private List<QuestObjectiveRow> compactRows = new();

        [SerializeField] private Button claimButton;
        [SerializeField] private RectTransform rewards;
        [SerializeField] private GameObject rewardsSection;
        [SerializeField] private LayoutElement rewardsViewport;
        [SerializeField] private List<QuestObjectiveRow> rewardRows = new();
        private void ClaimReward()
        {
            var quest = QuestProgress.Current(catalog, showParallelQuest);
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var source = claimButton != null ? (RectTransform)claimButton.transform : (RectTransform)transform;
            var origin = RectTransformUtility.WorldToScreenPoint(camera, source.TransformPoint(source.rect.center));
            var newUnlocks = new List<ContentUnlockDefinition>();
            if (quest != null)
                foreach (var unlock in quest.unlockRewards)
                    if (unlock != null && !ContentUnlocks.IsUnlocked(unlock)) newUnlocks.Add(unlock);
            if (QuestProgress.TryClaim(quest, GlobalResourceManager.Instance))
            {
                QuestRewardFlight.Play(canvas, origin, quest.resourceRewards);
                QuestRewardFlight.PlayUnlocks(canvas, origin, newUnlocks);
            }
            Refresh();
        }
        private void OnEnable()
        {
            if (claimButton != null) claimButton.onClick.AddListener(ClaimReward);
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            QuestProgress.Changed += Refresh;
            GameFoundation.Base.BuildingUpgradeService.Changed += Refresh;
            observedDay = GameFoundation.MetaProgression.DayCycleService.Instance;
            if (observedDay != null) observedDay.Changed += Refresh;
            Refresh();
        }
        private void Start()
        {
            if (observedDay == null)
            {
                observedDay = GameFoundation.MetaProgression.DayCycleService.Instance;
                if (observedDay != null) observedDay.Changed += Refresh;
            }
            Refresh();
        }
        private void OnDisable()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(ClaimReward);
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            QuestProgress.Changed -= Refresh;
            GameFoundation.Base.BuildingUpgradeService.Changed -= Refresh;
            if (observedDay != null) observedDay.Changed -= Refresh;
            observedDay = null;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        public void Refresh()
        {
            var quest = QuestProgress.Current(catalog, showParallelQuest);
            var visibility = GetComponent<CanvasGroup>();
            if (visibility != null) { visibility.alpha = quest != null ? 1 : 0; visibility.blocksRaycasts = quest != null; visibility.interactable = quest != null; }
            if (quest == null) return;
            var resources = GlobalResourceManager.Instance;
            bool completed = QuestProgress.IsComplete(quest);
            title.text = quest.title;
            description.text = quest.description;
            bool claimed = QuestProgress.IsClaimed(quest);
            bool ready = QuestProgress.IsReadyToClaim(quest, resources);
            if (deadlineLabel != null)
            {
                deadlineLabel.gameObject.SetActive(quest.deadlineDays > 0);
                deadlineLabel.text = "Осталось дней: " + QuestProgress.DaysRemaining(quest) + " / " + quest.deadlineDays;
                deadlineLabel.color = QuestProgress.DaysRemaining(quest) <= 5 ? new Color(.95f,.4f,.4f) : new Color(.94f,.86f,.62f);
            }
            if (penaltyRow != null)
            {
                penaltyRow.gameObject.SetActive(quest.penaltyResource != null && quest.penaltyAmount > 0);
                penaltyRow.PresentReward(quest.penaltyResource != null ? quest.penaltyResource.resourceIcon : null, "−" + quest.penaltyAmount, "Штраф");
                penaltyRow.GetComponentInChildren<Text>().color = new Color(.95f,.4f,.4f);
            }
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(ready); claimButton.interactable = ready;
                var label = claimButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = quest.consumeRequirementsOnClaim ? "Сдать руду" : "Забрать награду";
            }
            status.text = claimed ? "Награда получена" : completed ? "Задание выполнено" :
                ready && quest.consumeRequirementsOnClaim ? "Руда готова к сдаче" : string.IsNullOrEmpty(quest.inProgressStatus) ? "Доставьте ресурсы в портал" : quest.inProgressStatus;
            if (rewards != null)
            {
                int count = quest.resourceRewards.Count + quest.unlockRewards.Count;
                rewards.gameObject.SetActive(count > 0);
                if (rewardsSection != null) rewardsSection.SetActive(count > 0);
                while (rewardRows.Count < count) rewardRows.Add(Instantiate(rowPrefab, rewards, false));
                for (int i = 0; i < rewardRows.Count; i++)
                {
                    rewardRows[i].gameObject.SetActive(i < count);
                    if (i >= count) continue;
                    if (i < quest.resourceRewards.Count)
                    {
                        var reward = quest.resourceRewards[i];
                        rewardRows[i].PresentReward(reward?.resource != null ? reward.resource.resourceIcon : null, "+" + (reward?.amount ?? 0), "");
                    }
                    else
                    {
                        var unlock = quest.unlockRewards[i - quest.resourceRewards.Count];
                        rewardRows[i].PresentReward(unlock != null ? unlock.icon : null, "", unlock != null ? unlock.title : "Не назначена разблокировка");
                    }
                }
                if (rewardsViewport != null)
                {
                    float height = 0;
                    for (int i = 0; i < count; i++) height += rewardRows[i].GetComponent<LayoutElement>().preferredHeight + 6;
                    rewardsViewport.preferredHeight = Mathf.Min(210, height);
                }
            }
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
                    rows[i].Present(goal, goal.CurrentAmount(resources != null ? resources.GetResourceAmount : null), completed);
                }
            }
            for (int i = 0; i < compactRows.Count; i++)
            {
                compactRows[i].gameObject.SetActive(i < quest.requirements.Count);
                if (i >= quest.requirements.Count) continue;
                var goal = quest.requirements[i];
                compactRows[i].Present(goal, goal.CurrentAmount(resources != null ? resources.GetResourceAmount : null), completed);
            }
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
