using UnityEngine;
using TMPro;
using System.Collections;

public partial class ResourceUI : MonoBehaviour
{
    [SerializeField] private bool showAllGlobalResources;
    public bool DisplaysGlobalResources => showAllGlobalResources;
    [Header("Настройки ресурса")]
    [Tooltip("Тип ресурса, который отображает этот конкретный элемент UI")]
    [SerializeField] private ResourceType resourceType;

    [Header("Ссылки на UI")]
    [SerializeField] private TextMeshProUGUI resourceText;
    
    [Tooltip("Оставьте пустым, чтобы использовать имя ресурса, или задайте свой символ (например, '👑 ' или '🌲 ')")]
    [SerializeField] private string customPrefix = "";

    [Header("Настройки анимации")]
    [SerializeField] private float bumpScale = 1.2f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField, Min(0f)] private float highlightDuration = 1f;
    [SerializeField] private Color addColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;
    [Tooltip("Общий источник цветов увеличения и уменьшения ресурсов.")]
    [SerializeField] private GameFoundation.UI.NotificationFeed notificationColors;
    private Color IncreaseColor => notificationColors != null ? notificationColors.PositiveColor : addColor;
    private Color DecreaseColor => notificationColors != null ? notificationColors.NegativeColor : errorColor;

    private Vector3 _originalScale;
    private Color _originalColor;
    private Coroutine _activeRoutine;
    private int _lastValue = -1; 

    private void Awake()
    {
        if (resourceText != null)
        {
            _originalScale = resourceText.transform.localScale;
            _originalColor = resourceText.color;
        }
    }

    private void OnEnable()
    {
        if (showAllGlobalResources)
        {
            EnableGlobalResources();
            return;
        }

        if (resourceType != null)
        {
            GlobalResourceManager.OnResourceChanged += HandleValueChange;
            
            // Инициализация начального значения при включении
            if (GlobalResourceManager.Instance != null) 
            {
                _lastValue = GlobalResourceManager.Instance.GetResourceAmount(resourceType);
                UpdateText(_lastValue);
            }
        }
    }

    private void OnDisable()
    {
        if (showAllGlobalResources)
        {
            DisableGlobalResources();
            return;
        }

        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = null;
        if (resourceText != null) { resourceText.transform.localScale = _originalScale; resourceText.color = _originalColor; }
        if (resourceType != null)
        {
            GlobalResourceManager.OnResourceChanged -= HandleValueChange;
        }
    }

    private void HandleValueChange(ResourceType changedType, int newValue)
    {
        // Реагируем ТОЛЬКО на изменения нашего типа ресурса
        if (changedType != resourceType) return;

        // Если это самая первая загрузка, просто обновляем текст без анимации
        if (_lastValue == -1)
        {
            _lastValue = newValue;
            UpdateText(newValue);
            return;
        }

        // Определяем цвет: зеленый если добавили, стандартный если потратили
        if (newValue == _lastValue) return;
        Color targetColor = (newValue > _lastValue) ? IncreaseColor : DecreaseColor;
        
        _lastValue = newValue;
        UpdateText(newValue);
        TriggerEffect(targetColor);
    }

    public void TriggerError() => TriggerEffect(errorColor);

    private void TriggerEffect(Color color)
    {
        if (!gameObject.activeInHierarchy || resourceText == null) return;
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(BumpRoutine(color));
    }

    private IEnumerator BumpRoutine(Color targetColor) => AnimateCount(resourceText, _originalScale, _originalColor, targetColor);

    private IEnumerator AnimateCount(TextMeshProUGUI text, Vector3 baseScale, Color baseColor, Color targetColor)
    {
        float elapsed = 0f;
        float total = Mathf.Max(duration, highlightDuration);
        while (text != null && elapsed < total)
        {
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            text.transform.localScale = baseScale * (1f + (bumpScale - 1f) * Mathf.Sin(t * Mathf.PI));
            text.color = elapsed < highlightDuration ? targetColor : baseColor;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        if (text != null) { text.transform.localScale = baseScale; text.color = baseColor; }
    }

    private void UpdateText(int val) 
    {
        if (resourceText == null) return;

        string prefixToUse = string.IsNullOrEmpty(customPrefix) ? GetAutoPrefix() : customPrefix;
        resourceText.text = prefixToUse + val.ToString();
    }

    private string GetAutoPrefix()
    {
        if (resourceType != null)
        {
            // Если префикс не задан вручную, используем имя ресурса из ScriptableObject
            return resourceType.DisplayName + " "; 
        }
        return "";
    }
}
