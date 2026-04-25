using UnityEngine;
using UnityEngine.Events;

public class TimerController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject progressBarObject; 

    [Header("Настройки времени")]
    [SerializeField] private float duration = 5f; 
    [SerializeField] private bool loopInfinitely = false; 
    [SerializeField] private int repeatCount = 1; 
    [SerializeField] private bool runOnStart = true;

    [Header("Событие")]
    public UnityEvent OnTimerEnd; 

    private float _currentTime;
    private int _remainingRepeats;
    private bool _isActive = false;

    void Start()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        
        if (runOnStart) StartTimer();
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

    // Метод настройки извне
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
        float progress = 1f - (Mathf.Clamp01(_currentTime / duration));
        progressBarObject.SendMessage("SetProgress", progress, SendMessageOptions.DontRequireReceiver);
        
        if (progress > 0.001f && progress < 0.999f)
            progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    private void TimerFinished()
    {
        OnTimerEnd?.Invoke();

        if (loopInfinitely)
        {
            _currentTime = duration;
        }
        else
        {
            _remainingRepeats--;
            if (_remainingRepeats > 0)
            {
                _currentTime = duration;
            }
            else
            {
                _isActive = false;
                progressBarObject.SendMessage("SetProgress", 1f, SendMessageOptions.DontRequireReceiver);
                progressBarObject.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void StartTimer() 
    {
        _isActive = true;
        if (progressBarObject != null) progressBarObject.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
    }

    public void ResetTimer()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        StartTimer();
    }
}
