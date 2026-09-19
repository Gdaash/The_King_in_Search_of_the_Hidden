using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ResourceUI
{
    [SerializeField] private GameObject globalTooltipPrefab;
    private readonly Dictionary<ResourceType, TextMeshProUGUI> _globalCounts = new();
    private RectTransform _globalContent;
    private Coroutine _waitForResources;

    private void EnableGlobalResources()
    {
        GlobalResourceManager.OnResourceChanged += UpdateGlobalResource;

        if (TooltipManager.Instance == null && globalTooltipPrefab != null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                Instantiate(globalTooltipPrefab, canvas.transform).transform.SetAsLastSibling();
        }

        if (_globalContent == null)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);

            var content = new GameObject("Global resource entries", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            content.transform.SetParent(transform, false);
            _globalContent = content.GetComponent<RectTransform>();
            _globalContent.anchorMin = Vector2.zero;
            _globalContent.anchorMax = Vector2.one;
            _globalContent.offsetMin = Vector2.zero;
            _globalContent.offsetMax = Vector2.zero;

            var layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        _globalContent.gameObject.SetActive(true);
        if (_waitForResources != null) StopCoroutine(_waitForResources);
        _waitForResources = StartCoroutine(WaitForGlobalResources());
    }

    private IEnumerator WaitForGlobalResources()
    {
        while (GlobalResourceManager.Instance == null)
            yield return null;

        RefreshGlobalResources();
        _waitForResources = null;
    }

    private void DisableGlobalResources()
    {
        GlobalResourceManager.OnResourceChanged -= UpdateGlobalResource;
        if (_waitForResources != null)
        {
            StopCoroutine(_waitForResources);
            _waitForResources = null;
        }
    }

    private void RefreshGlobalResources()
    {
        foreach (var pair in GlobalResourceManager.Instance.GetAllResourcesData())
            UpdateGlobalResource(pair.Key, pair.Value);
    }

    private void UpdateGlobalResource(ResourceType type, int amount)
    {
        if (type == null || _globalContent == null) return;

        if (!_globalCounts.TryGetValue(type, out var countText))
        {
            countText = AddGlobalResourceCell(type);
            _globalCounts.Add(type, countText);
            var panel = (RectTransform)transform;
            float width = Mathf.Max(460, 8 + _globalCounts.Count * 92);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            panel.anchoredPosition = new Vector2(0, panel.anchoredPosition.y);
        }

        countText.text = amount.ToString();
    }

    private TextMeshProUGUI AddGlobalResourceCell(ResourceType type)
    {
        var cell = new GameObject(type.resourceName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(LayoutElement), typeof(ResourceTooltipTrigger));
        cell.transform.SetParent(_globalContent, false);
        cell.GetComponent<LayoutElement>().minWidth = 86;
        var cellRect = cell.GetComponent<RectTransform>();
        var background = cell.GetComponent<Image>();
        background.color = new Color(0.10f, 0.13f, 0.17f, 0.9f);
        background.raycastTarget = true;
        cell.GetComponent<ResourceTooltipTrigger>().Initialize(type.resourceName, cellRect);

        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(cell.transform, false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(7, 0);
        iconRect.sizeDelta = new Vector2(30, 30);
        var image = iconObject.GetComponent<Image>();
        image.sprite = type.resourceIcon;
        image.preserveAspect = type.resourceIcon != null;
        image.color = type.resourceIcon == null ? new Color(0.75f, 0.75f, 0.75f) : Color.white;
        image.raycastTarget = false;

        return AddCellText(cellRect, "Count", "0", 20, new Vector2(40, -14), new Vector2(-4, 14));
    }

    private TextMeshProUGUI AddCellText(RectTransform parent, string objectName, string value,
        float fontSize, Vector2 offsetMin, Vector2 offsetMax)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = resourceText != null ? resourceText.font : null;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }
}
