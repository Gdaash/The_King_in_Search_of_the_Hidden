using UnityEngine;
using UnityEngine.UI;

public class AutoProgressBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Image progressImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Настройки")]
    [SerializeField] private float fillSpeed = 15f; 
    [SerializeField] private float fadeSpeed = 5f;

    private float _targetValue = 0f;
    private float _targetAlpha = 0f; 

    void Awake()
    {
        // Если забыл назначить в инспекторе, попробуем найти сами
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (progressImage == null) progressImage = GetComponentInChildren<Image>();
    }

    void Update()
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.MoveTowards(progressImage.fillAmount, _targetValue, fillSpeed * Time.deltaTime);
        }

        if (canvasGroup != null)
        {
            // Плавно меняем альфу
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            
            // ДИАГНОСТИКА: Если альфа больше 0, но объекта не видно, принудительно включаем объект
            if (canvasGroup.alpha > 0.01f && !canvasGroup.gameObject.activeSelf)
                canvasGroup.gameObject.SetActive(true);
        }
    }

    public void SetProgress(float value)
    {
        _targetValue = Mathf.Clamp01(value);
        
        // Логика: если процесс идет — показываем, если закончился — скрываем
        if (value > 0.001f && value < 0.999f) 
            _targetAlpha = 1f;
        else if (value >= 0.999f || value <= 0.001f) 
            _targetAlpha = 0f;
    }

    public void Show() => _targetAlpha = 1f;
    public void Hide() => _targetAlpha = 0f;
}