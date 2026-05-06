using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; // Добавлено для работы с эвентами

public class LogisticFlag : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite idleSprite;   
    [SerializeField] private Sprite activeSprite; 

    [Header("События")]
    [SerializeField] private UnityEvent onActivated; // Сработает при смене на activeSprite

    private int _buildingsUnderFlag = 0;
    private BoxCollider2D _myCollider;
    private bool _isActive = false; // Флаг для контроля смены состояния

    void Awake() {
        _myCollider = GetComponent<BoxCollider2D>();
        UpdateVisual();
    }

    void OnEnable() => StartCoroutine(ValidationRoutine());

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
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        List<Collider2D> results = new List<Collider2D>();
        
        int count = _myCollider.Overlap(filter, results);
        _buildingsUnderFlag = 0;
        
        for (int i = 0; i < count; i++) {
            if (results[i].TryGetComponent<ResourceRequester>(out var req)) {
                _buildingsUnderFlag++;
                req.UpdateIndicator(); 
            }
        }
        UpdateState(); // Заменяем UpdateVisual на проверку состояния
        if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.GetComponent<ResourceRequester>()) {
            _buildingsUnderFlag++;
            UpdateState();
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.GetComponent<ResourceRequester>()) {
            _buildingsUnderFlag = Mathf.Max(0, _buildingsUnderFlag - 1);
            UpdateState();
        }
    }

    // Новый метод для контроля логики активации
    private void UpdateState() {
        bool shouldBeActive = _buildingsUnderFlag > 0;

        if (shouldBeActive && !_isActive) {
            _isActive = true;
            onActivated?.Invoke(); // Вызываем событие только в момент "включения"
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
