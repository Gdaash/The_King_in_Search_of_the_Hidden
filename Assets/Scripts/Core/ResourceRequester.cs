using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ResourceRequirement {
    public ResourceType resourceType;
    public int requiredAmount;
    [HideInInspector] public int currentAmount = 0;
}

[System.Serializable]
public class ResourceOutput {
    public GameObject prefab;
    public int count = 1;
}

public class ResourceRequester : MonoBehaviour {
    public static List<ResourceRequester> AllRequesters = new List<ResourceRequester>();

    [Header("Настройки рецепта")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
    public int priority = 1;

    [Header("Выходные ресурсы")]
    [SerializeField] private List<ResourceOutput> outputResources = new List<ResourceOutput>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpread = 0.5f;

    [Header("Визуал")]
    [SerializeField] private SpriteRenderer requestSpriteDisplay;

    [Header("События")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived;
    public UnityEvent OnActionExecuted;

    private int _reservedSpots = 0; // Кто уже идет к нам (даже если еще не взял ресурс)
    private int _carryingToUs = 0; // Кто уже несет ресурс в руках
    private bool _isProcessing = false;

    void OnEnable() { AllRequesters.Add(this); UpdateIndicator(); }
    void OnDisable() => AllRequesters.Remove(this);

    public bool NeedsResource() {
        if (_isProcessing) return false;
        int totalCurrent = requirements.Sum(r => r.currentAmount);
        int totalRequired = requirements.Sum(r => r.requiredAmount);
        // Здание занято, если (внутри + в пути) >= нужно
        return (totalCurrent + _reservedSpots) < totalRequired;
    }

    public void ReserveSpot() { _reservedSpots++; UpdateIndicator(); }
    public void UnreserveSpot() { _reservedSpots = Mathf.Max(0, _reservedSpots - 1); UpdateIndicator(); }
    
    // Носильщик сообщает, что подобрал ресурс и теперь реально его несет
    public void StartPhysicalDelivery() { _carryingToUs++; UpdateIndicator(); }

    public void DeliverResource(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null && req.currentAmount < req.requiredAmount) {
            req.currentAmount++;
            _reservedSpots = Mathf.Max(0, _reservedSpots - 1);
            _carryingToUs = Mathf.Max(0, _carryingToUs - 1);
            OnResourceReceived?.Invoke();
            CheckCompletion();
            UpdateIndicator();
        }
    }

    private void CheckCompletion() {
        if (requirements.All(r => r.currentAmount >= r.requiredAmount)) {
            _isProcessing = true;
            OnAllResourcesReceived?.Invoke();
        }
    }

    public void FinishProcessing() {
        SpawnAllResults();
        _isProcessing = false;
        _reservedSpots = 0;
        _carryingToUs = 0;
        foreach (var req in requirements) req.currentAmount = 0;
        OnActionExecuted?.Invoke();
        UpdateIndicator();
    }

    public void UpdateIndicator() {
        if (requestSpriteDisplay == null) return;
        var nextNeeded = requirements.FirstOrDefault(r => r.currentAmount < r.requiredAmount);
        // Иконка гаснет, только если ресурс уже физически в руках у носильщика или здание полно
        bool shouldShow = !_isProcessing && nextNeeded != null && _carryingToUs == 0;
        if (requestSpriteDisplay.gameObject.activeSelf != shouldShow)
            requestSpriteDisplay.gameObject.SetActive(shouldShow);
        if (shouldShow) requestSpriteDisplay.sprite = nextNeeded.resourceType.defaultCarrySprite;
    }

    private void SpawnAllResults() {
        foreach (var output in outputResources) {
            if (output.prefab == null) continue;
            for (int i = 0; i < output.count; i++) {
                Vector3 randomPos = new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0);
                Vector3 targetPos = (spawnPoint != null ? spawnPoint.position : transform.position) + randomPos;
                GameObject res = Instantiate(output.prefab, transform.position, Quaternion.identity);
                res.tag = "Resource";
                StartCoroutine(TossResource(res.transform, transform.position, targetPos));
            }
        }
    }

    private IEnumerator TossResource(Transform tr, Vector3 start, Vector3 end) {
        float elapsed = 0;
        while (elapsed < 0.6f) {
            elapsed += Time.deltaTime;
            float p = elapsed / 0.6f;
            Vector3 pos = Vector3.Lerp(start, end, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 1.0f;
            if (tr != null) tr.position = pos;
            yield return null;
        }
        if (tr != null) tr.position = end;
    }
}