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

    public UnityEngine.UI.Image GetResourceIcon(ResourceType resource)
    {
        foreach (var cell in globalCells)
            if (cell != null && cell.resource == resource) return cell.icon;
        return null;
    }

    private void EnableGlobalResources()
    {
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
