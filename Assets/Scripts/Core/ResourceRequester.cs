using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ResourceRequirement
{
    public ResourceType resourceType;
    public int requiredAmount;
    [HideInInspector] public int currentAmount = 0;
}

public class ResourceRequester : MonoBehaviour
{
    public static List<ResourceRequester> AllRequesters = new List<ResourceRequester>();

    [Header("Настройки рецепта")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
    public int priority = 1;

    [Header("Выходной ресурс (Результат)")]
    [SerializeField] private GameObject resultResourcePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpread = 0.5f;

    [Header("Визуал")]
    [SerializeField] private SpriteRenderer requestSpriteDisplay;
    [SerializeField] private float bobbingAmount = 0.1f;
    [SerializeField] private float bobbingSpeed = 2f;

    [Header("События")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived; // НОВОЕ СОБЫТИЕ
    public UnityEvent OnActionExecuted;

    private int _reservedAmount = 0; 
    private bool _isProcessing = false;
    private Vector3 _originalIconPos;

    void OnEnable() { AllRequesters.Add(this); UpdateIndicator(); }
    void OnDisable() => AllRequesters.Remove(this);

    void Start()
    {
        if (requestSpriteDisplay != null)
            _originalIconPos = requestSpriteDisplay.transform.localPosition;
        
        UpdateIndicator();
    }

    void Update()
    {
        if (requestSpriteDisplay != null && requestSpriteDisplay.gameObject.activeSelf)
        {
            float newY = _originalIconPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            requestSpriteDisplay.transform.localPosition = new Vector3(_originalIconPos.x, newY, _originalIconPos.z);
        }
    }

    public bool NeedsResource() 
    {
        if (_isProcessing) return false;
        int totalRequired = requirements.Sum(r => r.requiredAmount);
        int totalCurrent = requirements.Sum(r => r.currentAmount);
        return (totalCurrent + _reservedAmount) < totalRequired;
    }

    public void ReserveSpot() { _reservedAmount++; UpdateIndicator(); }
    public void UnreserveSpot() { _reservedAmount = Mathf.Max(0, _reservedAmount - 1); UpdateIndicator(); }

    public void DeliverResource(ResourceType type)
    {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null && req.currentAmount < req.requiredAmount)
        {
            req.currentAmount++;
            _reservedAmount = Mathf.Max(0, _reservedAmount - 1);
            
            OnResourceReceived?.Invoke();
            CheckCompletion();
            UpdateIndicator();
        }
    }

    private void CheckCompletion()
    {
        bool allMet = requirements.All(r => r.currentAmount >= r.requiredAmount);
        if (allMet)
        {
            _isProcessing = true;
            OnAllResourcesReceived?.Invoke(); // ВЫЗОВ СОБЫТИЯ
            
            // Если это не стройка, а производство, запускаем выдачу результата
            // Если это стройка, она сама вызовет FinishProcessing через событие
            // FinishProcessing(); // Можно закомментировать, если логикой рулит ConstructionSite
        }
    }

    public void FinishProcessing()
    {
        SpawnResult();
        _isProcessing = false;
        foreach (var req in requirements) req.currentAmount = 0;
        OnActionExecuted?.Invoke();
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (requestSpriteDisplay == null) return;

        var nextNeeded = requirements.FirstOrDefault(r => r.currentAmount < r.requiredAmount);
        bool shouldShow = !_isProcessing && nextNeeded != null && _reservedAmount == 0;
        
        if (shouldShow)
        {
            requestSpriteDisplay.sprite = nextNeeded.resourceType.defaultCarrySprite;
            requestSpriteDisplay.gameObject.SetActive(true);
        }
        else
        {
            requestSpriteDisplay.gameObject.SetActive(false);
        }
    }

    private void SpawnResult()
    {
        if (resultResourcePrefab == null) return;
        Vector3 randomPos = new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0);
        Vector3 targetPos = (spawnPoint != null ? spawnPoint.position : transform.position) + randomPos;
        GameObject newResource = Instantiate(resultResourcePrefab, transform.position, Quaternion.identity);
        newResource.tag = "Resource";
        StartCoroutine(TossResource(newResource.transform, transform.position, targetPos));
    }

    private IEnumerator TossResource(Transform resource, Vector3 start, Vector3 end)
    {
        float elapsed = 0;
        float duration = 0.6f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(start, end, percent);
            currentPos.y += Mathf.Sin(percent * Mathf.PI) * 1.0f;
            if (resource != null) resource.position = currentPos;
            yield return null;
        }
        if (resource != null) resource.position = end;
    }
}