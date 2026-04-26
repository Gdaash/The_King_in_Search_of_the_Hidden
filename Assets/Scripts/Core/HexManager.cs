using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HexManager : MonoBehaviour
{
    [System.Serializable]
    public class HexPrefabData
    {
        public string contentID; 
        public GameObject prefab;
        public bool isMandatory; 
        public bool autoUnlockHex; 
        public bool startLocked; 
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

    private void Awake()
    {
        // Переносим загрузку в Awake, чтобы данные были готовы ПЕРЕД Start всех остальных скриптов
        if (globalHexStats != null) 
        {
            globalHexStats.LoadStats();
        }
    }

    private void Start()
    {
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

                    // Спавним объект
                    GameObject instance = Instantiate(spawnPool[i].prefab, hexesInGroup[i].transform.position, Quaternion.identity);
                    
                    // ГАРАНТИЯ ВИДИМОСТИ: принудительно проверяем, чтобы альфа была 1 при спавне
                    // (на случай, если в префабе случайно сохранили 0)
                    var renderers = instance.GetComponentsInChildren<SpriteRenderer>();
                    foreach(var r in renderers) {
                        Color c = r.color;
                        c.a = 1f;
                        r.color = c;
                    }

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

        var allowedData = dataList.Where(d => 
        {
            if (d.startLocked)
            {
                return globalHexStats != null && globalHexStats.unlockedHexContentIDs.Contains(d.contentID);
            }
            return true;
        }).ToList();

        var mandatory = allowedData.Where(d => d.isMandatory).ToList();
        pool.AddRange(mandatory);

        var optional = allowedData.Where(d => !d.isMandatory).ToList();
        ShuffleList(optional);

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
