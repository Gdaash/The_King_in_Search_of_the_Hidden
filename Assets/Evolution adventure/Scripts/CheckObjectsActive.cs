using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Evolution_adventure.Scripts
{
public class CheckObjectsActive : MonoBehaviour
{
    [Header("Объекты для проверки")]
    [SerializeField] private List<GameObject> objectsToWatch;

    [Header("Событие, запускаемое при включении всех объектов")]
    public UnityEvent onAllActive;

    private bool _eventTriggered = false;

    private void Update()
    {
        // Если эвент уже сработал, проверку можно прекратить
        if (_eventTriggered) return;

        if (CheckIfAllActive())
        {
            TriggerEvent();
        }
    }

    private bool CheckIfAllActive()
    {
        if (objectsToWatch == null || objectsToWatch.Count == 0) return false;

        foreach (var obj in objectsToWatch)
        {
            // Проверка: объект не равен null и активен в иерархии
            if (obj == null || !obj.activeInHierarchy)
            {
                return false;
            }
        }
        return true;
    }

    private void TriggerEvent()
    {
        _eventTriggered = true;
        Debug.Log("Все объекты активны! Запуск эвента.");
        onAllActive?.Invoke();
    }
}
}