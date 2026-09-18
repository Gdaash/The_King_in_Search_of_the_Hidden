using UnityEngine;
using System.Collections.Generic;

public class ResourceGlobalMonitor : MonoBehaviour
{
    [Header("Настройки UI")]
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private float updateInterval = 0.5f; 

    private Dictionary<ResourceType, UIResourceRow> _uiRows = new Dictionary<ResourceType, UIResourceRow>();
    private float _timer;

    private void OnEnable()
    {
        if (GlobalResourceManager.Instance != null)
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChangedHandler;
        }
        
        RefreshGlobalResources();
    }

    private void OnDisable()
    {
        if (GlobalResourceManager.Instance != null)
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChangedHandler;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            RefreshGlobalResources();
            _timer = 0f;
        }
    }

    private void OnResourceChangedHandler(ResourceType type, int newAmount)
    {
        if (_uiRows.ContainsKey(type))
        {
            _uiRows[type].UpdateRow(type.resourceIcon, type.resourceName, newAmount);
        }
        else
        {
            RefreshGlobalResources();
        }
    }

    private void RefreshGlobalResources()
    {
        if (GlobalResourceManager.Instance == null) return;

        Dictionary<ResourceType, int> totals = GlobalResourceManager.Instance.GetAllResourcesData();

        foreach (var pair in totals)
        {
            ResourceType type = pair.Key;
            int count = pair.Value;

            if (!_uiRows.ContainsKey(type))
            {
                GameObject newRow = Instantiate(rowPrefab, container);
                UIResourceRow rowScript = newRow.GetComponent<UIResourceRow>();
                
                rowScript.UpdateRow(type.resourceIcon, type.resourceName, count);
                
                _uiRows.Add(type, rowScript);
            }
            else
            {
                _uiRows[type].UpdateRow(type.resourceIcon, type.resourceName, count);
            }
        }
    }
}