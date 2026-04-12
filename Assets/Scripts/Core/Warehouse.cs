using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class Warehouse : MonoBehaviour
{
    // Список всех складов в сцене для поиска носильщиками
    public static List<Warehouse> AllWarehouses = new List<Warehouse>();

    [System.Serializable]
    public class ResourceEntry
    {
        public string typeName;
        public int amount;
    }

    [System.Serializable]
    public class StorageData
    {
        public List<ResourceEntry> resources = new List<ResourceEntry>();
    }

    [Header("Настройки визуала")]
    [SerializeField] private GameObject floatingIconPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2f, 0);

    private Dictionary<string, int> _inventory = new Dictionary<string, int>();
    private string _savePath;

    void OnEnable() => AllWarehouses.Add(this);
    void OnDisable() => AllWarehouses.Remove(this);

    void Awake()
    {
        // У каждого склада свой файл сохранения на основе его имени в иерархии
        _savePath = Path.Combine(Application.persistentDataPath, name + "_inventory.json");
        LoadResources();
    }

    // --- ЛОГИКА ДЛЯ НОСИЛЬЩИКА ---

    // 1. Проверка: есть ли ресурс (вызывается носильщиком при поиске склада)
    public bool HasResource(ResourceType type)
    {
        if (type == null) return false;
        return _inventory.ContainsKey(type.resourceName) && _inventory[type.resourceName] > 0;
    }

    // 2. Изъятие ресурса (вызывается носильщиком, когда он дошел до склада)
    public bool TryTakeResource(ResourceType type)
    {
        if (HasResource(type))
        {
            _inventory[type.resourceName]--;
            SaveResources();
            Debug.Log($"{name}: Ресурс {type.resourceName} забран. Осталось: {_inventory[type.resourceName]}");
            return true;
        }
        return false;
    }

    // 3. Прием ресурса (вызывается носильщиком при разгрузке)
    public void AddResource(ResourceType type, int amount, Sprite resourceSprite)
    {
        if (type == null) return;

        if (_inventory.ContainsKey(type.resourceName))
            _inventory[type.resourceName] += amount;
        else
            _inventory.Add(type.resourceName, amount);

        SpawnFloatingIcon(resourceSprite);
        SaveResources();

        Debug.Log($"{name}: Принято {type.resourceName}. Всего: {_inventory[type.resourceName]}");
    }

    // --- ВИЗУАЛ И СОХРАНЕНИЕ ---

    private void SpawnFloatingIcon(Sprite iconSprite)
    {
        if (floatingIconPrefab == null || iconSprite == null) return;

        GameObject iconObj = Instantiate(floatingIconPrefab, transform.position + spawnOffset, Quaternion.identity);
        if (iconObj.TryGetComponent(out SpriteRenderer sr))
        {
            sr.sprite = iconSprite;
        }
    }

    private void SaveResources()
    {
        StorageData saveData = new StorageData();
        foreach (var pair in _inventory)
        {
            saveData.resources.Add(new ResourceEntry { typeName = pair.Key, amount = pair.Value });
        }
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(_savePath, json);
    }

    private void LoadResources()
    {
        if (!File.Exists(_savePath)) return;

        try
        {
            string json = File.ReadAllText(_savePath);
            StorageData loadData = JsonUtility.FromJson<StorageData>(json);
            _inventory.Clear();
            foreach (var entry in loadData.resources)
            {
                _inventory.Add(entry.typeName, entry.amount);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка загрузки склада {name}: {e.Message}");
        }
    }
}