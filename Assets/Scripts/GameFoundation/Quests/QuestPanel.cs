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
        public bool HasDeadline => (QuestProgress.Current(catalog, showParallelQuest)?.deadlineDays ?? 0) > 0;
        [SerializeField] private Text title;
        [SerializeField] private Sprite completedStarSprite;
        private RisingLabelStars completedStars;
        private Text completionTitle;
        private Color originalTitleColor;
        private bool titleColorCaptured;
        private bool CanClaimHere => gameObject.scene.name == "Base";
        private void RefreshCompletionTitle(bool completed)
        {
            if (title == null || gameObject.scene.name != "World") return;
            if (completionTitle == null)
            {
                completionTitle = title;
                titleColorCaptured = false;
                foreach (var mirror in GetComponentsInChildren<QuestTitleMirror>(true))
                {
                    if (mirror.source != title || mirror.target == null) continue;
                    completionTitle = mirror.target;
                    // The source remains a transparent data label for the mirrored header.
                    var hiddenColor = title.color; hiddenColor.a = 0; title.color = hiddenColor;
                    break;
                }
            }
            if (completedStars != null && completedStars.transform.parent != completionTitle.transform)
            {
                completedStars.transform.SetParent(completionTitle.transform, false);
                completedStars.Configure(completionTitle.rectTransform, completedStarSprite);
            }
            if (!titleColorCaptured) { originalTitleColor = completionTitle.color; titleColorCaptured = true; }
            var theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
            var green = theme != null ? theme.positiveLabelColor : new Color(.5f, .85f, .5f);
            completionTitle.color = completed ? green : originalTitleColor;
            if (completed && completedStars == null && completedStarSprite != null)
            {
                var effect = new GameObject("Completed Quest Stars", typeof(RectTransform), typeof(CanvasRenderer), typeof(RisingLabelStars));
                var rect = (RectTransform)effect.transform;
                rect.SetParent(completionTitle.transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                completedStars = effect.GetComponent<RisingLabelStars>();
                completedStars.Configure(completionTitle.rectTransform, completedStarSprite);
            }
            if (completedStars != null) { completedStars.color = green; completedStars.Emit = completed; completedStars.gameObject.SetActive(completed); }
        }
        [SerializeField] private Text description;
        [SerializeField] private Text status;
        [SerializeField] private RectTransform objectives;
        [SerializeField] private QuestObjectiveRow rowPrefab;
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
            if (!CanClaimHere) return;
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
            if (completedStars != null) completedStars.Emit = false;
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
            if (objectives != null) objectives.gameObject.SetActive(false);
            if (description != null)
            {
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                var layout = description.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    // Let Text report its full preferred height at the available width.
                    layout.minHeight = -1;
                    layout.preferredHeight = -1;
                    layout.flexibleHeight = 0;
                }
            }
            if (status != null)
            {
                status.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (description != null) status.fontSize = description.fontSize;
                var layout = status.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minHeight = -1;
                    layout.preferredHeight = -1;
                    layout.flexibleHeight = 0;
                }
            }
            var quest = QuestProgress.Current(catalog, showParallelQuest);
            var visibility = GetComponent<CanvasGroup>();
            if (visibility != null) { visibility.alpha = quest != null ? 1 : 0; visibility.blocksRaycasts = quest != null; visibility.interactable = quest != null; }
            if (quest == null) { RefreshCompletionTitle(false); if (claimButton != null) claimButton.gameObject.SetActive(false); return; }
            var resources = GlobalResourceManager.Instance;
            bool completed = QuestProgress.IsComplete(quest);
            title.text = GameFoundation.Localization.GameText.Get(quest.title);
            description.text = GameFoundation.Localization.GameText.Get(quest.description);
            bool claimed = QuestProgress.IsClaimed(quest);
            bool ready = QuestProgress.IsReadyToClaim(quest, resources);
            RefreshCompletionTitle(completed || ready);
            if (deadlineLabel != null)
            {
                deadlineLabel.gameObject.SetActive(quest.deadlineDays > 0);
                deadlineLabel.text = GameFoundation.Localization.GameText.Get("Осталось дней: ") + QuestProgress.DaysRemaining(quest) + " / " + quest.deadlineDays;
                deadlineLabel.color = QuestProgress.DaysRemaining(quest) <= 5 ? new Color(.95f,.4f,.4f) : new Color(.94f,.86f,.62f);
            }
            if (penaltyRow != null)
            {
                penaltyRow.gameObject.SetActive(quest.penaltyResource != null && quest.penaltyAmount > 0);
                penaltyRow.PresentReward(quest.penaltyResource != null ? quest.penaltyResource.resourceIcon : null, "−" + quest.penaltyAmount, GameFoundation.Localization.GameText.Get("Штраф"));
                penaltyRow.GetComponentInChildren<Text>().color = new Color(.95f,.4f,.4f);
            }
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(CanClaimHere && ready); claimButton.interactable = CanClaimHere && ready;
                var label = claimButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = quest.consumeRequirementsOnClaim ? GameFoundation.Localization.GameText.Get("Сдать руду") :
                    GameFoundation.Localization.GameText.Get(quest.resourceRewards.Count == 0 && quest.unlockRewards.Count == 0 ? "Завершить задание" : "Забрать награду");
            }
            status.text = claimed ? GameFoundation.Localization.GameText.Get("Награда получена") : completed ? GameFoundation.Localization.GameText.Get("Задание выполнено") :
                ready && quest.consumeRequirementsOnClaim ? GameFoundation.Localization.GameText.Get("Руда готова к сдаче") : string.IsNullOrEmpty(quest.inProgressStatus) ? GameFoundation.Localization.GameText.Get("Доставьте ресурсы в портал") : GameFoundation.Localization.GameText.Get(quest.inProgressStatus);
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
                        rewardRows[i].PresentReward(unlock != null ? unlock.icon : null, "", unlock != null ? unlock.title : GameFoundation.Localization.GameText.Get("Не назначена разблокировка"));
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
            if (compactRowPrefab != null)
                while (compactRows.Count < quest.requirements.Count) compactRows.Add(Instantiate(compactRowPrefab, compactObjectives, false));
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
