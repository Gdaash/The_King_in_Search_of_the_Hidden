using UnityEngine;

public class ObjectStateSwitcher : MonoBehaviour
{
    [Header("Настройки GlobalStats")]
    [SerializeField] private GlobalStats stats;
    [SerializeField] private string stateID; // Уникальный ID для этого улучшения (напр. "Mill_Fixed")

    [Header("Объекты")]
    [SerializeField] private GameObject defaultObject;  // Сломанная мельница
    [SerializeField] private GameObject upgradedObject; // Починенная мельница

    void Start()
    {
        ApplyState();
    }

    private void OnEnable()
    {
        if (stats != null) stats.OnStatsUpdated += ApplyState;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnStatsUpdated -= ApplyState;
    }

    public void ApplyState()
    {
        if (stats == null || defaultObject == null || upgradedObject == null) return;

        // Проверяем, разблокировано ли улучшенное состояние
        bool isUpgraded = stats.upgradedVisualStates.Contains(stateID);

        defaultObject.SetActive(!isUpgraded);
        upgradedObject.SetActive(isUpgraded);
    }
}
