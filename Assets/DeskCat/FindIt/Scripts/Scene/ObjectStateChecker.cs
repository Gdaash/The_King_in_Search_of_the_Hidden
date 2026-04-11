using UnityEngine;
using UnityEngine.Events;

public class ObjectStateChecker : MonoBehaviour
{
    [Header("Какой объект проверяем?")]
    [SerializeField] private GameObject targetObject;

    [Header("События")]
    public UnityEvent OnObjectIsActive;   // Сработает, если включен
    public UnityEvent OnObjectIsDisabled; // Сработает, если выключен

    // Главный метод для проверки (можно вызвать через Unity Event из другого скрипта)
    public void CheckState()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Объект для проверки не назначен!");
            return;
        }

        // activeInHierarchy проверяет, включен ли объект И все его родители.
        // Если тебе нужно проверить только галочку на самом объекте, используй activeSelf.
        if (targetObject.activeInHierarchy)
        {
            OnObjectIsActive?.Invoke();
            Debug.Log($"{targetObject.name} сейчас включен.");
        }
        else
        {
            OnObjectIsDisabled?.Invoke();
            Debug.Log($"{targetObject.name} сейчас выключен.");
        }
    }

    // Дополнительный метод, возвращающий true/false для использования в коде
    public bool IsActive()
    {
        return targetObject != null && targetObject.activeInHierarchy;
    }
}
