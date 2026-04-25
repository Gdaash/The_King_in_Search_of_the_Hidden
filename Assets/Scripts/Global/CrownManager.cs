using UnityEngine;
using System;

public class CrownManager : MonoBehaviour
{
    public static CrownManager Instance { get; private set; }

    [Header("Настройки сохранения")]
    [SerializeField] private string saveKey = "Global_Crowns";
    
    [Header("Текущее состояние")]
    [SerializeField] private int _currentCrowns;

    // Событие для обновления UI
    public static event Action<int> OnCrownsChanged;

    public int CurrentCrowns => _currentCrowns;

    private void Awake()
    {
        // Реализация Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCrowns();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Добавить короны
    public void AddCrowns(int amount)
    {
        _currentCrowns += amount;
        SaveCrowns();
        OnCrownsChanged?.Invoke(_currentCrowns);
    }

    // Попытаться потратить короны
    public bool TrySpendCrowns(int cost)
    {
        if (_currentCrowns >= cost)
        {
            _currentCrowns -= cost;
            SaveCrowns();
            OnCrownsChanged?.Invoke(_currentCrowns);
            return true;
        }
        
        Debug.Log("Недостаточно корон!");
        return false;
    }

    private void SaveCrowns()
    {
        PlayerPrefs.SetInt(saveKey, _currentCrowns);
        PlayerPrefs.Save();
    }

    private void LoadCrowns()
    {
        _currentCrowns = PlayerPrefs.GetInt(saveKey, 0);
        OnCrownsChanged?.Invoke(_currentCrowns);
    }

    // Для тестов через инспектор
    [ContextMenu("Add 100 Crowns")]
    public void DebugAdd() => AddCrowns(100);
}
