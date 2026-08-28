using UnityEngine;
using System.Collections.Generic;

public class HexMapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class PrefabWeight
    {
        public GameObject prefab;
        [Min(0)] public int count = 1;
    }

    [System.Serializable]
    public class RingConfig
    {
        public List<PrefabWeight> prefabs = new List<PrefabWeight>();
    }

    [Header("Настройки карты")]
    [Tooltip("Префаб центрального шестиугольника. Оставьте пустым, если центр не нужно генерировать.")]
    public GameObject centerPrefab;

    [Tooltip("Каждый элемент списка = одно кольцо (1-е, 2-е, 3-е...)")]
    public List<RingConfig> rings = new List<RingConfig>();

    [Header("Параметры 2D сетки")]
    [Tooltip("Координаты центра. Обычно (0, 0, 0) для 2D.")]
    public Vector3 centerPosition = Vector3.zero;

    [Tooltip("Множитель размера сетки. 1 = использовать ваши базовые координаты (3, 2.5, 1.5).")]
    public float hexScale = 1f;

    private static readonly Vector3[] RingDirections =
    {
        new Vector3( 0f,    3f, 0f),
        new Vector3( 2.5f,  1.5f, 0f),
        new Vector3( 2.5f, -1.5f, 0f),
        new Vector3( 0f,   -3f, 0f),
        new Vector3(-2.5f, -1.5f, 0f),
        new Vector3(-2.5f,  1.5f, 0f)
    };

    [ContextMenu("🔨 Сгенерировать карту")]
    public void GenerateMap()
    {
        ClearMap(); // Сначала очищаем старое

        if (centerPrefab != null)
        {
            CreateHex(centerPrefab, centerPosition, "Center_Hex");
        }

        for (int r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            if (ring == null || ring.prefabs == null || ring.prefabs.Count == 0) continue;

            int ringNumber = r + 1;
            List<Vector3> positions = GetRingPositions(ringNumber);
            List<GameObject> pool = BuildPrefabPool(ring);
            Shuffle(pool);

            int expectedCount = 6 * ringNumber;
            if (pool.Count != expectedCount)
            {
                Debug.LogWarning($"Кольцо {ringNumber}: Ожидается {expectedCount} префабов, а настроено {pool.Count}.");
            }

            int count = Mathf.Min(positions.Count, pool.Count);
            for (int i = 0; i < count; i++)
            {
                CreateHex(pool[i], positions[i], $"Ring{ringNumber}_Hex{i}");
            }
        }

        Debug.Log($"✅ Генерация завершена! Создано колец: {rings.Count}");
    }

    /// <summary>
    /// Метод для очистки карты (вызывается кнопкой или перед генерацией)
    /// </summary>
    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
                DestroyImmediate(transform.GetChild(i).gameObject);
            #else
                Destroy(transform.GetChild(i).gameObject);
            #endif
        }
        Debug.Log("🗑️ Карта очищена!");
    }

    private List<Vector3> GetRingPositions(int ringNumber)
    {
        var positions = new List<Vector3>(6 * ringNumber);

        for (int side = 0; side < 6; side++)
        {
            Vector3 cornerA = RingDirections[side] * ringNumber * hexScale;
            Vector3 cornerB = RingDirections[(side + 1) % 6] * ringNumber * hexScale;

            for (int step = 0; step < ringNumber; step++)
            {
                float t = (float)step / ringNumber;
                Vector3 pos = centerPosition + Vector3.Lerp(cornerA, cornerB, t);
                pos.z = 0f; 
                positions.Add(pos);
            }
        }
        return positions;
    }

    private List<GameObject> BuildPrefabPool(RingConfig ring)
    {
        var pool = new List<GameObject>();
        foreach (var pw in ring.prefabs)
        {
            if (pw.prefab == null) continue;
            for (int i = 0; i < pw.count; i++) pool.Add(pw.prefab);
        }
        return pool;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void CreateHex(GameObject prefab, Vector3 position, string objectName)
    {
        GameObject go;
#if UNITY_EDITOR
        go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
#else
        go = Instantiate(prefab, transform);
#endif
        go.name = objectName;
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        if (centerPrefab != null) Gizmos.DrawWireSphere(centerPosition, 0.2f);

        for (int r = 0; r < rings.Count; r++)
        {
            var positions = GetRingPositions(r + 1);
            foreach (var pos in positions)
            {
                Gizmos.DrawWireSphere(pos, 0.15f);
            }
        }
    }
    #endif
}