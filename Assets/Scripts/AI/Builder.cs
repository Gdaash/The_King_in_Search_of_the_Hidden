using UnityEngine;
using System.Linq;
using System.Collections;

public class Builder : MonoBehaviour, IEnemyAI
{
    [Header("Настройки")]
    public float stopDistance = 0.5f;

    [Header("Визуал (Без аниматора)")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite normalSprite;    // Обычный спрайт (ходьба)
    [SerializeField] private Sprite buildingSprite;  // Спрайт в позе стройки
    [SerializeField] private Transform hammerPivot;  // Объект молотка (вложенный)

    [Header("Настройки молотка")]
    [SerializeField] private float hammerSpeed = 10f; // Скорость маха
    [SerializeField] private float hammerAngle = 30f; // Угол наклона молотка

    private EnemyMovement _movement;
    private ConstructionSite _targetSite;
    private bool _isBuilding = false;

    // Реализация интерфейса IEnemyAI для EnemyMovement
    public Transform GetTarget() => _targetSite != null ? _targetSite.transform : null;

    void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (hammerPivot != null) hammerPivot.gameObject.SetActive(false);
    }

    void Update()
    {
        // Если мы уже в процессе махания молотком, ничего не делаем
        if (_isBuilding) return;

        if (_targetSite == null)
        {
            FindWork();
            if (_movement != null) _movement.SetMove(false);
        }
        else
        {
            // Проверяем, не достроил ли кто-то здание до нас
            if (_targetSite.isConstructed)
            {
                _targetSite = null;
                if (_movement != null) _movement.SetMove(false);
                return;
            }

            // Включаем движение к цели
            if (_movement != null) _movement.SetMove(true);
            
            // Проверка дистанции прибытия
            float dist = Vector2.Distance(transform.position, _targetSite.transform.position);
            if (dist <= stopDistance)
            {
                if (_movement != null) _movement.SetMove(false);
                StartCoroutine(BuildRoutine());
            }
        }
    }

    private void FindWork()
    {
        // Ищем площадки, где ресурсы собраны, но стройка не завершена
        var sites = Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None)
            .Where(s => s.isResourcesReady && !s.isConstructed && s.enabled)
            .OrderBy(s => Vector2.Distance(transform.position, s.transform.position))
            .ToList();

        if (sites.Count > 0)
        {
            _targetSite = sites[0];
        }
    }

    private IEnumerator BuildRoutine()
    {
        _isBuilding = true;
        
        if (_targetSite != null)
        {
            // ПРИНУДИТЕЛЬНЫЙ РАЗВОРОТ К ЗДАНИЮ (через Scale)
            // Так как EnemyMovement на стопе, разворачиваемся вручную лицом к цели
            float diff = _targetSite.transform.position.x - transform.position.x;
            float initialScaleX = Mathf.Abs(transform.localScale.x);
            float newScaleX = diff > 0 ? initialScaleX : -initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);

            // Меняем спрайт и включаем молоток
            if (bodyRenderer != null) bodyRenderer.sprite = buildingSprite;
            if (hammerPivot != null) hammerPivot.gameObject.SetActive(true);

            Debug.Log($"Строитель приступил к {_targetSite.gameObject.name}");

            // Цикл пока здание не будет готово
            while (_targetSite != null && !_targetSite.isConstructed)
            {
                // Двигаем таймер стройки в самом объекте ConstructionSite
                _targetSite.AdvanceConstruction(Time.deltaTime);

                // Анимация маха молотком (математическая)
                if (hammerPivot != null)
                {
                    float rotation = Mathf.Sin(Time.time * hammerSpeed) * hammerAngle;
                    hammerPivot.localRotation = Quaternion.Euler(0, 0, rotation);
                }

                yield return null;
            }

            // Убираем молоток и возвращаем обычный спрайт
            if (hammerPivot != null) hammerPivot.gameObject.SetActive(false);
            if (bodyRenderer != null) bodyRenderer.sprite = normalSprite;
        }
        
        _isBuilding = false;
        _targetSite = null; // Ищем новую работу в следующем Update
    }
}