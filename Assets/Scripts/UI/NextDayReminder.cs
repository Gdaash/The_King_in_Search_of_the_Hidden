using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class NextDayReminder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup hint;
    [SerializeField] private GameFoundation.UI.RisingLabelStars stars;
    private bool hovered;
    private void LateUpdate()
    {
        if (!hint) return;
        bool nextDayNeeded = DayCycleService.Instance != null && DayCycleService.Instance.EnteredToday;
        bool show = nextDayNeeded && !hovered;
        hint.alpha = show ? 1 : 0;
        hint.blocksRaycasts = false;
        hint.interactable = false;
        hint.transform.localScale = Vector3.one;
        if (stars) stars.Emit = nextDayNeeded;
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; if (hint) hint.alpha = 0; }
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; if (hint) { hint.alpha = 0; hint.transform.localScale = Vector3.one; } if (stars) stars.Emit = false; }
}
