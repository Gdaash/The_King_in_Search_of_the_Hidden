using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class ResourceInitialValue
{
    public ResourceType resourceType;
    public int startAmount = 0;
}

[System.Serializable]
public class ResourceDisplay
{
    public ResourceType resourceType;
    public int currentAmount;
}

public class GlobalResourceManager : MonoBehaviour
{
    public static GlobalResourceManager Instance { get; private set; }

    [Header("Настройки сохранения")]
    [SerializeField] private string saveKeyPrefix = "Global_Resource_";

    [Header("Доступные ресурсы")]
    [SerializeField] private List<ResourceType> availableResources;

    [Header("Стартовые значения")]
    [SerializeField] private List<ResourceInitialValue> initialValues = new List<ResourceInitialValue>();

    [Header("Отображение в инспекторе")]
    [SerializeField] private List<ResourceDisplay> currentValuesDisplay = new List<ResourceDisplay>();

    private Dictionary<ResourceType, int> _resourceAmounts = new Dictionary<ResourceType, int>();

    public static event Action<ResourceType, int> OnResourceChanged;

    public int GetResourceAmount(ResourceType type)
    {
        if (type != null && _resourceAmounts.TryGetValue(type, out int amount))
            return amount;
        return 0;
    }

    public Dictionary<ResourceType, int> GetAllResourcesData()
    {
        return new Dictionary<ResourceType, int>(_resourceAmounts);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeResources();
            LoadResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeResources()
    {
        _resourceAmounts.Clear();
        if (availableResources != null)
        {
            foreach (var res in availableResources)
            {
                if (res != null && !_resourceAmounts.ContainsKey(res))
                {
                    _resourceAmounts[res] = 0;
                }
            }
        }
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (type == null) return;
        if (!_resourceAmounts.ContainsKey(type)) _resourceAmounts[type] = 0;

        _resourceAmounts[type] += amount;
        SaveResource(type);
        OnResourceChanged?.Invoke(type, _resourceAmounts[type]);
        RefreshDisplay();
    }

    public bool TrySpendResource(ResourceType type, int cost)
    {
        if (type == null) return false;
        if (!_resourceAmounts.ContainsKey(type)) _resourceAmounts[type] = 0;

        if (_resourceAmounts[type] >= cost)
        {
            _resourceAmounts[type] -= cost;
            SaveResource(type);
            OnResourceChanged?.Invoke(type, _resourceAmounts[type]);
            RefreshDisplay();
            return true;
        }
        
        Debug.Log($"Недостаточно ресурса: {type.resourceName}!");
        return false;
    }

    private void SaveResource(ResourceType type)
    {
        if (type == null) return;
        string key = saveKeyPrefix + type.name; 
        PlayerPrefs.SetInt(key, _resourceAmounts[type]);
        PlayerPrefs.Save();
    }

    private void LoadResources()
    {
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        
        foreach (var res in keys)
        {
            string key = saveKeyPrefix + res.name;
            
            if (PlayerPrefs.HasKey(key))
            {
                int loadedAmount = PlayerPrefs.GetInt(key, 0);
                _resourceAmounts[res] = loadedAmount;
            }
            else
            {
                int startAmount = 0;
                foreach (var initial in initialValues)
                {
                    if (initial.resourceType == res)
                    {
                        startAmount = initial.startAmount;
                        break;
                    }
                }
                _resourceAmounts[res] = startAmount;
                PlayerPrefs.SetInt(key, startAmount);
            }
            
            OnResourceChanged?.Invoke(res, _resourceAmounts[res]);
        }
        
        PlayerPrefs.Save();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        currentValuesDisplay.Clear();
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        foreach (var res in keys)
        {
            currentValuesDisplay.Add(new ResourceDisplay
            {
                resourceType = res,
                currentAmount = _resourceAmounts[res]
            });
        }
    }

    [ContextMenu("Refresh Inspector Display")]
    public void RefreshDisplayContextMenu()
    {
        RefreshDisplay();
    }

    [ContextMenu("Debug: Add 100 of ALL resources")]
    public void DebugAddAll()
    {
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        foreach (var res in keys)
        {
            AddResource(res, 100);
        }
    }

    [ContextMenu("Debug: Reset ALL resources to initial values")]
    public void DebugResetToInitial()
    {
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        foreach (var res in keys)
        {
            int startAmount = 0;
            foreach (var initial in initialValues)
            {
                if (initial.resourceType == res)
                {
                    startAmount = initial.startAmount;
                    break;
                }
            }
            
            _resourceAmounts[res] = startAmount;
            SaveResource(res);
            OnResourceChanged?.Invoke(res, startAmount);
        }
        RefreshDisplay();
    }

    [ContextMenu("Debug: Clear ALL saves")]
    public void DebugClearAllSaves()
    {
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        foreach (var res in keys)
        {
            string key = saveKeyPrefix + res.name;
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }
}