using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class HealthBar : MonoBehaviour
{
    [Header("Ссылки на полоски")]
    [SerializeField] private Image mainBar;
    [SerializeField] private Image bufferBar;
    
    [Header("Настройки")]
    [SerializeField] private float smoothSpeed = 5f; 
    [SerializeField] private float fadeSpeed = 3f;   

    private CanvasGroup _canvasGroup;
    private float _targetProgress = 1f;
    private Coroutine _fadeRoutine;
    private Transform _mainCameraTransform;
    private Vector3 _initialLocalScale; // Запоминаем начальный размер

    public const float UnitWidth = .35f * 1.15f, UnitHeight = .14f;
    private bool initialized;
    private Health owner;
    private bool unitBar, portalBar, subscribed;
    private RectTransform frame;
    private Image shieldBar;
    private readonly Vector3[] corners = new Vector3[4];

    private void OnEnable()
    {
        owner = GetComponentInParent<Health>();
        if (owner == null) return;
        owner.OnHealthChanged ??= new UnityEngine.Events.UnityEvent<float>();
        bool authored = false;
        for (int i = 0; i < owner.OnHealthChanged.GetPersistentEventCount(); i++)
            if (owner.OnHealthChanged.GetPersistentTarget(i) == this && owner.OnHealthChanged.GetPersistentMethodName(i) == nameof(SetProgress)) authored = true;
        if (!authored) { owner.OnHealthChanged.AddListener(SetProgress); subscribed = true; }
        SetProgress(owner.NormalizedHealth);
    }
    private void OnDisable()
    {
        if (subscribed && owner != null) owner.OnHealthChanged.RemoveListener(SetProgress);
        subscribed = false;
        StopAllCoroutines(); _fadeRoutine = null;
    }
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _initialLocalScale = transform.localScale; // Запоминаем (напр. 0.01, 0.01, 0.01)
        unitBar = GetComponentInParent<EnemyMovement>() != null || GetComponentInParent<ResourceWorker>() != null ||
            GetComponentInParent<GameFoundation.MetaProgression.WorldEngineer>() != null ||
            GetComponentInParent<GameFoundation.MetaProgression.WorldBallista>() != null;
        portalBar = GetComponentInParent<PortalMeleeBoundary>() != null;
        foreach (var image in GetComponentsInChildren<Image>(true))
        {
            image.raycastTarget = false;
            if (image != mainBar && image != bufferBar) frame = image.rectTransform;
        }
        if (unitBar && TryGetComponent<Canvas>(out var canvas))
        { canvas.overrideSorting = true; canvas.sortingLayerName = "OverLight"; canvas.sortingOrder = 500; }
        
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
        initialized = true;
    }

    private void Start()
    {
        SetProgress(owner != null ? owner.NormalizedHealth : 1f);
        if (bufferBar) bufferBar.fillAmount = _targetProgress;
        _canvasGroup.alpha = (_targetProgress > 0 && _targetProgress < 1) || (owner != null && owner.ShieldHealth > 0) ? 1 : 0;
    }

    public void ResetForSpawn()
    {
        StopAllCoroutines(); _fadeRoutine = null; _targetProgress = 1f;
        if (mainBar != null) mainBar.fillAmount = 1f;
        if (shieldBar != null) shieldBar.enabled = false;
        if (bufferBar != null) bufferBar.fillAmount = 1f;
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
    }

    private void LateUpdate()
    {
        // 1. BILLBOARD: Поворот к камере
        if (_mainCameraTransform != null)
        {
            transform.rotation = _mainCameraTransform.rotation;
        }

        // 2. FIX SCALE: Компенсация разворота родителя (Flip)
        // Мы берем абсолютное значение скейла родителя, чтобы полоска всегда была направлена "вперед"
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        
        transform.localScale = new Vector3(
            _initialLocalScale.x / Mathf.Sign(parentScale.x), 
            _initialLocalScale.y / Mathf.Sign(parentScale.y), 
            _initialLocalScale.z
        );
        if (unitBar && owner != null && owner.BodyRenderer != null && frame != null)
        {
            frame.GetWorldCorners(corners);
            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);
            var scale = transform.localScale;
            if (width > .00001f) scale.x *= UnitWidth / width;
            if (height > .00001f) scale.y *= UnitHeight / height;
            transform.localScale = scale;
            var body = owner.BodyRenderer;
            float gap = body.sprite != null ? 2f / body.sprite.pixelsPerUnit * Mathf.Abs(body.transform.lossyScale.y) : .0625f;
            transform.position = new Vector3(body.bounds.center.x, body.bounds.max.y + gap + UnitHeight * .5f, owner.transform.position.z);
        }
        else if (portalBar && frame != null)
        {
            frame.GetWorldCorners(corners);
            float height = Vector3.Distance(corners[0], corners[1]);
            if (height > .00001f)
            {
                var scale = transform.localScale;
                scale.y *= UnitHeight / height;
                transform.localScale = scale;
            }
        }
    }

    public void SetProgress(float progress)
    {
        bool shielded = owner != null && owner.ShieldHealth > 0;
        float capacity = shielded ? owner.MaxHealth + owner.ShieldCapacity : 0;
        _targetProgress = shielded ? owner.CurrentHealth / capacity : Mathf.Clamp01(progress);
        if (mainBar != null) mainBar.fillAmount = _targetProgress;
        UpdateShieldBar(shielded, capacity);
        bool show = shielded || (_targetProgress > 0 && _targetProgress < .99999f);
        // Parent Awake (EnemyLevel) may publish health before this child's Awake.
        if (!initialized || !isActiveAndEnabled)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null) _canvasGroup.alpha = show ? 1 : 0;
            return;
        }
        ToggleVisibility(show);
    }

    private void UpdateShieldBar(bool shielded, float capacity)
    {
        if (shielded && shieldBar == null && mainBar != null)
        {
            var visual = new GameObject("Shield health", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(mainBar.transform, false);
            shieldBar = visual.GetComponent<Image>();
            shieldBar.color = new Color(.72f, .73f, .78f);
            shieldBar.raycastTarget = false;
        }
        if (shieldBar == null) return;
        shieldBar.enabled = shielded;
        if (!shielded) return;
        var rect = shieldBar.rectTransform;
        rect.anchorMin = new Vector2(_targetProgress, 0);
        rect.anchorMax = new Vector2(Mathf.Clamp01(_targetProgress + owner.ShieldHealth / capacity), 1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (bufferBar == null) return;

        if (!Mathf.Approximately(bufferBar.fillAmount, _targetProgress))
        {
            bufferBar.fillAmount = Mathf.Lerp(bufferBar.fillAmount, _targetProgress, Time.deltaTime * smoothSpeed);
        }

        if ((owner == null || owner.ShieldHealth <= 0) &&
            ((_targetProgress >= .99999f && bufferBar.fillAmount >= .999f) || _targetProgress <= 0))
        {
            if (_canvasGroup.alpha > 0) ToggleVisibility(false);
        }
    }

    private void ToggleVisibility(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        if (Mathf.Approximately(_canvasGroup.alpha, targetAlpha)) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(targetAlpha));
    }

    private IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }
}
