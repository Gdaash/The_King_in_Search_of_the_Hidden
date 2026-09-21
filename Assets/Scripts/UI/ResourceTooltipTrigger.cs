using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string _resourceName;
    [SerializeField] private ResourceType _resourceType;
    [SerializeField] private RectTransform _target;
    private bool _hovering;
    private LocalizationService _localization;

    public void Initialize(ResourceType resourceType, RectTransform target)
    {
        _resourceType = resourceType;
        _resourceName = resourceType != null ? resourceType.resourceName : string.Empty;
        _target = target;
    }

    public void Initialize(string resourceName, RectTransform target)
    {
        _resourceType = null;
        _resourceName = resourceName;
        _target = target;
    }

    private void OnEnable()
    {
        SubscribeToLocalization();
    }

    private void Start()
    {
        SubscribeToLocalization();
    }

    private void SubscribeToLocalization()
    {
        var service = LocalizationService.Instance;
        if (_localization == service) return;
        if (_localization != null) _localization.LanguageChanged -= RefreshTooltip;
        _localization = service;
        if (_localization != null) _localization.LanguageChanged += RefreshTooltip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        if (_target == null) _target = transform as RectTransform;
        SubscribeToLocalization();
        RefreshTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        if (_localization != null) _localization.LanguageChanged -= RefreshTooltip;
        _localization = null;
        HideTooltip();
    }

    private void RefreshTooltip()
    {
        if (_hovering && TooltipManager.Instance != null)
            TooltipManager.Instance.Show(LocalizedName(), _target);
    }

    private string LocalizedName()
    {
        var id = _resourceType != null ? _resourceType.name : _resourceName;
        var key = "resource." + id + ".name";
        var translated = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
        return translated != key ? translated : _resourceName;
    }

    private void HideTooltip()
    {
        if (!_hovering) return;
        _hovering = false;
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
