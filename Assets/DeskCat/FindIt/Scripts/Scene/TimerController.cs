using UnityEngine;
using UnityEngine.Events;

public class TimerController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private CircularProgressBar progressBar; 

    [Header("Настройки времени")]
    [SerializeField] private float duration = 5f; 
    [SerializeField] private bool loopInfinitely = false; 
    [SerializeField] private int repeatCount = 1; 

    [Header("Событие")]
    public UnityEvent OnTimerEnd; 

    private float _currentTime;
    private int _remainingRepeats;
    private bool _isActive = true;

    void Start()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        UpdateProgressBar();
    }

    void Update()
    {
        if (!_isActive) return;

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            UpdateProgressBar();
        }
        else
        {
            TimerFinished();
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            // Формула для заполнения (от 0 до 1)
            float progress = 1f - (Mathf.Clamp01(_currentTime / duration));
            progressBar.SetProgress(progress);
        }
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
                if (progressBar != null) progressBar.SetProgress(1f); // Фиксируем заполнение
            }
        }
    }

    public void ResetTimer()
    {
        _currentTime = duration;
        _remainingRepeats = repeatCount;
        _isActive = true;
    }
}