using GameFoundation.MetaProgression;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class NextDayReminder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup hint;
    [SerializeField] private GameFoundation.UI.RisingLabelStars stars;
    private bool hovered;
    private Button button;
    private RectTransform clockFrame;
    private CanvasGroup clockVisibility;
    private RectTransform introductionLayer;
    private bool introducing;
    public bool BlocksActivation => introducing ||
        FirstNight && GameFoundation.Saves.SaveSlotPrefs.GetInt(SleepSeenKey, 0) == 0;
    public bool ReminderActive => !BlocksActivation && (FirstNight || DayCycleService.Instance != null && DayCycleService.Instance.EnteredToday);
    public RectTransform ReminderRect => hint != null ? (RectTransform)hint.transform : null;
    public const string SleepLine = "Ну вот, рыцарь, теперь и желудки сыты, и крыша над головой есть. Можно и поспать! Отправляйся на покой, а утром продолжим восстанавливать земли портала.";
    public const string SleepSeenKey = "foundation.tutorial.firstNightDialogue";
    private GameFoundation.Quests.QuestDialogueView dialogue;
    private float nextDialogueCheck;
    private float firstNightVisibleAt = -1f;
    public static bool FirstNight => !GameFoundation.PlaytestSettings.TutorialsAndQuestsDisabled &&
        DayCycleService.Instance != null && DayCycleService.Instance.Day == 1 &&
        GameFoundation.Base.BuildingUpgradeService.IsBuilt("housing");
    private void Awake()
    {
        button = GetComponent<Button>();
        clockFrame = GetComponent<DayButtonHover>()?.Frame;
        if (clockFrame != null)
            clockVisibility = clockFrame.gameObject.AddComponent<CanvasGroup>();
    }

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
            if (dialogue) dialogue.TryShowMessage("Стайк Пью", new[] { SleepLine }, OnSleepDialogueClosed);
        }
        if (button != null && (firstNight || introducing)) button.interactable = !BlocksActivation;
        if (clockVisibility != null && !introducing) clockVisibility.alpha = BlocksActivation ? 0f : 1f;
        if (!hint) return;
        var renderedLabel = hint.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (renderedLabel != null)
        {
            Vector2 preferred = renderedLabel.GetPreferredValues(renderedLabel.text,Mathf.Infinity,Mathf.Infinity);
            var sourceLabel = hint.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (sourceLabel != null) sourceLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Ceil(preferred.x));
            ReminderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Ceil(preferred.x)+32);
        }
        bool nextDayNeeded = ReminderActive;
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
    private void OnSleepDialogueClosed()
    {
        if (isActiveAndEnabled && clockVisibility != null && hint != null)
        {
            introducing = true;
            StartCoroutine(IntroduceNextDay());
        }
        GameFoundation.Saves.SaveSlotPrefs.SetInt(SleepSeenKey, 1);
        GameFoundation.Saves.SaveSlotPrefs.Save();
    }

    private IEnumerator IntroduceNextDay()
    {
        var viewport = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        introductionLayer = new GameObject("First Next Day Introduction", typeof(RectTransform)).GetComponent<RectTransform>();
        introductionLayer.SetParent(viewport, false);
        introductionLayer.anchorMin = Vector2.zero;
        introductionLayer.anchorMax = Vector2.one;
        introductionLayer.sizeDelta = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        RectTransform clock = CopyClock();
        RectTransform reminder = CopyHint();
        reminder.gameObject.SetActive(false);
        Vector3 center = viewport.TransformPoint(viewport.rect.center);
        clock.position = center;
        clock.localScale = Vector3.one * 20f;
        clockVisibility.alpha = 0f;
        yield return new WaitForSecondsRealtime(.18f);

        const float clockDuration = .9f, hintDuration = .62f;
        for (float elapsed = 0f; elapsed < clockDuration * .5f + hintDuration; elapsed += Time.unscaledDeltaTime)
        {
            float clockProgress = Mathf.Clamp01(elapsed / clockDuration);
            float clockTravel = Mathf.SmoothStep(0, 1, clockProgress);
            clock.position = Vector3.Lerp(center, clockFrame.TransformPoint(clockFrame.rect.center), clockTravel);
            clock.localScale = Vector3.one * Mathf.Lerp(20f, 1f, 1f - Mathf.Pow(1f - clockProgress, 3f));
            if (clockProgress >= 1f && clock.gameObject.activeSelf)
            {
                clock.gameObject.SetActive(false);
                clockVisibility.alpha = 1f;
            }
            if (elapsed >= clockDuration * .5f)
            {
                reminder.gameObject.SetActive(true);
                float progress = Mathf.SmoothStep(0, 1, (elapsed - clockDuration * .5f) / hintDuration);
                reminder.position = Vector3.Lerp(center, ReminderRect.TransformPoint(ReminderRect.rect.center), progress);
            }
            yield return null;
        }

        ResetIntroduction();
    }

    private RectTransform CopyClock()
    {
        var frame = CopyImage(clockFrame.GetComponent<Image>(), introductionLayer);
        frame.name = "Flying Clock";
        var icon = clockFrame.Find("Hourglass Icon")?.GetComponent<Image>();
        if (icon != null)
            CopyImage(icon, frame).anchoredPosition = icon.rectTransform.anchoredPosition;
        return frame;
    }

    private static RectTransform CopyImage(Image source, RectTransform parent)
    {
        var graphic = new GameObject(source.name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var rect = graphic.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = source.rectTransform.rect.size;
        graphic.sprite = source.sprite;
        graphic.type = source.type;
        graphic.color = source.color;
        graphic.material = source.material;
        graphic.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        graphic.preserveAspect = source.preserveAspect;
        graphic.fillCenter = source.fillCenter;
        graphic.raycastTarget = false;
        return rect;
    }

    private RectTransform CopyHint()
    {
        var source = ReminderRect;
        var copy = Instantiate(source, introductionLayer, false);
        copy.name = "Flying Next Day Hint";
        copy.anchorMin = copy.anchorMax = copy.pivot = Vector2.one * .5f;
        copy.sizeDelta = source.rect.size;
        foreach (var graphic in copy.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        var group = copy.GetComponent<CanvasGroup>();
        if (group == null) group = copy.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return copy;
    }

    private void ResetIntroduction()
    {
        introducing = false;
        if (introductionLayer != null)
        {
            introductionLayer.gameObject.SetActive(false);
            Destroy(introductionLayer.gameObject);
            introductionLayer = null;
        }
        if (clockVisibility != null) clockVisibility.alpha = 1f;
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true; if (hint) hint.alpha = 0; }
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { StopAllCoroutines(); ResetIntroduction(); hovered = false; if (hint) { hint.alpha = 0; hint.transform.localScale = Vector3.one; } if (stars) { stars.Emit = false; stars.EmissionMultiplier = stars.LifetimeMultiplier = 1; } }
}
