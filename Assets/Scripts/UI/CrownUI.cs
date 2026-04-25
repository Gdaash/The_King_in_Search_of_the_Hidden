using UnityEngine;
using TMPro;
using System.Collections;

public class CrownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI crownsText;
    [SerializeField] private string prefix = "👑 ";
    
    [Header("Настройки анимации")]
    [SerializeField] private float bumpScale = 1.2f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Color addColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

    private Vector3 _originalScale;
    private Color _originalColor;
    private Coroutine _activeRoutine;
    private int _lastValue = -1; // Храним предыдущее значение здесь

    private void Awake()
    {
        _originalScale = crownsText.transform.localScale;
        _originalColor = crownsText.color;
    }

    private void OnEnable()
    {
        CrownManager.OnCrownsChanged += HandleValueChange;
        SkillButton.OnPurchaseFailed += TriggerError; 
        
        if (CrownManager.Instance != null) 
        {
            _lastValue = CrownManager.Instance.CurrentCrowns;
            UpdateText(_lastValue);
        }
    }

    private void OnDisable()
    {
        CrownManager.OnCrownsChanged -= HandleValueChange;
        SkillButton.OnPurchaseFailed -= TriggerError;
    }

    private void HandleValueChange(int newValue)
    {
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
        if (!gameObject.activeInHierarchy) return;
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

            crownsText.transform.localScale = _originalScale * Mathf.Lerp(1f, bumpScale, curve);
            crownsText.color = Color.Lerp(_originalColor, targetColor, curve);
            yield return null;
        }
        crownsText.transform.localScale = _originalScale;
        crownsText.color = _originalColor;
    }

    private void UpdateText(int val) 
    {
        if (crownsText != null)
            crownsText.text = prefix + val.ToString();
    }
}
