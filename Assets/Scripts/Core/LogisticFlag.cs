using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class LogisticFlag : MonoBehaviour
{
    [Header("Ссылки на спрайты (ВАЖНО: используйте два разных SpriteRenderer)")]
    [Tooltip("Спрайт-рендерер для состояния покоя")]
    [SerializeField] private SpriteRenderer idleRenderer;   
    
    [Tooltip("Спрайт-рендерер для активного состояния")]
    [SerializeField] private SpriteRenderer activeRenderer; 

    [Header("Настройки прозрачности (Alpha Fader)")]
    [Tooltip("Скорость плавного изменения прозрачности")]
    [SerializeField] private float fadeSpeed = 5f;
    
    [Tooltip("Насколько ярче становится спрайт при наведении мыши (1 = без изменений)")]
    [SerializeField] private float hoverAlphaBoost = 1.2f; 

    [Header("События")]
    [SerializeField] private UnityEvent onActivated;

    private int _buildingsUnderFlag = 0;
    private Collider2D _myCollider;
    private bool _isActive = false;
    private bool _isHovered = false;

    private float _targetIdleAlpha = 1f;
    private float _targetActiveAlpha = 0f;

    void Awake() {
        _myCollider = GetComponent<Collider2D>();
        
        if (activeRenderer != null) {
            Color c = activeRenderer.color;
            c.a = 0f;
            activeRenderer.color = c;
        }
        if (idleRenderer != null) {
            Color c = idleRenderer.color;
            c.a = 1f;
            idleRenderer.color = c;
        }
    }

    void OnEnable() => StartCoroutine(ValidationRoutine());
    void OnDisable() => StopAllCoroutines();

    private IEnumerator ValidationRoutine() {
        while (true) {
            yield return new WaitForSeconds(0.5f);
            if (_buildingsUnderFlag > 0 && OrderManager.Instance != null)
                OrderManager.Instance.ForceUpdateOrders();
        }
    }

    public void OnMouseUp() {
        StopAllCoroutines();
        StartCoroutine(NotifyRoutine());
        StartCoroutine(ValidationRoutine());
    }

    private IEnumerator NotifyRoutine() {
        yield return new WaitForFixedUpdate();
        
        float checkRadius = _myCollider != null ? _myCollider.bounds.size.x * 0.5f : 1f; 
        Collider2D[] results = Physics2D.OverlapCircleAll(transform.position, checkRadius);
        
        _buildingsUnderFlag = 0;
        
        foreach (var col in results) {
            if (col.gameObject != gameObject && col.TryGetComponent<ResourceRequester>(out var req)) {
                _buildingsUnderFlag++;
                req.UpdateIndicator(); 
            }
        }
        
        UpdateState();

        // Проверяем реальное положение мыши в момент отпускания
        if (Camera.main != null && _myCollider != null) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isHovered = _myCollider.OverlapPoint(mousePos);
        }
        
        UpdateVisualTargets();
        
        if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject != gameObject && collision.GetComponent<ResourceRequester>() != null) {
            _buildingsUnderFlag++;
            UpdateState();
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject != gameObject && collision.GetComponent<ResourceRequester>() != null) {
            _buildingsUnderFlag = Mathf.Max(0, _buildingsUnderFlag - 1);
            UpdateState();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject != gameObject && collision.collider.GetComponent<ResourceRequester>() != null) {
            _buildingsUnderFlag++;
            UpdateState();
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject != gameObject && collision.collider.GetComponent<ResourceRequester>() != null) {
            _buildingsUnderFlag = Mathf.Max(0, _buildingsUnderFlag - 1);
            UpdateState();
        }
    }

    private void OnMouseEnter() {
        _isHovered = true;
        UpdateVisualTargets();
    }

    private void OnMouseExit() {
        _isHovered = false;
        UpdateVisualTargets();
    }

    private void UpdateState() {
        bool shouldBeActive = _buildingsUnderFlag > 0;

        if (shouldBeActive && !_isActive) {
            _isActive = true;
            onActivated?.Invoke();
        } else if (!shouldBeActive) {
            _isActive = false;
        }

        // === ИСПРАВЛЕНИЕ: Принудительно проверяем положение мыши при смене состояния ===
        if (Camera.main != null && _myCollider != null) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isHovered = _myCollider.OverlapPoint(mousePos);
        }

        UpdateVisualTargets();
    }

    private void UpdateVisualTargets() {
        if (_isActive) {
            if (_isHovered) {
                _targetIdleAlpha = 1f;
                _targetActiveAlpha = Mathf.Clamp01(hoverAlphaBoost);
            } else {
                _targetIdleAlpha = 0f;
                _targetActiveAlpha = 1f;
            }
        } else {
            _targetIdleAlpha = _isHovered ? Mathf.Clamp01(hoverAlphaBoost) : 1f;
            _targetActiveAlpha = 0f;
        }
    }

    void Update() {
        if (idleRenderer != null) {
            Color idleColor = idleRenderer.color;
            idleColor.a = Mathf.MoveTowards(idleColor.a, _targetIdleAlpha, fadeSpeed * Time.deltaTime);
            idleRenderer.color = idleColor;
        }

        if (activeRenderer != null) {
            Color activeColor = activeRenderer.color;
            activeColor.a = Mathf.MoveTowards(activeColor.a, _targetActiveAlpha, fadeSpeed * Time.deltaTime);
            activeRenderer.color = activeColor;
        }
    }
}