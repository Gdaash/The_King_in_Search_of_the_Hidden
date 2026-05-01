using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private Text tooltipText; // Ссылка на текст внутри префаба
    [SerializeField] private float fadeSpeed = 10f; // Скорость появления
    [SerializeField] private float distanceToTarget = 20f; // Отступ от элемента

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _fadeRoutine;
    private RectTransform _currentTarget;

    private void Awake()
    {
        // Синглтон для быстрого доступа
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();

        // Начальное состояние: невидим и не мешает кликам
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        // Если тултип активен, обновляем его положение вслед за целью
        if (_canvasGroup.alpha > 0 && _currentTarget != null)
        {
            UpdatePosition();
        }
    }

    public void Show(string description, RectTransform target)
    {
        _currentTarget = target;
        if (tooltipText != null) tooltipText.text = description;

        // Обновляем верстку, чтобы RectTransform тултипа пересчитал размер под новый текст
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        UpdatePosition();
        Fade(1f);
    }

    public void Hide()
    {
        _currentTarget = null;
        Fade(0f);
    }

    private void UpdatePosition()
    {
        if (_currentTarget == null) return;

        // Получаем мировые углы целевого объекта (0 - bottom-left, 1 - top-left, 2 - top-right, 3 - bottom-right)
        Vector3[] corners = new Vector3[4];
        _currentTarget.GetWorldCorners(corners);

        // Центр целевого объекта в экранных координатах
        Vector2 targetCenter = RectTransformUtility.WorldToScreenPoint(null, _currentTarget.position);

        float screenW = Screen.width;
        float screenH = Screen.height;

        float pivotX, pivotY;
        Vector2 spawnPosition;

        // 1. ГОРИЗОНТАЛЬНАЯ ЛОГИКА (Определяем, сбоку ли мы)
        bool isSide = false;

        if (targetCenter.x > screenW * 0.75f) // Объект в правой четверти экрана
        {
            pivotX = 1f; // Тултип растет влево
            spawnPosition.x = corners[0].x - distanceToTarget; // Левая граница объекта
            isSide = true;
        }
        else if (targetCenter.x < screenW * 0.25f) // Объект в левой четверти экрана
        {
            pivotX = 0f; // Тултип растет вправо
            spawnPosition.x = corners[2].x + distanceToTarget; // Правая граница объекта
            isSide = true;
        }
        else // Объект в центре экрана
        {
            pivotX = 0.5f; // Тултип центрирован по X
            spawnPosition.x = targetCenter.x;
        }

        // 2. ВЕРТИКАЛЬНАЯ ЛОГИКА И ЦЕНТРОВКА
        if (isSide)
        {
            // Если тултип появился сбоку, выравниваем его строго по центру элемента по вертикали
            pivotY = 0.5f;
            spawnPosition.y = targetCenter.y;
        }
        else
        {
            // Если тултип сверху или снизу от объекта
            if (targetCenter.y > screenH * 0.5f) // Объект в верхней части экрана
            {
                pivotY = 1f; // Тултип растет вниз
                spawnPosition.y = corners[0].y - distanceToTarget; // Нижняя граница объекта
            }
            else // Объект в нижней части экрана
            {
                pivotY = 0f; // Тултип растет вверх
                spawnPosition.y = corners[1].y + distanceToTarget; // Верхняя граница объекта
            }
        }

        // Применяем настройки
        _rectTransform.pivot = new Vector2(pivotX, pivotY);
        _rectTransform.position = spawnPosition;
    }

    private void Fade(float targetAlpha)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }
}
