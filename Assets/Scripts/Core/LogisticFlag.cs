using UnityEngine;
using System.Collections;

public class LogisticFlag : MonoBehaviour
{
    [Header("Настройки визуала")]
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite idleSprite;   
    [SerializeField] private Sprite activeSprite; 

    private int _buildingsUnderFlag = 0;

    void Awake()
    {
        if (flagRenderer == null) flagRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    // Вызывается через DragObj (событие OnEndDrag) или OnMouseUp
    public void OnMouseUp()
    {
        StopAllCoroutines();
        StartCoroutine(NotifyRoutine());
    }

    private IEnumerator NotifyRoutine()
    {
        // Ждем физический кадр, чтобы позиция флага зафиксировалась
        yield return new WaitForFixedUpdate();

        // Находим все здания под флагом
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<ResourceRequester>(out var req))
            {
                // Принудительно обновляем индикаторы здания
                req.UpdateIndicator(); 
            }
        }
        
        // ИСПРАВЛЕНО: Вместо Porter.NotifyAllPorters() вызываем обновление в Менеджере
        if (OrderManager.Instance != null)
        {
            // Мы просто заставляем менеджер выполнить проверку немедленно
            OrderManager.Instance.ForceUpdateOrders();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ResourceRequester>())
        {
            _buildingsUnderFlag++;
            UpdateVisual();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ResourceRequester>())
        {
            _buildingsUnderFlag = Mathf.Max(0, _buildingsUnderFlag - 1);
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (flagRenderer == null || idleSprite == null || activeSprite == null) return;
        flagRenderer.sprite = (_buildingsUnderFlag > 0) ? activeSprite : idleSprite;
    }
}
