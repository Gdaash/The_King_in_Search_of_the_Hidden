using UnityEngine;

public class LogisticFlag : MonoBehaviour
{
    [Header("Настройки визуала")]
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite idleSprite;   // Спрайт, когда флаг просто лежит на земле
    [SerializeField] private Sprite activeSprite; // Спрайт, когда флаг на здании

    private int _buildingsUnderFlag = 0; // Счетчик зданий под флажком

    void Awake()
    {
        if (flagRenderer == null) flagRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    // Срабатывает, когда коллайдер флажка входит в коллайдер здания
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, есть ли на объекте скрипт запроса ресурсов
        if (collision.TryGetComponent<ResourceRequester>(out var requester))
        {
            _buildingsUnderFlag++;
            UpdateVisual();
        }
    }

    // Срабатывает, когда коллайдер флажка выходит из коллайдера здания
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<ResourceRequester>(out var requester))
        {
            _buildingsUnderFlag = Mathf.Max(0, _buildingsUnderFlag - 1);
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (flagRenderer == null || idleSprite == null || activeSprite == null) return;

        // Если под флагом есть хотя бы одно здание — ставим активный спрайт
        flagRenderer.sprite = (_buildingsUnderFlag > 0) ? activeSprite : idleSprite;
    }
}