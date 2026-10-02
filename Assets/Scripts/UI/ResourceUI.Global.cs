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
    }

    [SerializeField] private GameObject globalTooltipPrefab;
    [SerializeField] private List<GlobalResourceCell> globalCells = new();
    private Coroutine _waitForResources;

    private void EnableGlobalResources()
    {
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
                cell.count.text = FormatGlobalCount(cell.resource, GlobalResourceManager.Instance.GetResourceAmount(cell.resource));
        _waitForResources = null;
    }

    private void DisableGlobalResources()
    {
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
                cell.count.text = FormatGlobalCount(type, amount);
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
