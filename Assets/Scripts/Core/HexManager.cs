using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HexManager : MonoBehaviour
{
    [System.Serializable]
    public class HexPrefabData
    {
        public GameObject prefab;
        public bool isMandatory; // Обязательный префаб (будет заспавнен точно)
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
        // 1. Находим ВООБЩЕ ВСЕ гексы на сцене (даже если у них нет настроек в менеджере)
        HexBlocker[] allHexes = Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None);

        // 2. Группируем гексы по их ID для распределения спавна
        var groupedHexes = allHexes.GroupBy(h => h.groupID);

        foreach (var group in groupedHexes)
        {
            int currentID = group.Key;
            List<HexBlocker> hexesInGroup = group.ToList();
            
            // Ищем настройки префабов именно для этого ID
            HexGroupSettings settings = groups.Find(g => g.groupID == currentID);
            
            // Если для этой группы есть список префабов — выполняем спавн
            if (settings != null && settings.prefabsForGroup.Count > 0)
            {
                // Формируем пул (обязательные + случайные необязательные)
                List<GameObject> spawnPool = PrepareSpawnPool(settings.prefabsForGroup, hexesInGroup.Count);
                
                // Перемешиваем список гексов этой группы для случайного распределения
                ShuffleList(hexesInGroup);

                for (int i = 0; i < hexesInGroup.Count; i++)
                {
                    if (i >= spawnPool.Count) break;

                    // Создаем объект строго в координатах гекса
                    Instantiate(spawnPool[i], hexesInGroup[i].transform.position, Quaternion.identity);
                }
            }
        }

        // 3. ФИНАЛЬНЫЙ ШАГ: Просим КАЖДЫЙ гекс на сцене выполнить логику скрытия.
        // Это скроет и те объекты, что мы только что заспавнили, 
        // и те, что стояли на сцене изначально (ручная расстановка).
        foreach (var hex in allHexes)
        {
            hex.InitializeHexContent();
        }
    }

    private List<GameObject> PrepareSpawnPool(List<HexPrefabData> dataList, int hexCount)
    {
        List<GameObject> pool = new List<GameObject>();

        // Сначала берем все обязательные префабы
        var mandatory = dataList.Where(d => d.isMandatory).Select(d => d.prefab).ToList();
        pool.AddRange(mandatory);

        // Получаем список необязательных префабов
        var optional = dataList.Where(d => !d.isMandatory).Select(d => d.prefab).ToList();
        ShuffleList(optional);

        // Дополняем оставшиеся слоты случайными необязательными префабами без повторов
        int remainingSlots = hexCount - pool.Count;
        for (int i = 0; i < remainingSlots && i < optional.Count; i++)
        {
            pool.Add(optional[i]);
        }

        return pool;
    }

    // Утилита для перемешивания списка (Fisher-Yates shuffle)
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
