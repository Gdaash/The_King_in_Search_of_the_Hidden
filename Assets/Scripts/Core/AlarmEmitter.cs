using UnityEngine;

/// <summary>
/// Добавляет тревогу из UnityEvent. Компонент можно размещать на любом префабе.
/// </summary>
public class AlarmEmitter : MonoBehaviour
{
    [Tooltip("Значение, которое добавляет метод AddConfiguredAlarm.")]
    [SerializeField, Min(0f)] private float alarmAmount = 5f;
    public float ConfiguredAmount => Mathf.Max(0f, alarmAmount);

    public static float Preview(UnityEngine.Events.UnityEvent action)
    {
        if (action == null) return 0f;
        float amount = 0f;
        for (int i = 0; i < action.GetPersistentEventCount(); i++)
            if (ConfiguredEmitter(action, i) is AlarmEmitter emitter)
                amount += emitter.ConfiguredAmount;
        return amount;
    }

    public static AlarmEmitter ConfiguredEmitter(UnityEngine.Events.UnityEvent action, int index) =>
        action.GetPersistentListenerState(index) != UnityEngine.Events.UnityEventCallState.Off &&
        action.GetPersistentMethodName(index) == nameof(AddConfiguredAlarm) ?
        action.GetPersistentTarget(index) as AlarmEmitter : null;

    /// <summary>Метод без параметров для UnityEvent: добавляет значение из Inspector.</summary>
    public void AddConfiguredAlarm()
    {
        AddAlarm(ConfiguredAmount);
    }

    /// <summary>Метод для UnityEvent&lt;float&gt;: добавляет переданное значение.</summary>
    public void AddAlarm(float amount)
    {
        if (amount <= 0f)
        {
            AlarmSystem.Instance?.CancelActionAlarm(this);
            return;
        }

        AlarmSystem alarmSystem = AlarmSystem.Instance ?? UnityEngine.Object.FindFirstObjectByType<AlarmSystem>();
        if (alarmSystem == null)
        {
            Debug.LogWarning("[AlarmEmitter] AlarmSystem не найден в сцене.", this);
            return;
        }

        alarmSystem.AddAlarmFromAction(amount, transform.position, this);
    }
}
