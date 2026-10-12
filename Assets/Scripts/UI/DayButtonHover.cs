using UnityEngine;
using UnityEngine.EventSystems;

// Stretch the sliced frame and grow its icon by the same hover ratio.
public sealed class DayButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform frame;
    [SerializeField] private Vector2 normalSize = new Vector2(48, 48);
    [SerializeField] private float hoverMultiplier = 1.08f;
    private bool hovered;
    private Transform icon;
    private void Awake() => icon = transform.Find("Hourglass Icon");
    private void Update()
    {
        if (!frame) return;
        bool available = GetComponent<UnityEngine.UI.Button>().interactable;
        frame.sizeDelta = Vector2.Lerp(frame.sizeDelta, normalSize * (hovered && available ? hoverMultiplier : 1), 1 - Mathf.Exp(-16 * Time.unscaledDeltaTime));
        if (icon) icon.localScale = Vector3.one * (frame.sizeDelta.x / normalSize.x);
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; if (frame) frame.sizeDelta = normalSize; if (icon) icon.localScale = Vector3.one; }
}
