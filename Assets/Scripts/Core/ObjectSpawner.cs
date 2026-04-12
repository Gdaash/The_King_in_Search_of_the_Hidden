using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject prefabToSpawn; // Что создаем (префаб)
    [SerializeField] private Transform spawnPoint;     // Где создаем (точка в пространстве)

    [Header("Дополнительно")]
    [SerializeField] private bool parentToSpawner = false; // Делать ли объект дочерним?

    // Основной метод для создания объекта
    public void SpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Prefab для спавна не назначен в инспекторе!");
            return;
        }

        // Определяем позицию и поворот (если точка не указана, берем позицию самого спавнера)
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        // Создаем объект
        GameObject newObject = Instantiate(prefabToSpawn, position, rotation);

        // Если нужно прикрепить объект к спавнеру
        if (parentToSpawner)
        {
            newObject.transform.SetParent(this.transform);
        }
    }
}