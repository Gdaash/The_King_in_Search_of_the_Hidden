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
    private void Awake() => icon = (frame ? frame.Find("Hourglass Icon") : transform.Find("Hourglass Icon")) as RectTransform;
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
        frame.sizeDelta = normalSize * frameScale;
        if (icon)
        {
            // Keep four Canvas units clear at every inside edge, including nonstandard icon sizes.
            Vector2 inside = frame.rect.size - new Vector2(8, 8) - 2 * new Vector2(Mathf.Abs(icon.anchoredPosition.x), Mathf.Abs(icon.anchoredPosition.y));
            float fit = Mathf.Min(inside.x / Mathf.Max(1, icon.rect.width), inside.y / Mathf.Max(1, icon.rect.height));
            float iconScale = Mathf.Min(1 + .2f * pulse, Mathf.Max(0, fit));
            icon.localScale = Vector3.one * iconScale;
        }
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; if (frame) frame.sizeDelta = normalSize; if (icon) icon.localScale = Vector3.one; }
}
