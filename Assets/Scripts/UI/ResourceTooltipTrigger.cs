using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _resourceName;
    private RectTransform _target;
    private bool _hovering;

    public void Initialize(string resourceName, RectTransform target)
    {
        _resourceName = resourceName;
        _target = target;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Show(_resourceName, _target);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (!_hovering) return;
        _hovering = false;
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
