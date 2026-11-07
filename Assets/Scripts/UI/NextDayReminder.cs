using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class NextDayReminder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup hint;
    [SerializeField] private GameFoundation.UI.RisingLabelStars stars;
    private bool hovered;
    public bool ReminderActive => FirstNight || DayCycleService.Instance != null && DayCycleService.Instance.EnteredToday;
    public RectTransform ReminderRect => hint != null ? (RectTransform)hint.transform : null;
    public const string SleepLine = "Ну вот, рыцарь, теперь и желудки сыты, и крыша над головой есть. Можно и поспать! Отправляйся на покой, а утром продолжим восстанавливать земли портала.";
    public const string SleepSeenKey = "foundation.tutorial.firstNightDialogue";
    private GameFoundation.Quests.QuestDialogueView dialogue;
    private float nextDialogueCheck;
    private float firstNightVisibleAt = -1f;
    public static bool FirstNight => !GameFoundation.PlaytestSettings.TutorialsAndQuestsDisabled &&
        DayCycleService.Instance != null && DayCycleService.Instance.Day == 1 &&
        GameFoundation.Base.BuildingUpgradeService.IsBuilt("housing");
    private void LateUpdate()
    {
        bool firstNight = FirstNight;
        if (!firstNight) firstNightVisibleAt = -1f;
        else if (firstNightVisibleAt < 0f) firstNightVisibleAt = Time.unscaledTime;
        if (firstNight && Time.unscaledTime - firstNightVisibleAt >= 2f && Time.unscaledTime >= nextDialogueCheck &&
            GameFoundation.Saves.SaveSlotPrefs.GetInt(SleepSeenKey, 0) == 0 &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Base")
        {
            nextDialogueCheck = Time.unscaledTime + .5f;
            if (!dialogue) dialogue = Object.FindAnyObjectByType<GameFoundation.Quests.QuestDialogueView>(FindObjectsInactive.Include);
            if (dialogue) dialogue.TryShowMessage("Стайк Пью", new[] { SleepLine }, () =>
            {
                GameFoundation.Saves.SaveSlotPrefs.SetInt(SleepSeenKey, 1);
                GameFoundation.Saves.SaveSlotPrefs.Save();
            });
        }
        if (!hint) return;
        var renderedLabel = hint.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (renderedLabel != null)
        {
            Vector2 preferred = renderedLabel.GetPreferredValues(renderedLabel.text,Mathf.Infinity,Mathf.Infinity);
            var sourceLabel = hint.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (sourceLabel != null) sourceLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Ceil(preferred.x));
            ReminderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Ceil(preferred.x)+32);
        }
        bool nextDayNeeded = DayCycleService.Instance != null && DayCycleService.Instance.EnteredToday;
        nextDayNeeded |= firstNight;
        bool show = nextDayNeeded && !hovered;
        hint.alpha = show ? 1 : 0;
        hint.blocksRaycasts = false;
        hint.interactable = false;
        hint.transform.localScale = Vector3.one;
        if (stars)
        {
            stars.Emit = nextDayNeeded;
            stars.EmissionMultiplier = stars.LifetimeMultiplier = 1;
        }
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; if (hint) hint.alpha = 0; }
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; if (hint) { hint.alpha = 0; hint.transform.localScale = Vector3.one; } if (stars) { stars.Emit = false; stars.EmissionMultiplier = stars.LifetimeMultiplier = 1; } }
}
