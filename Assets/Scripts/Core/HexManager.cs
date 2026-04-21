using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HexManager : MonoBehaviour
{
    [System.Serializable]
    public class HexPrefabData
    {
        public GameObject prefab;
        public bool isMandatory; // Обязательный префаб
        public bool autoUnlockHex; // НОВОЕ: Если стоит, гекс откроется сразу после спавна
    }

    [System.Serializable]
    public class HexGroupSettings
    {
        public int groupID;
        public List<HexPrefabData> prefabsForGroup;
    }

    [Header("Настройки групп префабов")]
    [SerializeField] private List<HexGroupSettings> groups;

    private void Start()
    {
        GenerateHexContents();
    }

    private void GenerateHexContents()
    {
        // 1. Находим ВООБЩЕ ВСЕ гексы на сцене
        HexBlocker[] allHexes = Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None);

        // Список для хранения гексов, которые нужно будет открыть в конце
        List<HexBlocker> hexesToAutoUnlock = new List<HexBlocker>();

        // 2. Группируем гексы по их ID для распределения спавна
        var groupedHexes = allHexes.GroupBy(h => h.groupID);

        foreach (var group in groupedHexes)
        {
            int currentID = group.Key;
            List<HexBlocker> hexesInGroup = group.ToList();
            
            HexGroupSettings settings = groups.Find(g => g.groupID == currentID);
            
            if (settings != null && settings.prefabsForGroup.Count > 0)
            {
                // Формируем пул (теперь храним HexPrefabData целиком, чтобы знать про галочку autoUnlock)
                List<HexPrefabData> spawnPool = PrepareSpawnPool(settings.prefabsForGroup, hexesInGroup.Count);
                
                ShuffleList(hexesInGroup);

                for (int i = 0; i < hexesInGroup.Count; i++)
                {
                    if (i >= spawnPool.Count) break;

                    // Спавним префаб
                    Instantiate(spawnPool[i].prefab, hexesInGroup[i].transform.position, Quaternion.identity);

                    // Если у этого префаба стоит галочка авто-разблокировки, запоминаем гекс
                    if (spawnPool[i].autoUnlockHex)
                    {
                        hexesToAutoUnlock.Add(hexesInGroup[i]);
                    }
                }
            }
        }

        // 3. Скрываем объекты под всеми гексами
        foreach (var hex in allHexes)
        {
            hex.InitializeHexContent();
        }

        // 4. НОВОЕ: Открываем гексы, под которыми заспавнились "особые" префабы
        // Делаем это в самом конце, чтобы не мешать инициализации соседей
        foreach (var hex in hexesToAutoUnlock)
        {
            hex.RemoveHex();
        }
    }

    // Изменил возвращаемый тип на List<HexPrefabData>, чтобы сохранить информацию о галочке
    private List<HexPrefabData> PrepareSpawnPool(List<HexPrefabData> dataList, int hexCount)
    {
        List<HexPrefabData> pool = new List<HexPrefabData>();

        // Сначала берем все обязательные
        var mandatory = dataList.Where(d => d.isMandatory).ToList();
        pool.AddRange(mandatory);

        // Получаем список необязательных
        var optional = dataList.Where(d => !d.isMandatory).ToList();
        ShuffleList(optional);

        // Заполняем оставшиеся слоты случайными необязательными без повторов
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
