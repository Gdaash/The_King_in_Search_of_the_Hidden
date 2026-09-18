using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ProductionCycle
{
    [Tooltip("Ресурсы, которые спаунятся в этом цикле")]
    public List<ResourceOutput> resources = new List<ResourceOutput>();
    
    [Tooltip("Спрайт для этого цикла")]
    public Sprite cycleSprite;
}

public class ResourceVein : ResourceRequester
{
    [Header("Циклы производства")]
    [Tooltip("Список циклов. Каждый цикл имеет свои ресурсы и спрайт")]
    public List<ProductionCycle> cycles = new List<ProductionCycle>();
    
    [Header("Настройки человека")]
    [Tooltip("Тип ресурса 'Человек'")]
    public ResourceType humanResourceType;
    
    [Tooltip("Префаб человека для выплёвывания при снятии флага")]
    public GameObject humanPrefab;
    
    [Header("Объект после уничтожения")]
    [Tooltip("Объект, который создаётся под жилой после последнего цикла")]
    public GameObject finalObject;
    
    [Header("Бамп-анимация")]
    [SerializeField] private float bumpScale = 1.2f;
    [SerializeField] private float bumpDuration = 0.3f;
    
    [Header("Задержка перед стартом цикла")]
    [Tooltip("Время ожидания (в секундах) после смены спрайта перед вызовом OnCycleStart")]
    [SerializeField] private float cycleStartDelay = 0.5f;
    
    [Header("Задержка перед уничтожением")]
    [Tooltip("Время (в секундах) между отключением спрайта и уничтожением жилы")]
    [SerializeField] private float destroyDelay = 1.0f;
    
    [Header("События циклов")]
    [Tooltip("Вызывается при старте каждого цикла (после задержки). Прокиньте сюда метод таймера")]
    public UnityEvent OnCycleStart;
    
    private int _currentCycleIndex = 0;
    private bool _isWorking = false;
    private SpriteRenderer _spriteRenderer;
    private bool _hadFlagAtCycleStart = false;
    private Coroutine _bumpRoutine;
    private Coroutine _delayedStartRoutine;
    private int _humansConsumed = 0;
    private bool _isReturningHumans = false;

    protected override void Awake()
    {
        base.Awake();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (cycles.Count > 0 && _spriteRenderer != null && cycles[0].cycleSprite != null)
        {
            _spriteRenderer.sprite = cycles[0].cycleSprite;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _currentCycleIndex = 0;
        _isWorking = false;
        _humansConsumed = 0;
        _isReturningHumans = false;
        
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.ForceUpdateOrders();
        }
    }

    private void OnDisable()
    {
        if (_bumpRoutine != null) StopCoroutine(_bumpRoutine);
        if (_delayedStartRoutine != null) StopCoroutine(_delayedStartRoutine);
    }

    protected override void CheckCompletion()
    {
        if (_isReturningHumans) return;
        
        var humanReq = requirements.FirstOrDefault(r => r.resourceType == humanResourceType);
        if (humanReq != null && humanReq.currentAmount >= humanReq.requiredAmount)
        {
            _humansConsumed = humanReq.requiredAmount;
            OnAllResourcesReceived?.Invoke();
            StartCycle();
        }
    }

    private void StartCycle()
    {
        if (_currentCycleIndex >= cycles.Count) 
        {
            FinishVein();
            return;
        }
        
        var cycle = cycles[_currentCycleIndex];
        _isWorking = true;
        _hadFlagAtCycleStart = HasLogisticFlag();
        
        if (_bumpRoutine != null) StopCoroutine(_bumpRoutine);
        if (_delayedStartRoutine != null) StopCoroutine(_delayedStartRoutine);
        
        if (_spriteRenderer != null && cycle.cycleSprite != null)
        {
            _spriteRenderer.sprite = cycle.cycleSprite;
            _bumpRoutine = StartCoroutine(BumpAnimationRoutine());
        }
        
        outputResources = cycle.resources;
        _delayedStartRoutine = StartCoroutine(DelayedCycleStartRoutine());
    }

    private IEnumerator DelayedCycleStartRoutine()
    {
        yield return new WaitForSeconds(cycleStartDelay);
        
        if (OnCycleStart != null)
        {
            OnCycleStart.Invoke();
        }
    }

    public void OnCycleComplete()
    {
        if (!_isWorking) return;
        
        _isWorking = false;
        
        var cycle = cycles[_currentCycleIndex];
        
        SpawnAllResults();
        
        bool flagNowPresent = HasLogisticFlag();
        
        if (_hadFlagAtCycleStart && flagNowPresent)
        {
            ResetHumanRequirements();
        }
        else
        {
            ReturnHumansToMap();
        }
        
        bool isLastCycle = _currentCycleIndex >= cycles.Count - 1;
        
        if (isLastCycle)
        {
            FinishVein();
        }
        else
        {
            if (flagNowPresent)
            {
                _currentCycleIndex++;
                StartCycle();
            }
            else
            {
                // Приостанавливаем жилу, сохраняя прогресс
                var humanReq = requirements.FirstOrDefault(r => r.resourceType == humanResourceType);
                if (humanReq != null)
                {
                    humanReq.currentAmount = 0;
                    humanReq.reservedAmount = 0;
                }
                _humansConsumed = 0;
                UpdateIndicator();
                if (OrderManager.Instance != null)
                {
                    OrderManager.Instance.ForceUpdateOrders();
                }
            }
        }
    }

    private void ReturnHumansToMap()
    {
        if (humanPrefab == null || _humansConsumed <= 0) return;
        
        _isReturningHumans = true;
        
        Collider2D veinCollider = GetComponent<Collider2D>();
        bool wasEnabled = false;
        if (veinCollider != null)
        {
            wasEnabled = veinCollider.enabled;
            veinCollider.enabled = false;
        }
        
        for (int i = 0; i < _humansConsumed; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f), 
                0
            );
            Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        }
        
        StartCoroutine(ReenableColliderAfterDelay(veinCollider, wasEnabled, 1.0f));
        
        _humansConsumed = 0;
        _isReturningHumans = false;
    }

    private IEnumerator ReenableColliderAfterDelay(Collider2D collider, bool originalState, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null)
        {
            collider.enabled = originalState;
        }
    }

    private void ResetHumanRequirements()
    {
        var humanReq = requirements.FirstOrDefault(r => r.resourceType == humanResourceType);
        if (humanReq != null)
        {
            humanReq.currentAmount = 0;
            humanReq.reservedAmount = 0;
        }
        _humansConsumed = 0;
    }

    private void FinishVein()
    {
        if (!HasLogisticFlag() && _humansConsumed > 0)
        {
            ReturnHumansToMap();
        }
        
        if (finalObject != null)
        {
            Instantiate(finalObject, transform.position, Quaternion.identity);
        }
        
        StartCoroutine(DisableSpriteAndDestroyRoutine());
    }

    private IEnumerator DisableSpriteAndDestroyRoutine()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = false;
        }
        
        yield return new WaitForSeconds(destroyDelay);
        
        Destroy(gameObject);
    }

    private IEnumerator BumpAnimationRoutine()
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < bumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bumpDuration;
            float curve = Mathf.Sin(t * Mathf.PI);
            transform.localScale = originalScale * Mathf.Lerp(1f, bumpScale, curve);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public override bool IsStorageFull() => false;
    public override void FinishProcessing() { }
}