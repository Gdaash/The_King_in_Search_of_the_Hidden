using UnityEngine;
using TMPro;
using System.Collections;

public class ResourceUI : MonoBehaviour
{
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
    [SerializeField] private Color addColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

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
        Color targetColor = (newValue > _lastValue) ? addColor : _originalColor;
        
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

    private IEnumerator BumpRoutine(Color targetColor)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI); 

            resourceText.transform.localScale = _originalScale * Mathf.Lerp(1f, bumpScale, curve);
            resourceText.color = Color.Lerp(_originalColor, targetColor, curve);
            yield return null;
        }
        resourceText.transform.localScale = _originalScale;
        resourceText.color = _originalColor;
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
            return resourceType.resourceName + " "; 
        }
        return "";
    }
}