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
    private float _cycleDuration;
    private int _remainingRepeats;
    private bool _isActive = false;
    private bool _stoppedForEscape;

    // Логика: берем время из статов или из локальной переменной
    private float CurrentDuration => stats != null ? stats.TotalProductionTime : duration;

    void Start()
    {
        _currentTime = CurrentDuration;
        _cycleDuration = _currentTime;
        _remainingRepeats = repeatCount;
        
        if (runOnStart) StartTimer();
    }

    void Update()
    {
        if (!_isActive || _stoppedForEscape) return;

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
        if (_stoppedForEscape) return;
        duration = newDuration;
        _currentTime = duration;
        _cycleDuration = duration;
        _remainingRepeats = repeatCount;
        _isActive = true;
        
        if (progressBarObject != null) 
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    private void SendProgressToBar()
    {
        if (progressBarObject == null) return;
        
        float progress = 1f - Mathf.Clamp01(_currentTime / Mathf.Max(0.001f, _cycleDuration));
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
            _cycleDuration = _currentTime;
        }
        else
        {
            _remainingRepeats--;
            if (_remainingRepeats > 0)
            {
                _currentTime = CurrentDuration;
                _cycleDuration = _currentTime;
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
        if (_stoppedForEscape) return;
        _isActive = true;
        if (progressBarObject != null) 
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    public void ResetTimer()
    {
        if (_stoppedForEscape) return;
        _currentTime = CurrentDuration;
        _cycleDuration = _currentTime;
        _remainingRepeats = repeatCount;
        StartTimer();
    }

    public void StopForEscape()
    {
        _stoppedForEscape = true;
        _isActive = false;
        if (progressBarObject != null)
            progressBarObject.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
    }
}
