using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ResourceGlobalMonitor : MonoBehaviour
{
    [Header("Настройки UI")]
    [SerializeField] private GameObject rowPrefab;     // Префаб строки (UIResourceRow)
    [SerializeField] private Transform container;      // Куда складывать строки (Vertical Layout)
    [SerializeField] private float updateInterval = 0.5f; 

    // Словарь: Объект типа ресурса -> Скрипт строки в UI
    private Dictionary<ResourceType, UIResourceRow> _uiRows = new Dictionary<ResourceType, UIResourceRow>();
    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            RefreshGlobalResources();
            _timer = 0;
        }
    }

    private void RefreshGlobalResources()
    {
        // 1. Собираем общие данные со всех складов
        Dictionary<ResourceType, int> totals = new Dictionary<ResourceType, int>();

        foreach (Warehouse w in Warehouse.AllWarehouses)
        {
            foreach (var item in w.GetInventoryData())
            {
                if (totals.ContainsKey(item.Key))
                    totals[item.Key] += item.Value;
                else
                    totals.Add(item.Key, item.Value);
            }
        }

        // 2. Синхронизируем данные с UI
        foreach (var pair in totals)
        {
            ResourceType type = pair.Key;
            int count = pair.Value;

            if (!_uiRows.ContainsKey(type))
            {
                // Если строки для этого ресурса еще нет — создаем её
                GameObject newRow = Instantiate(rowPrefab, container);
                UIResourceRow rowScript = newRow.GetComponent<UIResourceRow>();
                
                // Сразу настраиваем иконку и имя из ассета ResourceType
                rowScript.UpdateRow(type.resourceIcon, type.resourceName, count);
                
                _uiRows.Add(type, rowScript);
            }
            else
            {
                // Если строка уже есть — просто обновляем количество
                _uiRows[type].UpdateRow(type.resourceIcon, type.resourceName, count);
            }
        }
    }
}