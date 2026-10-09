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
        private int line;
        private bool presenting;
        private GameObject presentationOverlay;
        private QuestPanel presentationDestination;
        [Header("Печать по словам")]
        [SerializeField, Min(.02f)] private float wordInterval = .12f;
        [SerializeField, Range(0, 1)] private float wordSoundVolume = .2f;
        [Header("Удержание кнопки диалога")]
        [SerializeField, Min(.1f)] private float holdDuration = 1f;
        [SerializeField] private Color holdFillColor = new Color(.55f, .7f, .85f, 1);
        private DialogueHoldButton holdButton;
        private Coroutine typing;
        public bool IsTyping { get; private set; }
        private string DoneKey(QuestDialogueDefinition d) => "foundation.dialogue." + d.id + ".seen";
        private void OnEnable() { nextButton.onClick.AddListener(RevealText); QuestProgress.Changed += Evaluate; }
        private void Awake()
        {
            holdButton = nextButton.GetComponent<DialogueHoldButton>();
            if (holdButton == null) holdButton = nextButton.gameObject.AddComponent<DialogueHoldButton>();
            holdButton.Configure(() => current != null && !presenting && !IsTyping, Advance, holdDuration, holdFillColor);
            window.SetActive(false);
        }
        private void RevealText() { if (holdButton != null && holdButton.ConsumeClick()) return; if (IsTyping) Advance(); }
        private void OnDisable()
        {
            nextButton.onClick.RemoveListener(RevealText); QuestProgress.Changed -= Evaluate;
            current = null; line = 0; window.SetActive(false);
            StopTyping();
            StopAllCoroutines();
            if (presentationOverlay != null) Destroy(presentationOverlay);
            if (presentationDestination != null) presentationDestination.Refresh();
            presentationDestination = null; presenting = false;
        }
        private void Start() => Evaluate();
        private bool Available(QuestDialogueDefinition d) => d != null && !string.IsNullOrWhiteSpace(d.id) &&
            d.lines != null && d.lines.Length > 0 &&
            (d.quest != null ? !QuestProgress.IsAccepted(d.quest) : SaveSlotPrefs.GetInt(DoneKey(d), 0) != 1) &&
            (d.prerequisite == null || (d.prerequisiteAccepted ? QuestProgress.IsAccepted(d.prerequisite) : QuestProgress.IsClaimed(d.prerequisite)));
        public void ActivateIfNeeded()
        {
            if (current != null || presenting) return;
            foreach (var d in conversations)
            {
                if (!Available(d)) continue;
                gameObject.SetActive(true);
                Evaluate(); return;
            }
        }
        public void Evaluate()
        {
            if (current != null || presenting) return;
            foreach (var d in conversations)
            {
                if (!Available(d)) continue;
                current = d; line = 0; transform.SetAsLastSibling(); window.SetActive(true); window.transform.SetAsLastSibling(); GameAudioController.PlayUI(GameAudioCue.DialogueOpen, .65f); ShowLine(); return;
            }
        }
        private void ShowLine()
        {
            speaker.text = current.speakerName;
            dialogue.text = current.lines[line];
            StopTyping();
            dialogue.ForceMeshUpdate();
            dialogue.maxVisibleCharacters = 0;
            IsTyping = true;
            typing = StartCoroutine(TypeWords());
            portrait.sprite = current.portrait;
            if (portrait.sprite != null) portrait.rectTransform.sizeDelta = portrait.sprite.rect.size * 2;
            UpdateButtonLabel();
        }
        private void UpdateButtonLabel()
        {
            if (current == null) return;
            nextLabel.text = IsTyping ? "…" : line < current.lines.Length - 1 ? "Далее" : current.quest != null ? "Взять задание" : "Закрыть";
            if (buttonTheme != null) nextLabel.color = line < current.lines.Length - 1 ? buttonTheme.neutralLabelColor :
                current.quest != null ? buttonTheme.positiveLabelColor : buttonTheme.negativeLabelColor;
        }
        public void Advance()
        {
            if (current == null || presenting) return;
            if (IsTyping) { StopTyping(); GameAudioController.PlayUI(GameAudioCue.DialogueNext, .45f); return; }
            if (++line < current.lines.Length) { GameAudioController.PlayUI(GameAudioCue.DialogueNext, .55f); ShowLine(); return; }
            var finished = current;
            presenting = finished.quest != null;
            SaveSlotPrefs.SetInt(DoneKey(finished), 1); SaveSlotPrefs.Save();
            window.SetActive(false); current = null;
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
        private IEnumerator PresentQuest(QuestPanel destinationPanel)
        {
            if (destinationPanel == null) { presenting = false; Evaluate(); yield break; }
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
            copy.name = "Accepted Quest Presentation";
            Destroy(copy.GetComponent<QuestPanel>());
            var align = copy.GetComponent<GameFoundation.UI.BesideMilitaryPanel>(); if (align != null) align.enabled = false;
            var stack = copy.GetComponent<QuestPanelStackBelow>(); if (stack != null) stack.enabled = false;
            var expansion = copy.GetComponent<QuestPanelExpansion>(); if (expansion != null) expansion.SetExpanded(true, true);
            if (expansion != null) expansion.KeepExpanded(true);
            var rect = (RectTransform)copy.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one * 1.35f;
            var group = copy.GetComponent<CanvasGroup>(); group.alpha = 1; group.blocksRaycasts = true; group.interactable = false;
            var realGroup = target.GetComponent<CanvasGroup>(); realGroup.alpha = 0; realGroup.blocksRaycasts = false; realGroup.interactable = false;
            Canvas.ForceUpdateCanvases();
            // The cloned rows need a layout pass before their preferred height is known.
            if (expansion != null) expansion.SetExpanded(true, true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
            // Reuse the dialogue button's authored background, font and hover feedback.
            var confirm = Instantiate(nextButton, overlayRect, false);
            confirm.name = "Quest Presentation OK";
            var clonedHold = confirm.GetComponent<DialogueHoldButton>();
            if (clonedHold != null) { clonedHold.enabled = false; Destroy(clonedHold); }
            confirm.gameObject.SetActive(true); confirm.interactable = true;
            confirm.onClick = new Button.ButtonClickedEvent();
            var label = confirm.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = "ОК"; if (buttonTheme != null) label.color = buttonTheme.neutralLabelColor; }
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot = new Vector2(.5f, .5f);
            confirmRect.localScale = Vector3.one;
            confirmRect.anchoredPosition = new Vector2(0, -rect.rect.height * 1.35f * .5f - confirmRect.rect.height * .5f - 16);
            bool confirmed = false;
            confirm.onClick.AddListener(() => { confirmed = true; confirm.interactable = false; });
            while (!confirmed) yield return null;
            confirm.gameObject.SetActive(false);
            if (expansion != null) expansion.KeepExpanded(false);
            var start = rect.position;
            GameAudioController.PlayUI(GameAudioCue.QuestFly, .6f);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .85f)
            {
                float eased = t * t * (3 - 2 * t);
                rect.position = Vector3.Lerp(start, target.TransformPoint(target.rect.center), eased);
                rect.localScale = Vector3.one * Mathf.Lerp(1.35f, 1, eased);
                if (expansion != null && t > .65f) expansion.SetExpanded(false);
                yield return null;
            }
            Destroy(presentationOverlay); presentationOverlay = null;
            presentationDestination = null; destinationPanel.Refresh(); presenting = false; Evaluate();
        }
    }
}
