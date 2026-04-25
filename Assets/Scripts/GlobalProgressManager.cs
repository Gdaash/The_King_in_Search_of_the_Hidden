using UnityEngine;
using System.Collections.Generic;

public class GlobalProgressManager : MonoBehaviour
{
    [Header("Список всех типов данных")]
    [Tooltip("Перетащите сюда все ваши ScriptableObjects (GlobalStats)")]
    [SerializeField] private List<GlobalStats> allStats; // Здесь исправлено UnitStats на GlobalStats

    private void Awake()
    {
        // При запуске игры загружаем прогресс для каждого файла в списке
        LoadAll();
    }

    public void LoadAll()
    {
        if (allStats == null) return;

        foreach (var stat in allStats)
        {
            if (stat != null)
            {
                stat.LoadStats();
            }
        }
        Debug.Log("Все глобальные данные прогресса загружены.");
    }

    public void ResetAllProgress()
    {
        if (allStats == null) return;

        foreach (var stat in allStats)
        {
            if (stat != null)
            {
                stat.ResetProgress();
            }
        }
        Debug.Log("Весь прогресс сброшен.");
    }
}