using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Linq;

namespace Evolution_adventure.Scripts
{
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

public class ConfigValueChecker : MonoBehaviour
{
    [Header("Настройки файла")]
    public string fileName = "GlobalSettingConfig.sav";
    
    [Header("Список ключей для проверки (все должны быть true)")]
    public List<string> keysToCheck = new List<string> { "1Wasps", "1Goblins", "Level1" };

    [Header("Событие")]
    public UnityEvent OnAllKeysAreTrue;

    // Классы для структуры JSON
    [Serializable]
    public class LevelEntry
    {
        public string Key;
        public bool Value;
    }

    [Serializable]
    public class ConfigData
    {
        public List<LevelEntry> LevelActiveList;
    }

    void Start() => CheckConfig();

    public void CheckConfig()
    {
        string filePath = Path.Combine(Application.dataPath, "Data", fileName);

        if (!File.Exists(filePath)) return;

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            
            // Если файл начинается сразу с { "LevelActiveList": ...
            ConfigData data = JsonUtility.FromJson<ConfigData>(jsonContent);

            if (data?.LevelActiveList == null) return;

            // Проверяем: для каждого ключа из keysToCheck должен найтись элемент в конфиге с Value == true
            bool allConditionsMet = keysToCheck.All(keyToFind => 
                data.LevelActiveList.Any(entry => entry.Key == keyToFind && entry.Value == true)
            );

            if (allConditionsMet)
            {
                Debug.Log("Все условия по ключам выполнены!");
                OnAllKeysAreTrue?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка парсинга конфига: {e.Message}");
        }
    }
}
}