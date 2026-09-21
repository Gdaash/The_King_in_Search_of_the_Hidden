using UnityEngine;

/// <summary>
/// Добавляет тревогу из UnityEvent. Компонент можно размещать на любом префабе.
/// </summary>
public class AlarmEmitter : MonoBehaviour
{
    [Tooltip("Значение, которое добавляет метод AddConfiguredAlarm.")]
    [SerializeField, Min(0f)] private float alarmAmount = 5f;

    /// <summary>Метод без параметров для UnityEvent: добавляет значение из Inspector.</summary>
    public void AddConfiguredAlarm()
    {
        AddAlarm(alarmAmount);
    }

    /// <summary>Метод для UnityEvent&lt;float&gt;: добавляет переданное значение.</summary>
    public void AddAlarm(float amount)
    {
        if (amount <= 0f) return;

        AlarmSystem alarmSystem = AlarmSystem.Instance ?? UnityEngine.Object.FindFirstObjectByType<AlarmSystem>();
        if (alarmSystem == null)
        {
            Debug.LogWarning("[AlarmEmitter] AlarmSystem не найден в сцене.", this);
            return;
        }

        alarmSystem.AddAlarmFromWorldPosition(amount, transform.position);
    }
}
