using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class HexLightUnlocker : MonoBehaviour
{
    // Статический список всех активных гексов
    public static List<HexLightUnlocker> ActiveInstances { get; } = new List<HexLightUnlocker>();

    [Header("Ссылки на компоненты разблокировки")]
    [Tooltip("Объект с прогрессбаром (на нём должен висеть RadialProgressBar)")]
    [SerializeField] private RadialProgressBar progressBar;

    [Header("Настройки таймера")]
    [Tooltip("Радиус, при попадании в который маркер начинает разблокировку")]
    [SerializeField] private float detectionRadius = 1.5f;
    
    [Tooltip("Время разблокировки в секундах (умножается на уровень опасности)")]
    [SerializeField] private float baseDuration = 5f;

    [Header("События")]
    [Tooltip("Вызывается при завершении таймера разблокировки")]
    public UnityEvent OnUnlockCompleteEvent;

    // === ВСТРОЕННЫЙ ТАЙМЕР ===
    private float _currentTime;
    private float _duration;
    private bool _isActive = false;

    // === СОСТОЯНИЕ РАЗБЛОКИРОВКИ ===
    private bool _isUnlocking = false;
    private bool _isUnlocked = false;

    private void Awake()
    {
        if (progressBar != null)
        {
            progressBar.Hide();
        }
    }

    private void OnEnable()
    {
        if (!ActiveInstances.Contains(this))
        {
            ActiveInstances.Add(this);
        }
    }

    private void OnDisable()
    {
        if (_isActive)
        {
            CancelUnlockProcess();
        }
        ActiveInstances.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }

    private void Update()
    {
        if (_isActive)
        {
            _currentTime -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(_currentTime / _duration);
            
            if (progressBar != null)
            {
                progressBar.SetProgress(progress);
            }

            if (_currentTime <= 0f)
            {
                OnUnlockComplete();
            }
        }
    }

    public void StartUnlockProcess(float dangerLevel)
    {
        if (_isUnlocked || _isUnlocking || _isActive) return;
        
        _isUnlocking = true;
        _isActive = true;

        _duration = baseDuration * dangerLevel;
        if (_duration <= 0f) _duration = baseDuration;
        
        _currentTime = _duration;
        
        if (progressBar != null)
        {
            progressBar.Show();
        }
    }

    public void CancelUnlockProcess()
    {
        if (_isUnlocked) return;
        
        _isUnlocking = false;
        _isActive = false;
        _currentTime = 0f;
        
        if (progressBar != null)
        {
            progressBar.Hide();
        }
    }

    private void OnUnlockComplete()
    {
        _isUnlocked = true;
        _isUnlocking = false;
        _isActive = false;

        if (progressBar != null)
        {
            progressBar.SetProgress(1f);
        }

        OnUnlockCompleteEvent?.Invoke();

        HexBlocker hexBlocker = GetComponent<HexBlocker>();
        if (hexBlocker != null)
        {
            hexBlocker.RemoveHex();
        }
    }

    public bool IsPointOverHex(Vector2 point)
    {
        float distance = Vector2.Distance(point, (Vector2)transform.position);
        return distance <= detectionRadius;
    }

    public bool IsUnlocking() => _isUnlocking;
    public bool IsUnlocked() => _isUnlocked;
} // <-- Эту скобку я забыл в прошлый раз