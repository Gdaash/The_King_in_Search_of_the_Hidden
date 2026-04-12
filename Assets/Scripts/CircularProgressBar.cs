using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CircularProgressBar : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private Image progressImage;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Сенсор наведения")]
    [Tooltip("Объект (спрайт/UI), на который нужно наводить мышку")]
    [SerializeField] private GameObject hoverSensor; 

    [Header("Настройки")]
    [SerializeField] private float fillSpeed = 10f;
    [SerializeField] private float fadeSpeed = 5f;

    private float _targetValue = 0f;
    private float _targetAlpha = 0f;

    void Awake()
    {
        // Настраиваем триггеры один раз при инициализации объекта
        SetupSensor();
    }

    void OnEnable()
    {
        // Когда объект включается, проверяем, не стоит ли над ним мышка прямо сейчас
        if (IsMouseOverSensor())
        {
            _targetAlpha = 1f;
            // Если нужно, чтобы он появился мгновенно при включении под мышкой:
            // if (canvasGroup != null) canvasGroup.alpha = 1f; 
        }
        else
        {
            _targetAlpha = 0f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }

    private bool IsMouseOverSensor()
    {
        if (hoverSensor == null || EventSystem.current == null) return false;

        // Создаем данные указателя для текущей позиции мыши
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        // Стреляем лучом во все объекты UI под мышкой
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Проверяем, есть ли среди них наш сенсор
        foreach (RaycastResult result in results)
        {
            if (result.gameObject == hoverSensor) return true;
        }

        return false;
    }

    void SetupSensor()
    {
        if (hoverSensor == null) return;

        EventTrigger trigger = hoverSensor.GetComponent<EventTrigger>();
        if (trigger == null) trigger = hoverSensor.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { _targetAlpha = 1f; });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { _targetAlpha = 0f; });
        trigger.triggers.Add(entryExit);
    }

    void Update()
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.MoveTowards(progressImage.fillAmount, _targetValue, fillSpeed * Time.deltaTime);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    public void SetProgress(float value)
    {
        _targetValue = Mathf.Clamp01(value);
    }

    public void SetProgressInstant(float value)
    {
        _targetValue = Mathf.Clamp01(value);
        if (progressImage != null) progressImage.fillAmount = _targetValue;
    }
}