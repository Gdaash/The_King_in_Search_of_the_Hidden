using UnityEngine;
using UnityEngine.UI;

public class RadialProgressBar : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Основной Image, который заполняется (Fill)")]
    [SerializeField] private Image progressImage;
    
    [Tooltip("Image подложки (Background), показывается всегда, когда прогрессбар виден")]
    [SerializeField] private Image backgroundImage;
    
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Настройки плавности")]
    [SerializeField] private float fillSpeed = 15f; 
    [SerializeField] private float fadeSpeed = 5f;

    private float _targetValue = 0f;
    private float _targetAlpha = 0f; 

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (progressImage == null) progressImage = GetComponentInChildren<Image>();
        
        // Инициализация: скрываем прогрессбар
        Hide();
    }

    void Update()
    {
        // Плавное заполнение основного прогресса
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.MoveTowards(progressImage.fillAmount, _targetValue, fillSpeed * Time.deltaTime);
        }

        // Плавное появление/скрытие всего блока
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            
            if (canvasGroup.alpha > 0.01f && !canvasGroup.gameObject.activeSelf)
                canvasGroup.gameObject.SetActive(true);
        }

        // Подложка появляется и исчезает синхронно с альфой CanvasGroup
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = Mathf.MoveTowards(bgColor.a, _targetAlpha, fadeSpeed * Time.deltaTime);
            backgroundImage.color = bgColor;
        }
    }

    public void SetProgress(float value)
    {
        _targetValue = Mathf.Clamp01(value);
        
        // Логика: если процесс идет — показываем, если закончился — скрываем
        if (value > 0.001f && value < 0.999f) 
            Show();
        else if (value >= 0.999f || value <= 0.001f) 
            Hide();
    }

    public void Show()
    {
        _targetAlpha = 1f;
        
        // СРАЗУ применяем значения, чтобы при включении объекта прогрессбар был виден
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            if (!canvasGroup.gameObject.activeSelf)
                canvasGroup.gameObject.SetActive(true);
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 1f;
            backgroundImage.color = bgColor;
        }
    }

    public void Hide()
    {
        _targetAlpha = 0f;
        
        // СРАЗУ применяем значения
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 0f;
            backgroundImage.color = bgColor;
        }
    }
}