using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class LogisticFlag : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite idleSprite;   
    [SerializeField] private Sprite activeSprite; 

    [Header("События")]
    [SerializeField] private UnityEvent onActivated;

    private int _buildingsUnderFlag = 0;
    private Collider2D _myCollider; // Изменили на базовый Collider2D
    private bool _isActive = false;

    void Awake() {
        _myCollider = GetComponent<Collider2D>();
        UpdateVisual();
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
        
        // ИСПРАВЛЕНИЕ: Используем OverlapCircleAll, который игнорирует настройку Is Trigger
        // и находит ВСЕ коллайдеры в радиусе. Радиус берем из размера коллайдера флага.
        float checkRadius = _myCollider.bounds.size.x * 0.5f; 
        Collider2D[] results = Physics2D.OverlapCircleAll(transform.position, checkRadius);
        
        _buildingsUnderFlag = 0;
        
        foreach (var col in results) {
            // Проверяем наличие нужного скрипта, игнорируя сам флаг
            if (col.gameObject != gameObject && col.TryGetComponent<ResourceRequester>(out var req)) {
                _buildingsUnderFlag++;
                req.UpdateIndicator(); 
            }
        }
        
        UpdateState();
        if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
    }

    // Оставляем триггеры для мгновенной реакции, но добавляем и коллизии на всякий случай
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

    // На случай, если коллайдер здания НЕ является триггером
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

    private void UpdateState() {
        bool shouldBeActive = _buildingsUnderFlag > 0;

        if (shouldBeActive && !_isActive) {
            _isActive = true;
            onActivated?.Invoke();
        } else if (!shouldBeActive) {
            _isActive = false;
        }

        UpdateVisual();
    }

    private void UpdateVisual() {
        if (flagRenderer && idleSprite && activeSprite)
            flagRenderer.sprite = _isActive ? activeSprite : idleSprite;
    }
}