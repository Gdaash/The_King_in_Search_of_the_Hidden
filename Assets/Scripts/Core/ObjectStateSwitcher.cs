using UnityEngine;

public class ObjectStateSwitcher : MonoBehaviour
{
    [Header("Настройки GlobalStats")]
    [SerializeField] private GlobalStats stats;
    [SerializeField] private string stateID; // Уникальный ID (напр. "Mill_Fixed")

    [Header("Объекты")]
    [SerializeField] private GameObject defaultObject;  // Сломанное состояние
    [SerializeField] private GameObject upgradedObject; // Улучшенное состояние

    private bool _isHiddenByHex = false;

    void Awake()
    {
        // Если при старте сам объект выключен, значит он под гексом
        _isHiddenByHex = !gameObject.activeSelf;
    }

    void Start()
    {
        ApplyState();
    }

    private void OnEnable()
    {
        // Когда HexBlocker вызывает SetActive(true), срабатывает этот метод
        _isHiddenByHex = false; 
        
        if (stats != null) stats.OnStatsUpdated += ApplyState;
        
        // Сразу применяем верное состояние (целое или сломанное)
        ApplyState();
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnStatsUpdated -= ApplyState;
    }

    public void ApplyState()
    {
        // Проверка: если объект скрыт гексом, выходим, чтобы не конфликтовать
        if (_isHiddenByHex) return;

        if (stats == null || defaultObject == null || upgradedObject == null) return;

        // Проверяем в списке GlobalStats, разблокировано ли улучшение
        bool isUpgraded = stats.upgradedVisualStates.Contains(stateID);

        // Включаем только нужный вариант
        defaultObject.SetActive(!isUpgraded);
        upgradedObject.SetActive(isUpgraded);
    }

    // Метод для принудительной блокировки из других скриптов
    public void SetHiddenByHex(bool hidden)
    {
        _isHiddenByHex = hidden;
        if (!hidden) ApplyState();
    }
}
