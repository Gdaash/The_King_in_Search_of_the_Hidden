using UnityEngine;
using UnityEngine.UI;
using TMPro; // Для отображения процентов

public class CircularProgressBar : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private Image progressImage;    // Тот самый Image с типом Filled
    [SerializeField] private TextMeshProUGUI textPercent; // Текст для процентов (опционально)

    [Header("Настройки")]
    [Range(0, 1)] 
    [SerializeField] private float currentValue = 0f; // Значение от 0 до 1
    [SerializeField] private float speed = 5f;        // Скорость плавного заполнения

    private float _targetValue = 0f;

    void Update()
    {
        // Плавно двигаем Fill Amount к целевому значению
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.MoveTowards(progressImage.fillAmount, _targetValue, speed * Time.deltaTime);
            
            // Обновляем текст, если он есть
            if (textPercent != null)
            {
                textPercent.text = Mathf.RoundToInt(progressImage.fillAmount * 100f) + "%";
            }
        }
    }

    // Публичный метод для установки прогресса (вызывай его из других скриптов)
    public void SetProgress(float value)
    {
        // Ограничиваем значение между 0 и 1
        _targetValue = Mathf.Clamp01(value);
    }

    // Метод для моментальной установки без анимации
    public void SetProgressInstant(float value)
    {
        _targetValue = Mathf.Clamp01(value);
        progressImage.fillAmount = _targetValue;
    }
}