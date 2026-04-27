using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class HealthBar : MonoBehaviour
{
    [Header("Ссылки на полоски")]
    [SerializeField] private Image mainBar;
    [SerializeField] private Image bufferBar;
    
    [Header("Настройки")]
    [SerializeField] private float smoothSpeed = 5f; 
    [SerializeField] private float fadeSpeed = 3f;   

    private CanvasGroup _canvasGroup;
    private float _targetProgress = 1f;
    private Coroutine _fadeRoutine;
    private Transform _mainCameraTransform;
    private Vector3 _initialLocalScale; // Запоминаем начальный размер

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _initialLocalScale = transform.localScale; // Запоминаем (напр. 0.01, 0.01, 0.01)
        
        if (Camera.main != null) 
            _mainCameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        _canvasGroup.alpha = 0f;
        if (mainBar) mainBar.fillAmount = 1f;
        if (bufferBar) bufferBar.fillAmount = 1f;
        _targetProgress = 1f;
    }

    private void LateUpdate()
    {
        // 1. BILLBOARD: Поворот к камере
        if (_mainCameraTransform != null)
        {
            transform.rotation = _mainCameraTransform.rotation;
        }

        // 2. FIX SCALE: Компенсация разворота родителя (Flip)
        // Мы берем абсолютное значение скейла родителя, чтобы полоска всегда была направлена "вперед"
        Vector3 parentScale = transform.parent.localScale;
        
        transform.localScale = new Vector3(
            _initialLocalScale.x / Mathf.Sign(parentScale.x), 
            _initialLocalScale.y / Mathf.Sign(parentScale.y), 
            _initialLocalScale.z
        );
    }

    public void SetProgress(float progress)
    {
        _targetProgress = Mathf.Clamp01(progress);
        if (mainBar != null) mainBar.fillAmount = _targetProgress;
        if (_targetProgress < 0.99f) ToggleVisibility(true);
    }

    private void Update()
    {
        if (bufferBar == null) return;

        if (!Mathf.Approximately(bufferBar.fillAmount, _targetProgress))
        {
            bufferBar.fillAmount = Mathf.Lerp(bufferBar.fillAmount, _targetProgress, Time.deltaTime * smoothSpeed);
        }

        if (_targetProgress >= 0.99f && bufferBar.fillAmount >= 0.99f)
        {
            if (_canvasGroup.alpha > 0) ToggleVisibility(false);
        }
    }

    private void ToggleVisibility(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        if (Mathf.Approximately(_canvasGroup.alpha, targetAlpha)) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(targetAlpha));
    }

    private IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }
}
