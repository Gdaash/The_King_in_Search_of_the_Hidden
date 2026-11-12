using System.Collections;
using GameFoundation.Saves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Audio;

namespace GameFoundation.Quests
{
    public sealed class QuestDialogueView : MonoBehaviour
    {
        [SerializeField] private QuestDialogueDefinition[] conversations;
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text speaker;
        [SerializeField] private TMP_Text dialogue;
        [SerializeField] private Image portrait;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private QuestPanel questPanel;
        [SerializeField] private QuestPanel parallelQuestPanel;
        [SerializeField] private GameFoundation.UI.ButtonVisualTheme buttonTheme;
        private QuestDialogueDefinition current;
        private string[] activeLines;
        private System.Action externalClosed;
        private bool temporaryConversation;
        private int line;
        private bool presenting;
        private GameObject presentationOverlay;
        private QuestPanel presentationDestination;
        private Coroutine rewardPresentation;
        [Header("Печать по словам")]
        [SerializeField, Min(.02f)] private float wordInterval = .12f;
        [SerializeField, Range(0, 1)] private float wordSoundVolume = .2f;
        private DialogueHoldButton holdButton;
        private Coroutine typing;
        private string displayedLanguage;
        public bool IsTyping { get; private set; }
        private string DoneKey(QuestDialogueDefinition d) => "foundation.dialogue." + d.id + ".seen";
        private void OnEnable() { nextButton.onClick.AddListener(RevealText); QuestProgress.Changed += Evaluate; }
        private void Awake()
        {
            holdButton = nextButton.GetComponent<DialogueHoldButton>();
            if (holdButton == null) holdButton = nextButton.gameObject.AddComponent<DialogueHoldButton>();
            holdButton.Configure(() => current != null && !presenting && !IsTyping, Advance);
            window.SetActive(false);
        }
        private void RevealText() { if (holdButton != null && holdButton.ConsumeClick()) return; if (IsTyping) Advance(); }
        private void OnDisable()
        {
            nextButton.onClick.RemoveListener(RevealText); QuestProgress.Changed -= Evaluate;
            if (temporaryConversation && current != null) Destroy(current.gameObject);
            temporaryConversation = false; externalClosed = null;
            current = null; line = 0; window.SetActive(false);
            StopTyping();
            StopAllCoroutines();
            rewardPresentation = null;
            if (presentationOverlay != null) Destroy(presentationOverlay);
            if (presentationDestination != null) presentationDestination.Refresh();
            presentationDestination = null; presenting = false;
        }
        private void Start() => ActivateIfNeeded();

        internal void QueueRewards()
        {
            if (rewardPresentation != null || ReadyRewardPanel() == null) return;
            gameObject.SetActive(true);
            rewardPresentation = StartCoroutine(PresentReadyRewards());
        }

        private QuestPanel ReadyRewardPanel()
        {
            if (questPanel != null && questPanel.ReadyReward != null) return questPanel;
            return parallelQuestPanel != null && parallelQuestPanel.ReadyReward != null ? parallelQuestPanel : null;
        }

        private IEnumerator PresentReadyRewards()
        {
            // Let the action that completed the quest start its feedback before covering the screen.
            yield return null;
            var constructionEffects = FindObjectsByType<GameFoundation.Base.BuildingConstructionEffect>();
            while (ReadyRewardPanel() is { } panel)
            {
                if (GameFoundation.MetaProgression.PortalCloudTransition.IsLoading ||
                    GameFoundation.Base.DayReportPopup.BlocksQuestDialogues ||
                    GameFoundation.Base.FirstExpeditionRecovery.Pending || GameFoundation.Base.ResidentRecovery.Pending ||
                    System.Array.Exists(constructionEffects, effect => effect != null && effect.IsPlaying))
                {
                    yield return null;
                    continue;
                }
                Evaluate();
                if (current != null || presenting)
                {
                    yield return null;
                    continue;
                }
                yield return PresentQuest(panel, panel.ReadyReward);
            }
            rewardPresentation = null;
        }

        public bool TryShowMessage(string name, string[] lines, System.Action closed, QuestDefinition rewardQuest = null)
        {
            if (current != null || presenting || GameFoundation.Base.DayReportPopup.BlocksQuestDialogues) return false;
            gameObject.SetActive(true);
            var definition = new GameObject("Temporary dialogue").AddComponent<QuestDialogueDefinition>();
            definition.transform.SetParent(transform, false); definition.speakerName = name; definition.lines = lines; definition.quest = rewardQuest;
            foreach (var source in conversations)
                if (source != null && source.speakerName == name) { definition.portrait = source.portrait; break; }
            externalClosed = closed; temporaryConversation = true; Begin(definition); return true;
        }
        private void Begin(QuestDialogueDefinition definition)
        {
            current = definition; activeLines = definition.lines;
            if (RoyalTributeService.IsRoyal(definition.quest) && RoyalTributeService.CartDebt > 0)
            {
                var expanded = new System.Collections.Generic.List<string>(activeLines);
                expanded.Add(RoyalTributeService.DebtLineTemplate); activeLines = expanded.ToArray();
            }
            line = 0; transform.SetAsLastSibling(); window.SetActive(true); window.transform.SetAsLastSibling();
            GameAudioController.PlayUI(GameAudioCue.DialogueOpen, .65f); ShowLine();
        }
        private bool Available(QuestDialogueDefinition d) => d != null && !string.IsNullOrWhiteSpace(d.id) &&
            !PlaytestSettings.TutorialsAndQuestsDisabled &&
            !GameFoundation.Base.DayReportPopup.BlocksQuestDialogues &&
            d.BuildingNightPassed && d.PrerequisiteNightPassed &&
            d.lines != null && d.lines.Length > 0 &&
            (d.quest != null ? !QuestProgress.IsAccepted(d.quest) : SaveSlotPrefs.GetInt(DoneKey(d), 0) != 1) &&
            (d.prerequisite == null || (d.prerequisiteAccepted ? QuestProgress.IsAccepted(d.prerequisite) : QuestProgress.IsClaimed(d.prerequisite)));
        public void ActivateIfNeeded()
        {
            if (current == null && !presenting && !GameFoundation.Base.FirstExpeditionRecovery.Pending && !GameFoundation.Base.ResidentRecovery.Pending)
            {
                foreach (var d in conversations)
                {
                    if (!Available(d)) continue;
                    gameObject.SetActive(true);
                    Evaluate();
                    break;
                }
            }
            QueueRewards();
        }
        public void Evaluate()
        {
            if (current != null || presenting || GameFoundation.Base.FirstExpeditionRecovery.Pending || GameFoundation.Base.ResidentRecovery.Pending) return;
            foreach (var d in conversations)
            {
                if (!Available(d)) continue;
                Begin(d); return;
            }
        }
        private void ShowLine()
        {
            displayedLanguage = GameFoundation.Localization.LocalizationService.Instance?.Language;
            speaker.text = GameFoundation.Localization.GameText.Get(current.speakerName);
            dialogue.text = activeLines[line] == RoyalTributeService.DebtLineTemplate ? RoyalTributeService.DebtLine :
                GameFoundation.Localization.GameText.Get(GameFoundation.Localization.GameText.Source(activeLines[line]));
            StopTyping();
            dialogue.ForceMeshUpdate();
            dialogue.maxVisibleCharacters = 0;
            IsTyping = true;
            typing = StartCoroutine(TypeWords());
            portrait.sprite = current.portrait;
            if (portrait.sprite != null) portrait.rectTransform.sizeDelta = portrait.sprite.rect.size * 2;
            UpdateButtonLabel();
        }
        private void Update()
        {
            if(current!=null&&!presenting&&displayedLanguage!=GameFoundation.Localization.LocalizationService.Instance?.Language)ShowLine();
        }
        private void UpdateButtonLabel()
        {
            if (current == null) return;
            nextLabel.text = line < activeLines.Length - 1 ? GameFoundation.Localization.GameText.Get("Далее") : current.quest != null ? GameFoundation.Localization.GameText.Get("Взять задание") : GameFoundation.Localization.GameText.Get("Закрыть");
            if (buttonTheme != null) nextLabel.color = line < activeLines.Length - 1 ? buttonTheme.neutralLabelColor :
                current.quest != null ? buttonTheme.positiveLabelColor : buttonTheme.neutralLabelColor;
        }
        public void Advance()
        {
            if (current == null || presenting) return;
            if (IsTyping) { StopTyping(); GameAudioController.PlayUI(GameAudioCue.DialogueNext, .45f); return; }
            if (++line < activeLines.Length) { GameAudioController.PlayUI(GameAudioCue.DialogueNext, .55f); ShowLine(); return; }
            var finished = current;
            presenting = finished.quest != null;
            if (!temporaryConversation) { SaveSlotPrefs.SetInt(DoneKey(finished), 1); SaveSlotPrefs.Save(); }
            window.SetActive(false); current = null;
            var callback = externalClosed; externalClosed = null;
            if (temporaryConversation) { temporaryConversation = false; Destroy(finished.gameObject); callback?.Invoke(); }
            if (finished.quest != null && QuestProgress.Accept(finished.quest)) StartCoroutine(PresentQuest(finished.quest.parallelQuest ? parallelQuestPanel : questPanel));
            else { presenting = false; Evaluate(); }
            if (finished.quest == null) GameAudioController.PlayUI(GameAudioCue.DialogueClose, .6f);
        }
        private void StopTyping()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null; IsTyping = false;
            dialogue.maxVisibleCharacters = int.MaxValue;
            holdButton?.ResetHold(); UpdateButtonLabel();
        }
        private IEnumerator TypeWords()
        {
            var info = dialogue.textInfo;
            int words = info.wordCount;
            for (int i = 0; i < words; i++)
            {
                int end = i + 1 < words ? info.wordInfo[i + 1].firstCharacterIndex : info.characterCount;
                dialogue.maxVisibleCharacters = end;
                GameAudioController.PlayUI(GameAudioCue.DialogueWord, wordSoundVolume, .94f, 1.06f, .02f);
                if (i + 1 < words) yield return new WaitForSecondsRealtime(wordInterval);
            }
            dialogue.maxVisibleCharacters = int.MaxValue; IsTyping = false; typing = null;
            holdButton?.ResetHold(); UpdateButtonLabel();
        }
        private static void FitPresentation(RectTransform panel, RectTransform confirm, RectTransform viewport)
        {
            const float margin = 24, gap = 16;
            float width = Mathf.Max(1, viewport.rect.width - margin * 2);
            float height = Mathf.Max(1, viewport.rect.height - margin * 2);
            float buttonScale = Mathf.Min(1, width / Mathf.Max(1, confirm.rect.width), height / Mathf.Max(1, confirm.rect.height));
            confirm.localScale = Vector3.one * buttonScale;
            float buttonHeight = confirm.rect.height * buttonScale;
            float scale = Mathf.Max(.01f, Mathf.Min(1.35f, width / Mathf.Max(1, panel.rect.width),
                Mathf.Max(1, height - buttonHeight - gap) / Mathf.Max(1, panel.rect.height)));
            panel.localScale = Vector3.one * scale;
            panel.anchoredPosition = new Vector2(0, (buttonHeight + gap) * .5f);
            confirm.anchoredPosition = new Vector2(0, panel.anchoredPosition.y - panel.rect.height * scale * .5f - gap - buttonHeight * .5f);
        }
        private IEnumerator PresentQuest(QuestPanel destinationPanel, QuestDefinition rewardQuest = null)
        {
            if (destinationPanel == null) { presenting = false; Evaluate(); yield break; }
            presenting = true;
            bool claimingReward = rewardQuest != null;
            destinationPanel.gameObject.SetActive(true); destinationPanel.Refresh();
            var target = (RectTransform)destinationPanel.transform;
            var canvas = target.GetComponentInParent<Canvas>().rootCanvas;
            presentationDestination = destinationPanel;
            presentationOverlay = new GameObject("Quest Presentation Overlay", typeof(RectTransform));
            var overlayRect = (RectTransform)presentationOverlay.transform;
            overlayRect.SetParent(canvas.transform, false);
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var dimmer = new GameObject("Presentation Dimmer", typeof(RectTransform), typeof(Image));
            var dimmerRect = (RectTransform)dimmer.transform;
            dimmerRect.SetParent(overlayRect, false);
            dimmerRect.anchorMin = Vector2.zero; dimmerRect.anchorMax = Vector2.one;
            dimmerRect.sizeDelta = Vector2.one * 8192;
            dimmer.GetComponent<Image>().color = new Color(0, 0, 0, .74f);
            // An independent visual copy keeps HUD alignment and layout ownership intact.
            var copy = Instantiate(target.gameObject, overlayRect, false);
            copy.name = claimingReward ? "Completed Quest Presentation" : "Accepted Quest Presentation";
            var copyPanel = copy.GetComponent<QuestPanel>();
            Destroy(copyPanel);
            var align = copy.GetComponent<GameFoundation.UI.BesideMilitaryPanel>(); if (align != null) align.enabled = false;
            var stack = copy.GetComponent<QuestPanelStackBelow>(); if (stack != null) stack.enabled = false;
            var expansion = copy.GetComponent<QuestPanelExpansion>(); if (expansion != null) expansion.SetExpanded(true, true);
            if (expansion != null) expansion.KeepExpanded(true);
            var rect = (RectTransform)copy.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one * 1.35f;
            var group = copy.GetComponent<CanvasGroup>(); group.alpha = 1; group.blocksRaycasts = true; group.interactable = false;
            if (!claimingReward)
            {
                var realGroup = target.GetComponent<CanvasGroup>();
                realGroup.alpha = 0; realGroup.blocksRaycasts = false; realGroup.interactable = false;
            }
            Canvas.ForceUpdateCanvases();
            // The cloned rows need a layout pass before their preferred height is known.
            if (expansion != null) expansion.SetExpanded(true, true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
            // Scale the layout parent so button hover owns the button's local scale.
            var buttonSlot = new GameObject("Quest Action Slot", typeof(RectTransform)).GetComponent<RectTransform>();
            buttonSlot.SetParent(overlayRect, false);
            var confirm = claimingReward ? destinationPanel.CreateRewardButton(buttonSlot, rewardQuest)
                : Instantiate(nextButton, buttonSlot, false);
            confirm.name = claimingReward ? "Claim Return Reward" : "Quest Presentation OK";
            var clonedHold = confirm.GetComponent<DialogueHoldButton>();
            if (clonedHold != null) { clonedHold.enabled = false; Destroy(clonedHold); }
            foreach (string decoration in new[] { "Hold Progress", "Hold LMB Tooltip" })
            {
                var child = confirm.transform.Find(decoration);
                if (child != null) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            }
            confirm.gameObject.SetActive(true); confirm.interactable = true;
            confirm.onClick = new Button.ButtonClickedEvent();
            var label = confirm.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !claimingReward)
            {
                label.text = GameFoundation.Localization.GameText.Get("Закрыть");
                if (buttonTheme != null) label.color = buttonTheme.neutralLabelColor;
            }
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot = new Vector2(.5f, .5f);
            confirmRect.anchoredPosition = Vector2.zero;
            confirmRect.localScale = Vector3.one;
            buttonSlot.sizeDelta = confirmRect.rect.size;
            FitPresentation(rect, buttonSlot, overlayRect);
            bool confirmed = false;
            confirm.onClick.AddListener(() =>
            {
                if (claimingReward && !destinationPanel.TryClaimReward(rewardQuest, confirmRect)) return;
                confirmed = true;
                confirm.interactable = false;
            });
            if (claimingReward)
            {
                var buttonGroup = buttonSlot.gameObject.AddComponent<CanvasGroup>();
                buttonGroup.blocksRaycasts = false;
                GameAudioController.PlayUI(GameAudioCue.QuestFly, .6f);
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .24f)
                {
                    FitPresentation(rect, buttonSlot, overlayRect);
                    var offset = Vector2.down * (48 * (1 - t) * (1 - t));
                    rect.anchoredPosition += offset;
                    buttonSlot.anchoredPosition += offset;
                    group.alpha = buttonGroup.alpha = t;
                    yield return null;
                }
                group.alpha = buttonGroup.alpha = 1;
                buttonGroup.blocksRaycasts = true;
            }
            while (!confirmed && (!claimingReward || destinationPanel.ReadyReward == rewardQuest))
            {
                FitPresentation(rect, buttonSlot, overlayRect);
                yield return null;
            }
            confirm.gameObject.SetActive(false);
            if (claimingReward)
            {
                Destroy(presentationOverlay); presentationOverlay = null;
                presentationDestination = null; presenting = false; Evaluate();
                yield break;
            }
            if (expansion != null) expansion.KeepExpanded(false);
            var start = rect.position;
            var presentationScale = rect.localScale;
            GameAudioController.PlayUI(GameAudioCue.QuestFly, .6f);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .85f)
            {
                float eased = t * t * (3 - 2 * t);
                rect.position = Vector3.Lerp(start, target.TransformPoint(target.rect.center), eased);
                rect.localScale = Vector3.Lerp(presentationScale, Vector3.one, eased);
                if (expansion != null && t > .65f) expansion.SetExpanded(false);
                yield return null;
            }
            Destroy(presentationOverlay); presentationOverlay = null;
            presentationDestination = null; destinationPanel.Refresh(); presenting = false; Evaluate();
        }
    }
}
