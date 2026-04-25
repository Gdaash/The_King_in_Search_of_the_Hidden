using UnityEngine;
using UnityEngine.Events;

public class TimerController : MonoBehaviour
{
    [Header("Глобальные настройки (Опционально)")]
    [SerializeField] private GlobalStats stats; 

    [Header("Ссылки")]
    [SerializeField] private GameObject progressBarObject; // Вернул эту переменную

    [Header("Настройки времени (Если нет GlobalStats)")]
    [SerializeField] private float duration = 5f; 
    [SerializeField] private bool loopInfinitely = false; 
    [SerializeField] private int repeatCount = 1; 
    [SerializeField] private bool runOnStart = true;

    [Header("Событие")]
    public UnityEvent OnTimerEnd; 

    private float _currentTime;
    private int _remainingRepeats;
    private bool _isActive = false;

    // Логика: берем время из статов или из локальной переменной
    private float CurrentDuration => stats != null ? stats.TotalProductionTime : duration;

    void Start()
    {
        _currentTime = CurrentDuration;
        _remainingRepeats = repeatCount;
        
        if (runOnStart) StartTimer();
    }

    private void OnEnable()
    {
        if (stats != null) stats.OnStatsUpdated += SyncWithGlobalStats;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnStatsUpdated -= SyncWithGlobalStats;
    }

    private void SyncWithGlobalStats()
    {
        // При обновлении глобальных данных таймер подхватит новое время в следующем цикле
    }

    void Update()
    {
        if (!_isActive) return;

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            SendProgressToBar();
        }
        else
        {
            TimerFinished();
        }
    }

    public void SetDurationAndStart(float newDuration)
    {
        duration = newDuration;
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        _isActive = true;
        
        if (progressBarObject != null) 
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    private void SendProgressToBar()
    {
        if (progressBarObject == null) return;
        
        float progress = 1f - (Mathf.Clamp01(_currentTime / CurrentDuration));
        progressBarObject.SendMessage("SetProgress", progress, SendMessageOptions.DontRequireReceiver);
        
        if (progress > 0.001f && progress < 0.999f)
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    private void TimerFinished()
    {
        OnTimerEnd?.Invoke();

        if (loopInfinitely)
        {
            _currentTime = CurrentDuration;
        }
        else
        {
            _remainingRepeats--;
            if (_remainingRepeats > 0)
            {
                _currentTime = CurrentDuration;
            }
            else
            {
                _isActive = false;
                if (progressBarObject != null)
                {
                    progressBarObject.SendMessage("SetProgress", 1f, SendMessageOptions.DontRequireReceiver);
                    progressBarObject.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    public void StartTimer() 
    {
        _isActive = true;
        if (progressBarObject != null) 
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    public void ResetTimer()
    {
        _currentTime = CurrentDuration;
        _remainingRepeats = repeatCount;
        StartTimer();
    }
}
