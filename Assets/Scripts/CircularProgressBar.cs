using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Нужно для работы с мышкой

public class CircularProgressBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private Image progressImage;
    [SerializeField] private CanvasGroup canvasGroup; // Добавь этот компонент на объект

    [Header("Настройки")]
    [Range(0, 1)] 
    [SerializeField] private float currentValue = 0f;
    [SerializeField] private float fillSpeed = 5f;
    [SerializeField] private float fadeSpeed = 5f; // Скорость появления/исчезновения

    private float _targetValue = 0f;
    private float _targetAlpha = 0f; // Целевая прозрачность

    void Start()
    {
        // В начале делаем прогрессбар невидимым
        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    void Update()
    {
        // 1. Плавное заполнение шкалы
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.MoveTowards(progressImage.fillAmount, _targetValue, fillSpeed * Time.deltaTime);
        }

        // 2. Плавное изменение прозрачности (Fade In / Fade Out)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    // Срабатывает, когда мышка входит в зону объекта
    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetAlpha = 1f;
    }

    // Срабатывает, когда мышка покидает зону объекта
    public void OnPointerExit(PointerEventData eventData)
    {
        _targetAlpha = 0f;
    }

    public void SetProgress(float value)
    {
        _targetValue = Mathf.Clamp01(value);
    }

    public void SetProgressInstant(float value)
    {
        _targetValue = Mathf.Clamp01(value);
        progressImage.fillAmount = _targetValue;
    }
}