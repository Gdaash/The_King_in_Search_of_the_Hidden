using UnityEngine;
using UnityEngine.Events;

public class TimerController : MonoBehaviour
{
    [Header("Настройки времени")]
    [SerializeField] private float duration = 5f; // Время таймера
    [SerializeField] private bool loopInfinitely = false; // Бесконечный повтор
    [SerializeField] private int repeatCount = 1; // Кол-во повторов (если не бесконечно)

    [Header("Событие")]
    public UnityEvent OnTimerEnd; // Сюда перетаскиваем действия в инспекторе

    private float _currentTime;
    private int _remainingRepeats;
    private bool _isActive = true;

    void Start()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
    }

    void Update()
    {
        if (!_isActive) return;

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
        }
        else
        {
            TimerFinished();
        }
    }

    private void TimerFinished()
    {
        OnTimerEnd?.Invoke(); // Запускаем событие

        if (loopInfinitely)
        {
            _currentTime = duration; // Сброс для бесконечного цикла
        }
        else
        {
            _remainingRepeats--;
            if (_remainingRepeats > 0)
            {
                _currentTime = duration; // Сброс для следующего повтора
            }
            else
            {
                _isActive = false; // Выключаем, когда повторы кончились
            }
        }
    }

    // Публичный метод, если захочешь перезапустить таймер кодом
    public void ResetTimer()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        _isActive = true;
    }
}