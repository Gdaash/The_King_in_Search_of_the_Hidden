using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("2D Light компонент (тип Spot)")]
    [SerializeField] private Light2D spotLight;
    
    [Tooltip("Объект-маркер, который таскается мышкой")]
    [SerializeField] private GameObject marker;

    [Header("Настройки перетаскивания")]
    [Tooltip("На каком слое находится маркер (для raycast)")]
    [SerializeField] private LayerMask markerLayer;

    [Header("Настройки плавности")]
    [Tooltip("Скорость поворота света (чем больше, тем быстрее)")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Tooltip("Скорость изменения радиуса света")]
    [SerializeField] private float radiusSpeed = 10f;

    [Header("Настройки конуса")]
    [Tooltip("Желаемая толщина конца луча (в мировых единицах)")]
    [SerializeField] private float targetBeamWidth = 1.5f;
    
    [Tooltip("Минимальный угол конуса (чтобы луч не схлопывался в ноль)")]
    [SerializeField] private float minSpotAngle = 5f;
    
    [Tooltip("Максимальный угол конуса (ограничение для близкого маркера)")]
    [SerializeField] private float maxSpotAngle = 45f;

    private bool _isDragging = false;
    private Camera _mainCamera;
    private float _currentRadius;
    private float _currentSpotAngle;
    private float _currentInnerAngle; // НОВОЕ: текущий внутренний угол
    
    private HexLightUnlocker _currentHex;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        if (spotLight == null)
            spotLight = GetComponent<Light2D>();
        
        _currentRadius = spotLight.pointLightOuterRadius;
        _currentSpotAngle = spotLight.pointLightOuterAngle;
        _currentInnerAngle = spotLight.pointLightInnerAngle; // Инициализация внутреннего угла
    }

    private void Update()
    {
        HandleDragging();
        UpdateLight();
        CheckHexUnderMarker();
    }

    private void HandleDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, markerLayer);
            
            if (hit.collider != null && hit.transform.gameObject == marker)
            {
                _isDragging = true;
            }
        }

        if (_isDragging && Input.GetMouseButton(0))
        {
            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            marker.transform.position = mousePos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            CenterOnCurrentHex();
        }
    }

    private void UpdateLight()
    {
        if (spotLight == null || marker == null) return;

        if (!marker.activeInHierarchy)
        {
            spotLight.enabled = false;
            return;
        }

        spotLight.enabled = true;

        Vector2 direction = (Vector2)marker.transform.position - (Vector2)transform.position;
        float distance = direction.magnitude;
        
        if (distance < 0.01f) distance = 0.01f;

        // === ПОВОРОТ ===
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
        
        // === РАДИУС ===
        float targetRadius = distance;
        _currentRadius = Mathf.Lerp(_currentRadius, targetRadius, Time.deltaTime * radiusSpeed);
        spotLight.pointLightOuterRadius = _currentRadius;

        // === ВНЕШНИЙ УГОЛ КОНУСА ===
        float targetSpotAngle = 2f * Mathf.Atan(targetBeamWidth / (2f * distance)) * Mathf.Rad2Deg;
        targetSpotAngle = Mathf.Clamp(targetSpotAngle, minSpotAngle, maxSpotAngle);
        _currentSpotAngle = Mathf.Lerp(_currentSpotAngle, targetSpotAngle, Time.deltaTime * radiusSpeed);
        spotLight.pointLightOuterAngle = _currentSpotAngle;

        // === ВНУТРЕННИЙ УГОЛ КОНУСА (НОВОЕ) ===
        // Внутренний угол всегда равен половине внешнего
        float targetInnerAngle = _currentSpotAngle / 2f;
        _currentInnerAngle = Mathf.Lerp(_currentInnerAngle, targetInnerAngle, Time.deltaTime * radiusSpeed);
        spotLight.pointLightInnerAngle = _currentInnerAngle;
    }

    private void CheckHexUnderMarker()
    {
        if (marker == null) return;

        Vector2 markerPos = marker.transform.position;

        // Защита от мерцания
        if (_currentHex != null && !_currentHex.IsUnlocked())
        {
            if (_currentHex.IsPointOverHex(markerPos))
            {
                return; 
            }
        }

        // Ищем новый гекс из списка активных экземпляров
        HexLightUnlocker newHex = null;
        foreach (var hex in HexLightUnlocker.ActiveInstances)
        {
            if (hex == null || hex.IsUnlocked() || hex == _currentHex) continue;

            if (hex.IsPointOverHex(markerPos))
            {
                newHex = hex;
                break;
            }
        }

        if (newHex != null)
        {
            if (_currentHex != null)
            {
                _currentHex.CancelUnlockProcess();
            }

            _currentHex = newHex;
            
            int dangerLevel = 1;
            HexBlocker hexBlocker = _currentHex.GetComponent<HexBlocker>();
            if (hexBlocker != null)
            {
                dangerLevel = hexBlocker.assignedDangerLevel;
                if (dangerLevel <= 0) dangerLevel = 1;
            }

            _currentHex.StartUnlockProcess(dangerLevel);
        }
        else if (_currentHex != null)
        {
            _currentHex.CancelUnlockProcess();
            _currentHex = null;
        }
    }

    private void CenterOnCurrentHex()
    {
        if (marker == null) return;

        Vector2 markerPos = marker.transform.position;

        // 1. Сначала проверяем, есть ли рядом открытый гекс (магнит)
        HexMagnet nearestMagnet = null;
        float minDistance = float.MaxValue;

        foreach (var magnet in HexMagnet.ActiveInstances)
        {
            if (magnet == null) continue;

            if (magnet.IsPointOverMagnet(markerPos))
            {
                float dist = Vector2.Distance(markerPos, (Vector2)magnet.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestMagnet = magnet;
                }
            }
        }

        // Если нашли магнит — притягиваем маркер к нему
        if (nearestMagnet != null)
        {
            StartCoroutine(SmoothCenterOnHex(nearestMagnet.transform.position));
            return;
        }

        // 2. Если нет магнита, используем старую логику с закрытым гексом
        if (_currentHex != null && !_currentHex.IsUnlocked())
        {
            StartCoroutine(SmoothCenterOnHex(_currentHex.transform.position));
        }
    }

    private IEnumerator SmoothCenterOnHex(Vector2 targetPosition)
    {
        Vector2 startPos = marker.transform.position;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            marker.transform.position = Vector2.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        marker.transform.position = targetPosition;
    }

    public void SetLightActive(bool active)
    {
        if (spotLight != null)
        {
            spotLight.enabled = active;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (marker != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(marker.transform.position, targetBeamWidth / 2f);
        }
    }
    // ... (весь предыдущий код FlashlightController остаётся без изменений) ...

    /// <summary>
    /// Мгновенно перемещает маркер в указанную точку (для притягивания к открытому гексу)
    /// </summary>
    public void SnapMarkerTo(Vector2 targetPosition)
    {
        if (marker != null)
        {
            // Сбрасываем текущую цель перетаскивания, чтобы маркер не "улетел" обратно
            _isDragging = false;
            _currentHex = null; 
            
            // Мгновенно перемещаем маркер
            marker.transform.position = targetPosition;
        }
    }
}