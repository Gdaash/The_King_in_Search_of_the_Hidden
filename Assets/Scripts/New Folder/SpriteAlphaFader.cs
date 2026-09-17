using UnityEngine;

public class SpriteAlphaFader : MonoBehaviour
{
    [Header("Настройки ссылок")]
    [Tooltip("Спрайт рендер, прозрачность которого нужно менять. Если оставить пустым, скрипт возьмет SpriteRenderer с текущего объекта.")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("Скорости анимации")]
    [Tooltip("Скорость появления (переход к полной непрозрачности)")]
    public float fadeInSpeed = 5f;

    [Tooltip("Скорость исчезновения (переход к полной прозрачности)")]
    public float fadeOutSpeed = 5f;

    private bool isHovered = false;
    private float currentAlpha;
    private bool hasCollider2D;
    private Camera mainCam;

    private void Awake()
    {
        // Если спрайт рендер не назначен вручную в инспекторе, берем тот, что на объекте
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetSpriteRenderer != null)
        {
            // Сохраняем стартовую альфу, чтобы не было резких скачков при старте
            currentAlpha = targetSpriteRenderer.color.a; 
        }

        // Проверяем наличие коллайдера для оптимизации проверок в Update
        hasCollider2D = GetComponent<Collider2D>() != null;
        
        // Кэшируем камеру, чтобы не искать её каждый кадр
        mainCam = Camera.main;
    }

    // Срабатывает автоматически Unity, если на объекте есть Collider2D (или Collider)
    private void OnMouseEnter()
    {
        if (hasCollider2D) isHovered = true;
    }

    private void OnMouseExit()
    {
        if (hasCollider2D) isHovered = false;
    }

    private void Update()
    {
        if (targetSpriteRenderer == null || mainCam == null) return;

        // Если коллайдера нет, определяем наведение по границам спрайта (работает без коллайдера)
        if (!hasCollider2D)
        {
            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            
            // Проверяем попадание только по X и Y, чтобы избежать проблем с Z-глубиной камеры в 2D
            isHovered = mousePos.x >= targetSpriteRenderer.bounds.min.x &&
                        mousePos.x <= targetSpriteRenderer.bounds.max.x &&
                        mousePos.y >= targetSpriteRenderer.bounds.min.y &&
                        mousePos.y <= targetSpriteRenderer.bounds.max.y;
        }

        // Определяем целевую прозрачность
        float targetAlpha = isHovered ? 1f : 0f;

        // Оптимизация: если альфа уже равна целевой, не пересчитываем цвет
        if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

        // Выбираем нужную скорость в зависимости от направления анимации
        float speed = isHovered ? fadeInSpeed : fadeOutSpeed;

        // Плавно интерполируем значение альфы
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);

        // Применяем новый цвет, сохраняя оригинальные каналы R, G и B
        Color currentColor = targetSpriteRenderer.color;
        targetSpriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha);
    }
}