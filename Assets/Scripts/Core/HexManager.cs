using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HexManager : MonoBehaviour
{
    [System.Serializable]
    public class HexPrefabData
    {
        public string contentID; // ID для GlobalStats
        public GameObject prefab;
        public bool isMandatory; // Обязательный префаб
        public bool autoUnlockHex; // Гекс откроется сразу после спавна
        public bool startLocked; // Обязательно ли улучшение для появления
    }

    [System.Serializable]
    public class HexGroupSettings
    {
        public int groupID;
        public List<HexPrefabData> prefabsForGroup;
    }

    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats globalHexStats;

    [Header("Настройки групп префабов")]
    [SerializeField] private List<HexGroupSettings> groups;

    private void Start()
    {
        // Перед генерацией убеждаемся, что данные загружены
        if (globalHexStats != null) 
        {
            globalHexStats.LoadStats();
            // Лог для проверки, что менеджер видит купленные ID на этой сцене
            Debug.Log($"<color=yellow>[HexManager]</color> Загрузка завершена. Доступные ID: {string.Join(", ", globalHexStats.unlockedHexContentIDs)}");
        }
        
        GenerateHexContents();
    }

    private void GenerateHexContents()
    {
        HexBlocker[] allHexes = Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None);
        List<HexBlocker> hexesToAutoUnlock = new List<HexBlocker>();
        var groupedHexes = allHexes.GroupBy(h => h.groupID);

        foreach (var group in groupedHexes)
        {
            int currentID = group.Key;
            List<HexBlocker> hexesInGroup = group.ToList();
            HexGroupSettings settings = groups.Find(g => g.groupID == currentID);
            
            if (settings != null && settings.prefabsForGroup.Count > 0)
            {
                List<HexPrefabData> spawnPool = PrepareSpawnPool(settings.prefabsForGroup, hexesInGroup.Count);
                ShuffleList(hexesInGroup);

                for (int i = 0; i < hexesInGroup.Count; i++)
                {
                    if (i >= spawnPool.Count) break;

                    Instantiate(spawnPool[i].prefab, hexesInGroup[i].transform.position, Quaternion.identity);

                    if (spawnPool[i].autoUnlockHex)
                    {
                        hexesToAutoUnlock.Add(hexesInGroup[i]);
                    }
                }
            }
        }

        foreach (var hex in allHexes) hex.InitializeHexContent();
        foreach (var hex in hexesToAutoUnlock) hex.RemoveHex();
    }

    private List<HexPrefabData> PrepareSpawnPool(List<HexPrefabData> dataList, int hexCount)
    {
        List<HexPrefabData> pool = new List<HexPrefabData>();

        // 1. Фильтруем список: оставляем ТОЛЬКО те префабы, которые разрешены
        // (либо не заперты изначально, либо уже куплены в GlobalStats)
        var allowedData = dataList.Where(d => 
        {
            if (d.startLocked)
            {
                return globalHexStats != null && globalHexStats.unlockedHexContentIDs.Contains(d.contentID);
            }
            return true;
        }).ToList();

        // 2. Из разрешенных сначала берем все обязательные
        var mandatory = allowedData.Where(d => d.isMandatory).ToList();
        pool.AddRange(mandatory);

        // 3. Из разрешенных берем необязательные для заполнения оставшихся мест
        var optional = allowedData.Where(d => !d.isMandatory).ToList();
        ShuffleList(optional);

        // 4. Заполняем свободные слоты гексов
        int remainingSlots = hexCount - pool.Count;
        for (int i = 0; i < remainingSlots && i < optional.Count; i++)
        {
            pool.Add(optional[i]);
        }

        return pool;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}
