using UnityEngine;
using System.Collections.Generic;

public class Warehouse : MonoBehaviour
{
    // Статический список всех складов для быстрого поиска монитором и носильщиками
    public static List<Warehouse> AllWarehouses = new List<Warehouse>();

    [System.Serializable]
    public class ResourceEntry
    {
        public ResourceType type; // Ссылка на ScriptableObject ресурса
        public int amount;        // Количество
    }

    [System.Serializable]
    public struct ResourceEntryDebug 
    { 
        public string typeName; 
        public int amount; 
    }

    [Header("Настройки всплывающих иконок")]
    [SerializeField] private GameObject floatingIconPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float spawnRandomX = 0.5f;
    [Range(0.1f, 5f)] [SerializeField] private float iconMoveSpeed = 1.5f;
    [Range(0.5f, 5f)] [SerializeField] private float iconDuration = 1.2f;
    [SerializeField] private int iconSortingOrder = 50;

    [Header("Начальные ресурсы (Заполняется в инспекторе)")]
    [SerializeField] private List<ResourceEntry> initialResources = new List<ResourceEntry>();

    [Header("Содержимое склада (Только для чтения в Play Mode)")]
    [SerializeField] private List<ResourceEntryDebug> debugInventoryDisplay = new List<ResourceEntryDebug>();

    // Основной инвентарь склада (Ключ - объект ResourceType)
    private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();

    private void OnEnable() => AllWarehouses.Add(this);
    private void OnDisable() => AllWarehouses.Remove(this);

    private void Awake()
    {
        // Заполняем склад из списка начальных ресурсов
        foreach (var entry in initialResources)
        {
            if (entry.type != null && entry.amount > 0)
            {
                if (_inventory.ContainsKey(entry.type))
                    _inventory[entry.type] += entry.amount;
                else
                    _inventory.Add(entry.type, entry.amount);
            }
        }
        SyncDebugList();
    }

    // --- МЕТОДЫ ДЛЯ МОНИТОРА ---

    // Позволяет скрипту ResourceGlobalMonitor получить копию инвентаря
    public Dictionary<ResourceType, int> GetInventoryData()
    {
        return new Dictionary<ResourceType, int>(_inventory);
    }

    // --- МЕТОДЫ ВЗАИМОДЕЙСТВИЯ ---

    public bool HasResource(ResourceType type)
    {
        return type != null && _inventory.ContainsKey(type) && _inventory[type] > 0;
    }

    public bool TryTakeResource(ResourceType type)
    {
        if (HasResource(type))
        {
            _inventory[type]--;
            SyncDebugList();
            return true;
        }
        return false;
    }

    public void AddResource(ResourceType type, int amount, Sprite resourceSprite)
    {
        if (type == null) return;

        if (_inventory.ContainsKey(type))
            _inventory[type] += amount;
        else
            _inventory.Add(type, amount);

        SpawnFloatingIcon(resourceSprite);
        SyncDebugList();
    }

    // --- ВИЗУАЛ И СЛУЖЕБНЫЕ МЕТОДЫ ---

    private void SpawnFloatingIcon(Sprite iconSprite)
    {
        if (floatingIconPrefab == null || iconSprite == null) return;

        float randomX = Random.Range(-spawnRandomX, spawnRandomX);
        Vector3 finalPos = transform.position + spawnOffset + new Vector3(randomX, 0, 0);

        GameObject iconObj = Instantiate(floatingIconPrefab, finalPos, Quaternion.identity);
        
        if (iconObj.TryGetComponent(out SpriteRenderer sr))
            sr.sortingOrder = iconSortingOrder;

        if (iconObj.TryGetComponent(out FloatingIcon iconScript))
            iconScript.Init(iconSprite, iconMoveSpeed, iconDuration);
    }

    private void SyncDebugList()
    {
        debugInventoryDisplay.Clear();
        foreach (var pair in _inventory)
        {
            debugInventoryDisplay.Add(new ResourceEntryDebug 
            { 
                typeName = pair.Key.resourceName, 
                amount = pair.Value 
            });
        }
    }
}