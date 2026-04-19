using UnityEngine;

public class AutoClickWorker : MonoBehaviour 
{
    [Header("Настройки визуала")]
    [SerializeField] private SpriteRenderer workerRenderer;
    [SerializeField] private Sprite idleSprite;   // Просто стоит
    [SerializeField] private Sprite workingSprite; // В процессе "клика"

    private int _requestersUnderFoot = 0;

    void Awake()
    {
        if (workerRenderer == null) workerRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ClickRequester>())
        {
            _requestersUnderFoot++;
            UpdateVisual();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ClickRequester>())
        {
            _requestersUnderFoot = Mathf.Max(0, _requestersUnderFoot - 1);
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (workerRenderer == null || idleSprite == null || workingSprite == null) return;
        workerRenderer.sprite = (_requestersUnderFoot > 0) ? workingSprite : idleSprite;
    }
}