using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;
using UnityEngine;
using System;
using System.Collections.Generic;
using GameFoundation.MetaProgression;

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
    [SerializeField, HideInInspector] private List<ResourceDisplay> currentValuesDisplay = new List<ResourceDisplay>();

    private Dictionary<ResourceType, int> _resourceAmounts = new Dictionary<ResourceType, int>();

    public static event Action<ResourceType, int> OnResourceChanged;

    public IReadOnlyList<ResourceType> AvailableResources => availableResources;

    public int GetResourceAmount(ResourceType type)
    {
        if (type == null) return 0;
        if (!Application.isPlaying || Instance != this)
            return GetSavedAmount(type);
        if (_resourceAmounts.TryGetValue(type, out int amount))
            return amount;
        return 0;
    }

    private int GetInitialAmount(ResourceType type)
    {
        if (initialValues != null)
            foreach (var initial in initialValues)
                if (initial.resourceType == type)
                    return initial.startAmount;
        return 0;
    }

    private int GetSavedAmount(ResourceType type)
    {
        return PlayerPrefs.GetInt(saveKeyPrefix + type.Id, GetInitialAmount(type));
    }

    public void SetResourceAmount(ResourceType type, int amount)
    {
        using var save = PlayerPrefs.Batch();
        if (type == null) return;
        amount = Mathf.Max(0, amount);
        if (Application.isPlaying && Instance == this)
            DayResourceLedger.RecordResourceChange(type, amount - GetResourceAmount(type));
        PlayerPrefs.SetInt(saveKeyPrefix + type.Id, amount);
        PlayerPrefs.Save();
        if (Application.isPlaying && Instance == this)
        {
            _resourceAmounts[type] = amount;
            OnResourceChanged?.Invoke(type, amount);
        }
        RefreshDisplay();
    }

    public Dictionary<ResourceType, int> GetAllResourcesData()
    {
        return new Dictionary<ResourceType, int>(_resourceAmounts);
    }

    private void OnEnable() => PlayerPrefs.SelectionChanged += ReloadSelectedSlot;
    private void OnDisable() => PlayerPrefs.SelectionChanged -= ReloadSelectedSlot;
    private void ReloadSelectedSlot()
    {
        if (Instance != this) return;
        InitializeResources(); LoadResources();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // A persistent object must be a scene root. The manager is placed under
            // the scene's Managers group for editor organization, so detach it only
            // at runtime before preserving it across scene loads.
            if (transform.parent != null)
                transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            if (GetComponent<GameFoundation.Saves.SaveSlotClock>() == null)
                gameObject.AddComponent<GameFoundation.Saves.SaveSlotClock>();
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
        AddOptionalResource("Engineer");
        AddOptionalResource("Slinger");
        AddOptionalResource("Chicken");
        AddOptionalResource("Hide");
        AddOptionalResource("Leather");
        AddOptionalResource("LeatherArmor");
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

    private void AddOptionalResource(string id)
    {
        var resource = ResourceCatalog.Find(id);
        if (resource == null) return;
        availableResources ??= new List<ResourceType>();
        if (!availableResources.Contains(resource)) availableResources.Add(resource);
    }

    public void AddResource(ResourceType type, int amount)
    {
        using var save = PlayerPrefs.Batch();
        if (type == null) return;
        if (!_resourceAmounts.ContainsKey(type)) _resourceAmounts[type] = 0;

        DayResourceLedger.RecordResourceChange(type, amount);
        _resourceAmounts[type] += amount;
        SaveResource(type);
        OnResourceChanged?.Invoke(type, _resourceAmounts[type]);
        RefreshDisplay();
    }

    public bool TrySpendResource(ResourceType type, int cost)
    {
        using var save = PlayerPrefs.Batch();
        if (type == null) return false;
        if (!_resourceAmounts.ContainsKey(type)) _resourceAmounts[type] = 0;

        if (_resourceAmounts[type] >= cost)
        {
            DayResourceLedger.RecordResourceChange(type, -cost);
            _resourceAmounts[type] -= cost;
            SaveResource(type);
            OnResourceChanged?.Invoke(type, _resourceAmounts[type]);
            RefreshDisplay();
            return true;
        }
        
        Debug.Log($"Недостаточно ресурса: {type.resourceName}!");
        return false;
    }

    public bool TryExchangeResources(ResourceType inputA, int inputAAmount,
        ResourceType inputB, int inputBAmount, ResourceType output, int outputAmount,
        ResourceType inputC = null, int inputCAmount = 0)
    {
        if (inputA == null || inputB == null || output == null ||
            inputAAmount < 0 || inputBAmount < 0 || inputCAmount < 0 || outputAmount < 0 ||
            inputCAmount > 0 && inputC == null)
            return false;

        var costs = new Dictionary<ResourceType, int>();
        AddAmount(costs, inputA, inputAAmount);
        AddAmount(costs, inputB, inputBAmount);
        if (inputCAmount > 0) AddAmount(costs, inputC, inputCAmount);
        foreach (var cost in costs)
            if (GetResourceAmount(cost.Key) < cost.Value) return false;

        using var save = PlayerPrefs.Batch();
        var changes = new Dictionary<ResourceType, int>();
        foreach (var cost in costs)
        {
            DayResourceLedger.RecordResourceChange(cost.Key, -cost.Value);
            changes.Add(cost.Key, GetResourceAmount(cost.Key) - cost.Value);
        }
        DayResourceLedger.RecordResourceChange(output, outputAmount);
        changes[output] = (changes.TryGetValue(output, out int remaining) ? remaining : GetResourceAmount(output)) + outputAmount;
        foreach (var change in changes)
        {
            _resourceAmounts[change.Key] = change.Value;
            PlayerPrefs.SetInt(saveKeyPrefix + change.Key.Id, change.Value);
        }
        foreach (var change in changes)
            OnResourceChanged?.Invoke(change.Key, GetResourceAmount(change.Key));

        PlayerPrefs.Save();
        RefreshDisplay();
        return true;
    }

    private static void AddAmount(Dictionary<ResourceType, int> costs, ResourceType type, int amount)
    {
        if (amount == 0) return;
        costs.TryGetValue(type, out int existing);
        costs[type] = existing + amount;
    }

    private void SaveResource(ResourceType type)
    {
        if (type == null) return;
        string key = saveKeyPrefix + type.Id;
        PlayerPrefs.SetInt(key, _resourceAmounts[type]);
        PlayerPrefs.Save();
    }

    private void LoadResources()
    {
        var keys = new List<ResourceType>(_resourceAmounts.Keys);
        
        foreach (var res in keys)
        {
            string key = saveKeyPrefix + res.Id;
            
            _resourceAmounts[res] = GetSavedAmount(res);
            if (!PlayerPrefs.HasKey(key))
                PlayerPrefs.SetInt(key, _resourceAmounts[res]);
            
            OnResourceChanged?.Invoke(res, _resourceAmounts[res]);
        }
        
        PlayerPrefs.Save();
        RefreshDisplay();
    }

    public bool RefreshDisplay()
    {
        if (currentValuesDisplay == null) currentValuesDisplay = new List<ResourceDisplay>();
        var values = new List<ResourceDisplay>();
        if (availableResources == null) return false;
        foreach (var res in availableResources)
        {
            if (res == null) continue;
            values.Add(new ResourceDisplay
            {
                resourceType = res,
                currentAmount = GetResourceAmount(res)
            });
        }
        bool changed = currentValuesDisplay.Count != values.Count;
        for (int i = 0; !changed && i < values.Count; i++)
            changed = currentValuesDisplay[i].resourceType != values[i].resourceType ||
                currentValuesDisplay[i].currentAmount != values[i].currentAmount;
        if (!changed) return false;
        currentValuesDisplay.Clear();
        currentValuesDisplay.AddRange(values);
        return true;
    }

    [ContextMenu("Debug: Add 100 of ALL resources")]
    public void DebugAddAll()
    {
        if (availableResources == null) return;
        foreach (var res in availableResources)
            if (res != null)
                SetResourceAmount(res, GetResourceAmount(res) + 100);
    }

    [ContextMenu("Debug: Reset ALL resources to initial values")]
    public void DebugResetToInitial()
    {
        if (availableResources == null) return;
        foreach (var res in availableResources)
            if (res != null)
                SetResourceAmount(res, GetInitialAmount(res));
    }

    [ContextMenu("Debug: Clear ALL saves")]
    public void DebugClearAllSaves()
    {
        if (availableResources == null) return;
        foreach (var res in availableResources)
        {
            if (res == null) continue;
            string key = saveKeyPrefix + res.Id;
            PlayerPrefs.DeleteKey(key);
            if (Application.isPlaying && Instance == this)
            {
                _resourceAmounts[res] = GetInitialAmount(res);
                OnResourceChanged?.Invoke(res, _resourceAmounts[res]);
            }
        }
        PlayerPrefs.Save();
        RefreshDisplay();
    }
}
