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
    [HideInInspector] public int reservedAmount = 0; 
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
    [SerializeField] protected List<ResourceOutput> outputResources = new List<ResourceOutput>();
    [SerializeField] protected Transform spawnPoint;
    [SerializeField] protected float spawnSpread = 0.5f;

    [Header("Визуал иконок")]
    [SerializeField] protected GameObject iconPrefab; 
    [SerializeField] protected Transform iconsContainer; 
    [SerializeField] protected float iconSpacing = 0.4f; 
    [SerializeField] protected float bobbingAmount = 0.1f;
    [SerializeField] protected float bobbingSpeed = 2f;

    [Header("События")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived;
    public UnityEvent OnActionExecuted;

    // Изменено на protected, чтобы ClickRequester имел доступ
    protected List<GameObject> _activeIcons = new List<GameObject>();
    protected int _carryingToUs = 0; 
    protected bool _isProcessing = false;
    protected Vector3 _containerBasePos;

    protected virtual void OnEnable() { 
        AllRequesters.Add(this); 
        if (iconsContainer != null) _containerBasePos = iconsContainer.localPosition;
        UpdateIndicator(); 
    }
    
    protected virtual void OnDisable() => AllRequesters.Remove(this);

    protected virtual void Update() {
        if (iconsContainer != null && iconsContainer.gameObject.activeSelf) {
            float newY = _containerBasePos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            iconsContainer.localPosition = new Vector3(_containerBasePos.x, newY, _containerBasePos.z);
        }
    }

    public bool NeedsSpecificResource(ResourceType type) {
        if (_isProcessing) return false;
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req == null) return false;
        return (req.currentAmount + req.reservedAmount) < req.requiredAmount;
    }

    public bool NeedsAnyResource() {
        if (_isProcessing) return false;
        return requirements.Any(r => (r.currentAmount + r.reservedAmount) < r.requiredAmount);
    }

    public void ReserveResource(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null) {
            req.reservedAmount++;
            UpdateIndicator();
        }
    }

    public void UnreserveResource(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null) {
            req.reservedAmount = Mathf.Max(0, req.reservedAmount - 1);
            UpdateIndicator();
        }
    }
    
    public void StartPhysicalDelivery() { 
        _carryingToUs++; 
        UpdateIndicator(); 
    }

    public void DeliverResource(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null && req.currentAmount < req.requiredAmount) {
            req.currentAmount++;
            req.reservedAmount = Mathf.Max(0, req.reservedAmount - 1);
            _carryingToUs = Mathf.Max(0, _carryingToUs - 1);
            
            OnResourceReceived?.Invoke();
            CheckCompletion();
            UpdateIndicator();
        }
    }

    protected void CheckCompletion() {
        if (requirements.All(r => r.currentAmount >= r.requiredAmount)) {
            _isProcessing = true;
            OnAllResourcesReceived?.Invoke();
        }
    }

    public void FinishProcessing() {
        SpawnAllResults();
        _isProcessing = false;
        _carryingToUs = 0;
        foreach (var req in requirements) {
            req.currentAmount = 0;
            req.reservedAmount = 0;
        }
        OnActionExecuted?.Invoke();
        UpdateIndicator();
    }

    // Сделан virtual, чтобы ClickRequester мог его переписать под иконку мыши
    public virtual void UpdateIndicator() {
        if (iconsContainer == null || iconPrefab == null) return;

        foreach (var icon in _activeIcons) Destroy(icon);
        _activeIcons.Clear();

        if (_isProcessing) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        List<ResourceType> displayTypes = new List<ResourceType>();
        int tempCarrying = _carryingToUs;

        foreach (var req in requirements) {
            int neededPhysically = req.requiredAmount - req.currentAmount;
            for (int i = 0; i < neededPhysically; i++) {
                if (tempCarrying > 0) {
                    tempCarrying--; 
                } else {
                    displayTypes.Add(req.resourceType);
                }
            }
        }

        if (displayTypes.Count == 0) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        iconsContainer.gameObject.SetActive(true);

        float totalWidth = (displayTypes.Count - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < displayTypes.Count; i++) {
            GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
            newIcon.transform.localPosition = new Vector3(startX + (i * iconSpacing), 0, 0);
            
            if (newIcon.TryGetComponent(out SpriteRenderer sr)) {
                sr.sprite = displayTypes[i].defaultCarrySprite;
            }
            _activeIcons.Add(newIcon);
        }
    }

    private void SpawnAllResults() {
        foreach (var output in outputResources) {
            if (output.prefab == null) continue;
            for (int i = 0; i < output.count; i++) {
                Vector3 randomPos = new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0);
                Vector3 origin = transform.position;
                Vector3 targetPos = (spawnPoint != null ? spawnPoint.position : origin) + randomPos;
                GameObject res = Instantiate(output.prefab, origin, Quaternion.identity);
                res.tag = "Resource";
                StartCoroutine(TossResource(res.transform, origin, targetPos));
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