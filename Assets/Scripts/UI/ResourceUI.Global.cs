using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class ResourceUI
{
    [Serializable]
    private sealed class GlobalResourceCell
    {
        public ResourceType resource;
        public TextMeshProUGUI count;
        public UnityEngine.UI.Image icon;
        [NonSerialized] public bool initialized, hasAmount;
        [NonSerialized] public int lastAmount;
        [NonSerialized] public Vector3 originalScale;
        [NonSerialized] public Color originalColor;
        [NonSerialized] public Coroutine animation;
    }

    [SerializeField] private GameObject globalTooltipPrefab;
    [SerializeField] private List<GlobalResourceCell> globalCells = new();
    private Coroutine _waitForResources;
    private RectTransform resourceStrip, leftOuterCap, rightOuterCap;
    private Canvas resourceCanvas;
    private readonly Vector3[] resourceBounds = new Vector3[4];

    public UnityEngine.UI.Image GetResourceIcon(ResourceType resource)
    {
        foreach (var cell in globalCells)
            if (cell != null && cell.resource == resource) return cell.icon;
        return null;
    }

    private void EnableGlobalResources()
    {
        CenterGlobalResourceStrip();
        foreach (var cell in globalCells)
        {
            if (cell?.count == null) continue;
            if (!cell.initialized)
            {
                cell.originalScale = cell.count.transform.localScale;
                cell.originalColor = cell.count.color;
                cell.initialized = true;
            }
            cell.hasAmount = false;
        }
        GlobalResourceManager.OnResourceChanged += UpdateGlobalResource;
        GameFoundation.Base.BuildingUpgradeService.Changed += RefreshHousingCount;
        if (TooltipManager.Instance == null && globalTooltipPrefab != null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                Instantiate(globalTooltipPrefab, canvas.transform).transform.SetAsLastSibling();
        }
        if (_waitForResources != null) StopCoroutine(_waitForResources);
        _waitForResources = StartCoroutine(WaitForGlobalResources());
    }

    private void CenterGlobalResourceStrip()
    {
        var rect = resourceStrip = (RectTransform)transform;
        resourceCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (TryGetComponent<GameFoundation.UI.BesideMilitaryPanel>(out var militaryAlignment))
            militaryAlignment.enabled = false;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
        // The two outer caps frame the whole strip. The inner edge uses a compact connector.
        if (rect.Find("Human") is RectTransform human && rect.Find("Crown") is RectTransform firstResource &&
            rect.Find("Human Left Cap") is RectTransform humanLeft && rect.Find("Human Right Cap") is RectTransform humanRight &&
            rect.Find("Left Cap") is RectTransform innerCap && rect.Find("Right Cap") is RectTransform outerRight)
        {
            var connector = Resources.Load<Sprite>("UI/Resource Strip Connector");
            if (connector != null)
            {
                const float connectorGap = 24f;
                float edge = human.anchoredPosition.x + human.rect.width * (1f - human.pivot.x);
                float shift = edge + connectorGap - firstResource.anchoredPosition.x;
                foreach (var cell in globalCells)
                    if (cell?.icon != null && cell.icon.transform.parent is RectTransform cellRect && cellRect != human)
                        cellRect.anchoredPosition += new Vector2(shift, 0);
                humanRight.gameObject.SetActive(false);
                var connectorImage = innerCap.GetComponent<UnityEngine.UI.Image>();
                ResourceIconSizing.Apply(connectorImage, connector);
                innerCap.anchorMin = innerCap.anchorMax = new Vector2(0, 1);
                innerCap.pivot = new Vector2(0, 1);
                innerCap.localScale = Vector3.one;
                innerCap.anchoredPosition = new Vector2(edge, human.anchoredPosition.y);
                SetOuterResourceCaps(rect, humanLeft, outerRight);
                return;
            }
        }
        SetOuterResourceCaps(rect, rect.Find("Left Cap") as RectTransform, rect.Find("Right Cap") as RectTransform);
    }

    private void SetOuterResourceCaps(RectTransform rect, RectTransform left, RectTransform right)
    {
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        var corners = new Vector3[4];
        foreach (var cell in globalCells)
        {
            if (cell?.icon == null || !(cell.icon.transform.parent is RectTransform cellRect) ||
                !cellRect.gameObject.activeSelf) continue;
            cellRect.GetWorldCorners(corners);
            min = Mathf.Min(min, rect.InverseTransformPoint(corners[0]).x);
            max = Mathf.Max(max, rect.InverseTransformPoint(corners[2]).x);
        }
        if (min <= max && left != null && right != null)
        {
            left.anchorMin = left.anchorMax = right.anchorMin = right.anchorMax = new Vector2(0, 1);
            float y = left.anchoredPosition.y;
            left.pivot = right.pivot = new Vector2(1, 1);
            left.anchoredPosition = new Vector2(min - rect.rect.xMin, y);
            right.GetComponent<UnityEngine.UI.Image>().sprite = left.GetComponent<UnityEngine.UI.Image>().sprite;
            right.localScale = new Vector3(-1, 1, 1);
            right.sizeDelta = left.sizeDelta;
            right.anchoredPosition = new Vector2(max - rect.rect.xMin, y);
            right.gameObject.SetActive(true);
        }
        leftOuterCap = left;
        rightOuterCap = right;
        AlignResourceStripToScreen();
    }

    private void LateUpdate() => AlignResourceStripToScreen();

    private void AlignResourceStripToScreen()
    {
        if (resourceCanvas == null || leftOuterCap == null || rightOuterCap == null ||
            !(resourceStrip.parent is RectTransform parent)) return;

        Camera camera = resourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : resourceCanvas.worldCamera;
        leftOuterCap.GetWorldCorners(resourceBounds);
        float left = ScreenEdge(camera, false);
        rightOuterCap.GetWorldCorners(resourceBounds);
        float right = ScreenEdge(camera, true);
        var midpoint = resourceCanvas.pixelRect.center;
        float currentX = (left + right) * .5f;
        if (Mathf.Abs(midpoint.x - currentX) < .05f) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, midpoint, camera, out var target) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(currentX, midpoint.y), camera, out var current))
            resourceStrip.anchoredPosition += new Vector2(target.x - current.x, 0);
    }

    private float ScreenEdge(Camera camera, bool right)
    {
        float edge = right ? float.NegativeInfinity : float.PositiveInfinity;
        foreach (var corner in resourceBounds)
        {
            float x = RectTransformUtility.WorldToScreenPoint(camera, corner).x;
            edge = right ? Mathf.Max(edge, x) : Mathf.Min(edge, x);
        }
        return edge;
    }

    private IEnumerator WaitForGlobalResources()
    {
        while (GlobalResourceManager.Instance == null)
            yield return null;
        foreach (var cell in globalCells)
            if (cell?.resource != null && cell.count != null)
            {
                cell.lastAmount = GlobalResourceManager.Instance.GetResourceAmount(cell.resource);
                cell.hasAmount = true;
                cell.count.text = FormatGlobalCount(cell.resource, cell.lastAmount);
            }
        _waitForResources = null;
    }

    private void DisableGlobalResources()
    {
        foreach (var cell in globalCells)
        {
            if (cell == null) continue;
            if (cell.animation != null) StopCoroutine(cell.animation);
            cell.animation = null;
            if (cell.count != null && cell.initialized)
            {
                cell.count.transform.localScale = cell.originalScale;
                cell.count.color = cell.originalColor;
            }
        }
        GlobalResourceManager.OnResourceChanged -= UpdateGlobalResource;
        GameFoundation.Base.BuildingUpgradeService.Changed -= RefreshHousingCount;
        if (_waitForResources != null)
        {
            StopCoroutine(_waitForResources);
            _waitForResources = null;
        }
    }

    private void UpdateGlobalResource(ResourceType type, int amount)
    {
        foreach (var cell in globalCells)
            if (cell?.resource == type && cell.count != null)
            {
                cell.count.text = FormatGlobalCount(type, amount);
                if (cell.hasAmount && amount != cell.lastAmount && isActiveAndEnabled)
                {
                    if (cell.animation != null) StopCoroutine(cell.animation);
                    cell.animation = StartCoroutine(AnimateCount(cell.count, cell.originalScale, cell.originalColor,
                        amount > cell.lastAmount ? IncreaseColor : DecreaseColor));
                }
                cell.lastAmount = amount;
                cell.hasAmount = true;
            }
    }

    private static string FormatGlobalCount(ResourceType type, int amount)
    {
        var housing = GameFoundation.Base.BuildingUpgradeService.Catalog?.Find("housing");
        return housing != null && type == housing.capacityResource
            ? $"{amount} / {GameFoundation.Base.BuildingUpgradeService.Capacity("housing")}"
            : amount.ToString();
    }

    private void RefreshHousingCount()
    {
        var human = GameFoundation.Base.BuildingUpgradeService.Catalog?.Find("housing")?.capacityResource;
        if (human != null && GlobalResourceManager.Instance != null)
            UpdateGlobalResource(human, GlobalResourceManager.Instance.GetResourceAmount(human));
    }
}
