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

    [Header("События конкретного ресурса")]
    public UnityEvent OnOneUnitDelivered;
    public UnityEvent OnAllUnitsDelivered;
}

[System.Serializable]
public class ResourceOutput {
    public GameObject prefab;
    public int count = 1;
}

public class ResourceRequester : MonoBehaviour {
    [Header("Настройки паузы")]
    [SerializeField] private SpriteRenderer pauseButtonRenderer;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private bool _isPaused = false;

    [Header("Настройки флажка")]
    [SerializeField] private bool ignoreFlag = false; 

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

    [Header("Общие события")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived;
    public UnityEvent OnActionExecuted;

    protected List<GameObject> _activeIcons = new List<GameObject>();
    protected int _carryingToUs = 0; 
    protected bool _isProcessing = false;
    protected Vector3 _containerBasePos;
    
    private float _lastValidationTime;
    private float _flagActiveTimer = 0f; // Таймер форы для носильщиков
    private BoxCollider2D _myCollider;

    protected virtual void Awake() { 
        if (iconsContainer != null) _containerBasePos = iconsContainer.localPosition;
        _myCollider = GetComponent<BoxCollider2D>();
    }

    protected virtual void OnEnable() {
        UpdateIndicator();
        UpdatePauseVisual();
    }

    protected virtual void Update() {
        if (iconsContainer != null && iconsContainer.gameObject.activeSelf) {
            float newY = _containerBasePos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            iconsContainer.localPosition = new Vector3(_containerBasePos.x, newY, _containerBasePos.z);
        }

        if (Time.time > _lastValidationTime + 2f) {
            ValidateReservations();
            _lastValidationTime = Time.time;
        }
    }

    public bool HasLogisticFlag() {
        if (ignoreFlag) return true;
        if (_myCollider == null) return false;

        Vector2 searchSize = Vector2.Scale(_myCollider.size, transform.localScale);
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(transform.position, searchSize, 0);
        foreach (var hit in hitColliders) {
            if (hit.GetComponent<LogisticFlag>() != null) return true;
        }
        return false;
    }

    public void TogglePause() {
        _isPaused = !_isPaused;
        UpdatePauseVisual();
        UpdateIndicator();
    }

    private void UpdatePauseVisual() {
        if (pauseButtonRenderer != null) {
            pauseButtonRenderer.sprite = _isPaused ? playSprite : pauseSprite;
        }
    }

    public bool NeedsSpecificResource(ResourceType type) {
        if (!gameObject.activeInHierarchy || _isProcessing || _isPaused || !HasLogisticFlag()) return false;
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req == null) return false;
        return (req.currentAmount + req.reservedAmount) < req.requiredAmount;
    }

    public bool NeedsAnyResource() {
        if (!gameObject.activeInHierarchy || _isProcessing || _isPaused || !HasLogisticFlag()) return false;
        return requirements.Any(r => (r.currentAmount + r.reservedAmount) < r.requiredAmount);
    }

    public void ReserveResource(ResourceType type) {
        if (_isPaused || !HasLogisticFlag()) return;
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null) {
            req.reservedAmount++;
            UpdateIndicator();
        }
    }

    public void ForceCancelReservation(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null) {
            req.reservedAmount = Mathf.Max(0, req.reservedAmount - 1);
            _carryingToUs = Mathf.Max(0, _carryingToUs - 1);
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
        if (_isPaused || !HasLogisticFlag()) return;
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
            req.OnOneUnitDelivered?.Invoke();
            if (req.currentAmount >= req.requiredAmount) req.OnAllUnitsDelivered?.Invoke();

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

    public virtual void UpdateIndicator() {
        if (iconsContainer == null || iconPrefab == null) return;
        foreach (var icon in _activeIcons) if(icon) Destroy(icon);
        _activeIcons.Clear();

        if (_isProcessing || !gameObject.activeInHierarchy) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        bool hasFlag = HasLogisticFlag();
        List<ResourceType> resourcesInTransit = new List<ResourceType>();
        
        if (hasFlag && !_isPaused) {
            resourcesInTransit = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)
                .Where(p => p.GetCurrentJob() == this && p.IsCarryingResource())
                .Select(p => p.GetCarriedResourceType())
                .ToList();
        }

        List<ResourceType> displayTypes = new List<ResourceType>();
        foreach (var req in requirements) {
            int stillNeededCount = req.requiredAmount - req.currentAmount;
            for (int i = 0; i < stillNeededCount; i++) {
                if (resourcesInTransit.Contains(req.resourceType)) {
                    resourcesInTransit.Remove(req.resourceType);
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
            if (newIcon.TryGetComponent(out SpriteRenderer sr)) sr.sprite = displayTypes[i].defaultCarrySprite;
            _activeIcons.Add(newIcon);
        }
    }

    private void ValidateReservations() {
        if (_isProcessing || !gameObject.activeInHierarchy) return;

        bool hasFlag = HasLogisticFlag();

        if (!hasFlag || _isPaused) {
            _flagActiveTimer = 0f; // Сброс таймера форы
            bool changed = false;
            foreach(var req in requirements) {
                if(req.reservedAmount > 0) { req.reservedAmount = 0; changed = true; }
            }
            if(changed) { _carryingToUs = 0; UpdateIndicator(); }
            return;
        }

        // --- ЛОГИКА ФОРЫ ---
        // Даем носильщикам 4 секунды (2 тика по 2 сек), чтобы занять здание после установки флага
        _flagActiveTimer += 2f; 
        if (_flagActiveTimer < 4f) return; 

        int actualCarriers = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)
            .Count(p => p.GetCurrentJob() == this);

        bool needsUpdate = false;
        foreach (var req in requirements) {
            if (req.reservedAmount > actualCarriers) {
                req.reservedAmount = actualCarriers;
                needsUpdate = true;
            }
        }

        if (needsUpdate) {
            _carryingToUs = actualCarriers;
            UpdateIndicator();
        }
    }

    private void SpawnAllResults() {
        foreach (var output in outputResources) {
            if (output.prefab == null) continue;
            for (int i = 0; i < output.count; i++) {
                Vector3 origin = transform.position;
                Vector3 spawnTarget = (spawnPoint != null ? spawnPoint.position : origin) + new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0);
                GameObject res = Instantiate(output.prefab, origin, Quaternion.identity);
                if (res.CompareTag("Untagged")) res.tag = "Resource";
                StartCoroutine(TossResource(res.transform, origin, spawnTarget));
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