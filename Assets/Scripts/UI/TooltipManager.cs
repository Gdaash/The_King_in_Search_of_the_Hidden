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
    [SerializeField] private Vector2 textPadding = new Vector2(16f, 12f);
    [SerializeField] private float maxWidth = 420f;
    [SerializeField] private float screenMargin = 8f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _fadeRoutine;
    private RectTransform _currentTarget;
    private RectTransform _textRect;

    private void Awake()
    {
        // Синглтон для быстрого доступа
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        if (tooltipText != null)
        {
            _textRect = tooltipText.rectTransform;
            // The original prefab uses a large font on a scaled-down text object.
            // Normalize it so text measurements match the visible size.
            tooltipText.fontSize = Mathf.Max(1, Mathf.RoundToInt(tooltipText.fontSize * _textRect.localScale.x));
            _textRect.localScale = Vector3.one;
            tooltipText.resizeTextForBestFit = false;
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipText.raycastTarget = false;
        }

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
        if (tooltipText != null)
        {
            tooltipText.text = description;
            ResizeToText();
        }

        UpdatePosition();
        Fade(1f);
    }

    private void ResizeToText()
    {
        var canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        scale = Mathf.Max(scale, 0.01f);
        float availableWidth = Screen.width / scale - 2f * screenMargin;
        float contentLimit = Mathf.Max(1f, Mathf.Min(maxWidth - 2f * textPadding.x,
            availableWidth - 2f * textPadding.x));

        tooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
        float contentWidth = Mathf.Min(tooltipText.preferredWidth, contentLimit);
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _textRect.anchorMin = _textRect.anchorMax = new Vector2(0.5f, 0.5f);
        _textRect.anchoredPosition = Vector2.zero;
        _textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        float contentHeight = tooltipText.preferredHeight;
        _textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth + 2f * textPadding.x);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight + 2f * textPadding.y);
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
        var canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        float width = _rectTransform.rect.width * scale;
        float height = _rectTransform.rect.height * scale;
        spawnPosition.x = Mathf.Clamp(spawnPosition.x,
            screenMargin + width * pivotX, screenW - screenMargin - width * (1f - pivotX));
        spawnPosition.y = Mathf.Clamp(spawnPosition.y,
            screenMargin + height * pivotY, screenH - screenMargin - height * (1f - pivotY));
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
