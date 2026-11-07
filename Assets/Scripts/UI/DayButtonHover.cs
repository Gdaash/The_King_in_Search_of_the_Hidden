using UnityEngine;
using UnityEngine.EventSystems;

// One shared pulse drives the sliced frame and its nested hourglass icon.
public sealed class DayButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform frame;
    [SerializeField] private Vector2 normalSize = new Vector2(48, 48);
    [SerializeField] private float hoverMultiplier = 1.08f;
    private bool hovered;
    private RectTransform icon;
    private Vector2 normalPosition;
    private RectTransform hitArea;
    private TMPro.TextMeshProUGUI hoverLabel;
    private NextDayReminder reminderView;
    private readonly Vector3[] reminderCorners = new Vector3[4];
    private void Awake()
    {
        icon = (frame ? frame.Find("Hourglass Icon") : transform.Find("Hourglass Icon")) as RectTransform;
        if (!frame) return;
        normalPosition = frame.anchoredPosition;
        reminderView = GetComponent<NextDayReminder>();
        foreach (var tooltip in GetComponents<ResourceTooltipTrigger>()) tooltip.enabled = false;
        var image = frame.GetComponent<UnityEngine.UI.Image>(); if (image != null) image.type = UnityEngine.UI.Image.Type.Sliced;
        hitArea = new GameObject("Next day expanded hit area", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
        hitArea.SetParent(transform,false); hitArea.anchorMin = hitArea.anchorMax = hitArea.pivot = new Vector2(1,.5f);
        var hitImage = hitArea.GetComponent<UnityEngine.UI.Image>();
        hitImage.color = Color.clear; hitImage.raycastTarget = true;
        hitImage.canvasRenderer.cullTransparentMesh = false;
        hitArea.anchorMin = hitArea.anchorMax = hitArea.pivot = new Vector2(.5f,.5f);
        hoverLabel = new GameObject("Next day inline label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
        hoverLabel.transform.SetParent(frame,false); hoverLabel.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Pixellari Reading UI");
        hoverLabel.fontSize = 24; hoverLabel.alignment = TMPro.TextAlignmentOptions.Center; hoverLabel.raycastTarget = false;
        hoverLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        hoverLabel.rectTransform.anchorMin = hoverLabel.rectTransform.anchorMax = hoverLabel.rectTransform.pivot = new Vector2(1,.5f);
        var theme = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme");
        hoverLabel.color = theme != null ? theme.positiveLabelColor : Color.green;
    }
    internal static float DoublePulse(float time)
    {
        float phase = Mathf.Repeat(time, 1.8f);
        float beat = phase < .38f ? phase / .38f : phase >= .52f && phase < .9f ? (phase - .52f) / .38f : -1;
        return beat < 0 ? 0 : Mathf.Pow(Mathf.Sin(beat * Mathf.PI), 2);
    }
    private void Update()
    {
        if (!frame) return;
        bool available = GetComponent<UnityEngine.UI.Button>().interactable;
        bool firstNight = NextDayReminder.FirstNight;
        bool reminder = firstNight || GameFoundation.MetaProgression.DayCycleService.Instance != null &&
            GameFoundation.MetaProgression.DayCycleService.Instance.EnteredToday;
        float pulse = available && reminder ? DoublePulse(Time.unscaledTime) : 0;
        ApplyPulse(pulse, available);
    }
    private void ApplyPulse(float pulse, bool available)
    {
        float frameScale = Mathf.Max(hovered && available ? Mathf.Min(hoverMultiplier, 1.1f) : 1, 1 + .1f * pulse);
        float labelWidth = 0;
        if (hoverLabel != null)
        {
            hoverLabel.text = GameFoundation.Localization.GameText.Get("Следующий день");
            labelWidth = Mathf.Ceil(hoverLabel.GetPreferredValues(hoverLabel.text,Mathf.Infinity,Mathf.Infinity).x) + 24;
            hoverLabel.gameObject.SetActive(hovered && available);
            hoverLabel.rectTransform.sizeDelta = new Vector2(labelWidth,normalSize.y);
            hoverLabel.rectTransform.anchoredPosition = new Vector2(-normalSize.x,0);
        }
        float width = normalSize.x + (hovered && available ? labelWidth : 0);
        frame.sizeDelta = new Vector2(width,normalSize.y) * frameScale;
        frame.anchoredPosition = normalPosition - Vector2.right * (frame.sizeDelta.x-normalSize.x)*.5f;
        if (hitArea != null)
        {
            Vector2 min = frame.rect.min, max = frame.rect.max;
            var reminder = reminderView != null && reminderView.ReminderActive ? reminderView.ReminderRect : null;
            if (reminder != null)
            {
                reminder.GetWorldCorners(reminderCorners);
                foreach (var corner in reminderCorners)
                {
                    Vector2 point = frame.InverseTransformPoint(corner);
                    min = Vector2.Min(min,point); max = Vector2.Max(max,point);
                }
            }
            hitArea.sizeDelta = max-min;
            hitArea.anchoredPosition = (min+max)*.5f;
        }
        if (icon)
        {
            // Keep four Canvas units clear at every inside edge, including nonstandard icon sizes.
            icon.anchoredPosition = new Vector2((frame.rect.width-normalSize.x)*.5f,0);
            Vector2 inside = normalSize * frameScale - new Vector2(8, 8);
            float fit = Mathf.Min(inside.x / Mathf.Max(1, icon.rect.width), inside.y / Mathf.Max(1, icon.rect.height));
            float iconScale = Mathf.Min(1 + .2f * pulse, Mathf.Max(0, fit));
            icon.localScale = Vector3.one * iconScale;
        }
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; if (frame) { frame.sizeDelta = normalSize; frame.anchoredPosition = normalPosition; } if (hoverLabel) hoverLabel.gameObject.SetActive(false); if (icon) { icon.localScale = Vector3.one; icon.anchoredPosition = Vector2.zero; } }
}
