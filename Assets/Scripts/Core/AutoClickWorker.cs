using UnityEngine;
using UnityEngine.Events; // Не забудь добавить этот namespace

public class AutoClickWorker : MonoBehaviour 
{
    [Header("Настройки визуала")]
    [SerializeField] private SpriteRenderer workerRenderer;
    [SerializeField] private Sprite idleSprite;   
    [SerializeField] private Sprite workingSprite; 

    [Header("События")]
    [SerializeField] private UnityEvent onStartedWorking; // Событие при начале работы

    private int _requestersUnderFoot = 0;
    private bool _isWorking = false; // Флаг текущего состояния

    void Awake()
    {
        if (workerRenderer == null) workerRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ClickRequester>())
        {
            _requestersUnderFoot++;
            UpdateState();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ClickRequester>())
        {
            _requestersUnderFoot = Mathf.Max(0, _requestersUnderFoot - 1);
            UpdateState();
        }
    }

    private void UpdateState()
    {
        bool shouldWork = _requestersUnderFoot > 0;

        // Если состояние изменилось с "покоя" на "работу"
        if (shouldWork && !_isWorking)
        {
            _isWorking = true;
            onStartedWorking?.Invoke(); // Вызываем эвент
        }
        else if (!shouldWork)
        {
            _isWorking = false;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (workerRenderer == null || idleSprite == null || workingSprite == null) return;
        workerRenderer.sprite = _isWorking ? workingSprite : idleSprite;
    }
}
