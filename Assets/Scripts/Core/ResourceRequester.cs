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
    public static readonly HashSet<ResourceRequester> ActiveInstances = new();
    [Header("Настройки ограничений")]
    [SerializeField] private int maxProductionPool = 3;  
    [SerializeField] private GameObject storageFullVisual; 
    [Tooltip("Максимальное количество циклов производства (0 = без ограничений)")]
    [SerializeField] private int maxProductionCycles = 0;

    [Header("Настройки флажка")]
    [SerializeField] protected bool ignoreFlag = false; 
    [Header("Рабочие внутри здания")]
    [Tooltip("Сохранять доставленных людей между производственными циклами.")]
    [SerializeField] private bool retainHumansBetweenCycles;
    [Tooltip("После завершения строительства передать людей этому рабочему зданию.")]
    [SerializeField] private ResourceRequester humanSuccessor;

    [Header("Настройки рецепта")]
    public int priority = 1;
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();

    [Header("Выходные ресурсы")]
    [SerializeField] protected List<ResourceOutput> outputResources = new List<ResourceOutput>();
    [SerializeField] protected Transform spawnPoint;
    [SerializeField] protected float spawnSpread = 0.5f;

    [Header("Визуал иконок")]
    [Tooltip("Обычная иконка ресурса")]
    [SerializeField] protected GameObject iconPrefab; 
    [Tooltip("Иконка 'в пути'")]
    [SerializeField] protected GameObject inTransitIconPrefab;
    [SerializeField] protected Transform iconsContainer; 
    [SerializeField] protected float iconSpacing = 0.4f; 
    [SerializeField] protected float bobbingAmount = 0.1f;
    [SerializeField] protected float bobbingSpeed = 2f;
    [Tooltip("Смещение иконок требований во время паузы: оставляет место для видимого прогрессбара.")]
    [SerializeField] private Vector3 pausedRequestOffset = new Vector3(0, 1.1f, 0);

    [Header("Общие события")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived;
    public UnityEvent OnActionExecuted;
    
    [Header("События склада")]
    public UnityEvent<bool> OnStorageFullChanged; 

    protected List<GameObject> _activeIcons = new List<GameObject>();
    protected int _carryingToUs = 0; 
    protected bool _isProcessing = false;
    protected Vector3 _containerBasePos;
    
    private Collider2D _myCollider;
    private float _lastValidationTime;
    private bool _lastFlagState;
    
    private List<GameObject> _spawnedResources = new List<GameObject>();
    private bool _wasFull; 
    protected float _bobbingOffset;
    
    private int _completedCycles = 0;
    private bool _crystalControlled;
    private LogisticFlag _crystalFlag;
    private bool _crystalCyclePaused;
    public bool IsProcessing => _isProcessing;
    public bool IsCyclePaused => _isProcessing && _crystalCyclePaused;
    public bool CanRecallHumans => isActiveAndEnabled && (!_isProcessing || (IsCyclePaused && !HasLogisticFlag())) &&
        requirements.Any(r => r.resourceType != null && r.resourceType.isHumanResource && r.currentAmount > 0);
    public int RecallIdleHumans()
    {
        if (!CanRecallHumans || Warehouse.Instance == null) return 0;
        int sent = 0;
        foreach (var type in requirements.Where(r => r.resourceType != null && r.resourceType.isHumanResource)
                     .Select(r => r.resourceType).Distinct())
            sent += SendHumansHome(type, Warehouse.Instance);
        return sent;
    }
    public bool CrystalResourcesReady => requirements.All(r => r.currentAmount >= r.requiredAmount);
    public bool CanSelectCrystalCycle => enabled && gameObject.activeInHierarchy && !ignoreFlag &&
        requirements.Count > 0 && (!_isProcessing || IsCyclePaused) && !IsStorageFull() &&
        (maxProductionCycles == 0 || _completedCycles < maxProductionCycles);
    public void SetCrystalFlag(LogisticFlag flag)
    {
        _crystalControlled = true;
        _crystalFlag = flag;
        if (flag == null)
        {
            if (_isProcessing)
            {
                _crystalCyclePaused = true;
                foreach (var timer in GetComponentsInChildren<TimerController>(true))
                    if (timer.CrystalOwner == this) timer.PauseCrystalCycle();
            }
            OrderManager.Instance?.CancelDeliveries(this);
        }
        UpdateIndicator();
    }
    public void TryStartCrystalCycle() => CheckCompletion();

    protected virtual void Awake() { 
        if (iconsContainer != null) _containerBasePos = iconsContainer.localPosition;
        _myCollider = GetComponent<Collider2D>();
        if (storageFullVisual != null) storageFullVisual.SetActive(false);
        _bobbingOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    protected virtual void OnEnable() {
        ActiveInstances.Add(this);
        _lastFlagState = HasLogisticFlag();
        _wasFull = IsStorageFull(); 
        UpdateIndicator(); 
        if (OrderManager.Instance != null) OrderManager.Instance.RegisterRequester(this);
    }

    protected virtual void OnDisable() {
        ActiveInstances.Remove(this);
        if (OrderManager.Instance != null) OrderManager.Instance.UnregisterRequester(this);
    }

    protected virtual void Update() {
        if (iconsContainer != null && iconsContainer.gameObject.activeSelf) {
            Vector3 origin = _containerBasePos + (IsCyclePaused ? pausedRequestOffset : Vector3.zero);
            origin.y += Mathf.Sin((Time.time * bobbingSpeed) + _bobbingOffset) * bobbingAmount;
            iconsContainer.localPosition = origin;
        }

        UpdateStorageStatus();

        bool currentFlag = HasLogisticFlag();
        if (currentFlag != _lastFlagState) {
            _lastFlagState = currentFlag;
            UpdateIndicator(); 
            if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
        }

        if (Time.time > _lastValidationTime + 2f) {
            ValidateReservations();
            _lastValidationTime = Time.time;
        }
    }

    public virtual bool IsStorageFull() {
        _spawnedResources.RemoveAll(item => item == null);
        int activeCount = _spawnedResources.Count(obj => obj.activeInHierarchy);
        return activeCount >= maxProductionPool;
    }

    private void UpdateStorageStatus() {
        bool currentlyFull = IsStorageFull();

        if (_wasFull && !currentlyFull) {
            _wasFull = false;
            if (storageFullVisual != null) storageFullVisual.SetActive(false);
            OnStorageFullChanged?.Invoke(false); 
            UpdateIndicator(); 
            if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
        } 
        else if (!_wasFull && currentlyFull) {
            _wasFull = true;
            if (storageFullVisual != null) storageFullVisual.SetActive(true);
            OnStorageFullChanged?.Invoke(true); 
            UpdateIndicator(); 
        }
    }

    public bool HasLogisticFlag() {
        if (_crystalControlled) return _crystalFlag != null && _crystalFlag.isActiveAndEnabled && _crystalFlag.CrystalTarget == this;
        if (ignoreFlag) return true;
        if (_myCollider == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true; 
        List<Collider2D> results = new List<Collider2D>();
        
        int count = _myCollider.Overlap(filter, results);
        for (int i = 0; i < count; i++) {
            if (results[i] != null && results[i].TryGetComponent<LogisticFlag>(out var flag) &&
                (!flag.CrystalControlled || flag.CrystalTarget == this)) return true;
        }
        return false;
    }

    public bool NeedsAnyResource() {
        if (maxProductionCycles > 0 && _completedCycles >= maxProductionCycles) {
            return false;
        }
        
        if (!gameObject.activeInHierarchy || (_isProcessing && !IsCyclePaused) || !HasLogisticFlag() || IsStorageFull()) return false;
        return requirements.Any(r => (r.currentAmount + r.reservedAmount) < r.requiredAmount);
    }

    public void ReserveResource(ResourceType type) {
        if (IsStorageFull()) return;
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

    public void StartPhysicalDelivery() { 
        if (IsStorageFull()) return;
        _carryingToUs++; 
        UpdateIndicator(); 
    }

    public void DeliverResource(ResourceType type) {
        var req = requirements.FirstOrDefault(r => r.resourceType == type);
        if (req != null && req.currentAmount < req.requiredAmount) {
            req.currentAmount++;
            req.reservedAmount = Mathf.Max(0, req.reservedAmount - 1);
            _carryingToUs = Mathf.Max(0, _carryingToUs - 1);
            
            // A replacement worker continues a paid cycle; delivery hooks must not restart it.
            if (!_isProcessing)
            {
                OnResourceReceived?.Invoke();
                req.OnOneUnitDelivered?.Invoke();
                if (req.currentAmount >= req.requiredAmount) req.OnAllUnitsDelivered?.Invoke();
            }

            CheckCompletion();
            UpdateIndicator();
        }
    }

    public int SendHumansHome(ResourceType humanType, Warehouse warehouse)
    {
        if (humanType == null || warehouse == null) return 0;

        int sent = 0;
        foreach (var requirement in requirements)
        {
            if (requirement.resourceType != humanType) continue;
            while (requirement.currentAmount > 0 && warehouse.SendHumanHomeFrom(transform.position))
            {
                requirement.currentAmount--;
                sent++;
            }
            requirement.reservedAmount = 0;
        }

        if (sent > 0) UpdateIndicator();
        return sent;
    }

    protected virtual void CheckCompletion() {
        if (_crystalControlled && IsCyclePaused)
        {
            if (!HasLogisticFlag() || !CrystalResourcesReady) return;
            _crystalCyclePaused = false;
            foreach (var timer in GetComponentsInChildren<TimerController>(true))
                if (timer.CrystalOwner == this) timer.ResumeCrystalCycle();
            UpdateIndicator();
            return;
        }
        if (_crystalControlled && (_isProcessing || !HasLogisticFlag() || !CanSelectCrystalCycle)) return;
        if (requirements.All(r => r.currentAmount >= r.requiredAmount)) {
            _isProcessing = true;
            OnAllResourcesReceived?.Invoke();
            if (_crystalControlled)
                foreach (var timer in GetComponentsInChildren<TimerController>(true))
                    if (timer.CrystalOwner == this) timer.ResumeCrystalCycle();
        }
    }

    public virtual void FinishProcessing() {
        if (humanSuccessor != null)
        {
            foreach (var req in requirements)
            {
                if (req.resourceType == null || !req.resourceType.isHumanResource) continue;
                var next = humanSuccessor.requirements.FirstOrDefault(r => r.resourceType == req.resourceType);
                if (next == null) continue;
                int count = Mathf.Min(req.currentAmount, Mathf.Max(0, next.requiredAmount - next.currentAmount));
                next.currentAmount += count;
                req.currentAmount -= count;
            }
            humanSuccessor.UpdateIndicator();
        }
        SpawnAllResults();
        _isProcessing = false;
        _crystalCyclePaused = false;
        _carryingToUs = 0;
        foreach (var req in requirements) {
            if (!retainHumansBetweenCycles || req.resourceType == null || !req.resourceType.isHumanResource)
                req.currentAmount = 0;
            req.reservedAmount = 0;
        }
        
        _completedCycles++;
        
        if (maxProductionCycles > 0 && _completedCycles >= maxProductionCycles) {
            Debug.Log($"[ResourceRequester] Достигнут лимит циклов: {_completedCycles}/{maxProductionCycles}");
        }
        
        OnActionExecuted?.Invoke();
        UpdateIndicator();
    }

    public virtual void UpdateIndicator() {
        if (iconsContainer == null) return;
        
        foreach (var icon in _activeIcons) if(icon) Destroy(icon);
        _activeIcons.Clear();

        if ((_isProcessing && !IsCyclePaused) || !gameObject.activeInHierarchy || IsStorageFull()) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        if (maxProductionCycles > 0 && _completedCycles >= maxProductionCycles) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        List<ResourceType> resourcesInTransit = new List<ResourceType>();
        
        var porters = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None);
        foreach (var p in porters) {
            if (p.GetCurrentJob() == this && p.IsCarryingResource()) {
                resourcesInTransit.Add(p.GetCarriedResourceType());
            }
        }
        
        if (OrderManager.Instance != null) {
            var humans = OrderManager.Instance.GetHumanUnitsForRequester(this);
            foreach (var h in humans) {
                resourcesInTransit.Add(h.GetCarriedResourceType());
            }
        }

        List<(ResourceType type, bool isInTransit)> displayIcons = new List<(ResourceType, bool)>();
        
        foreach (var req in requirements) {
            int needed = req.requiredAmount - req.currentAmount;
            for (int i = 0; i < needed; i++) {
                if (resourcesInTransit.Contains(req.resourceType)) {
                    displayIcons.Add((req.resourceType, true));
                    resourcesInTransit.Remove(req.resourceType);
                } else {
                    displayIcons.Add((req.resourceType, false));
                }
            }
        }

        if (displayIcons.Count == 0) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        iconsContainer.gameObject.SetActive(true);
        iconsContainer.localPosition = _containerBasePos + (IsCyclePaused ? pausedRequestOffset : Vector3.zero);
        float totalWidth = (displayIcons.Count - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < displayIcons.Count; i++) {
            var (type, isInTransit) = displayIcons[i];
            
            GameObject prefabToUse = isInTransit ? inTransitIconPrefab : iconPrefab;
            if (prefabToUse == null) prefabToUse = iconPrefab;
            if (prefabToUse == null) continue;
            
            GameObject newIcon = Instantiate(prefabToUse, iconsContainer);
            newIcon.transform.localPosition = new Vector3(startX + (i * iconSpacing), 0, 0);
            
            if (newIcon.TryGetComponent(out SpriteRenderer sr)) {
                sr.sprite = type.resourceIcon;
            }
            
            _activeIcons.Add(newIcon);
        }
    }

    private void ValidateReservations() {
        if ((_isProcessing && !IsCyclePaused) || !gameObject.activeInHierarchy || IsStorageFull()) return;

        if (!HasLogisticFlag()) {
            bool changed = false;
            foreach(var req in requirements) {
                if(req.reservedAmount > 0) { req.reservedAmount = 0; changed = true; }
            }
            if(changed) { _carryingToUs = 0; UpdateIndicator(); }
            return;
        }

        // === СЧИТАЕМ PORTER'ОВ ===
        var validPorters = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)
            .Where(p => p.GetCurrentJob() == this && p.GetTarget() != null)
            .ToList();

        // === СЧИТАЕМ HUMAN UNIT'ОВ ===
        var validHumans = new List<HumanUnit>();
        if (OrderManager.Instance != null)
        {
            validHumans = OrderManager.Instance.GetHumanUnitsForRequester(this);
        }

        bool needsUpdate = false;
        
        foreach (var req in requirements)
        {
            int actualCarriers = 0;
            
            if (req.resourceType.isHumanResource)
            {
                // Для людей считаем HumanUnit'ов
                actualCarriers = validHumans.Count(h => h.GetCarriedResourceType() == req.resourceType);
            }
            else
            {
                // Для обычных ресурсов считаем Porter'ов
                actualCarriers = validPorters.Count(p => p.GetCarriedResourceType() == req.resourceType);
            }

            if (req.reservedAmount > actualCarriers)
            {
                req.reservedAmount = actualCarriers;
                needsUpdate = true;
            }
        }
        
        if (needsUpdate) { 
            _carryingToUs = validPorters.Count + validHumans.Count;
            UpdateIndicator(); 
            if (OrderManager.Instance != null) OrderManager.Instance.ForceUpdateOrders();
        }
    }

    protected void SpawnAllResults() {
        foreach (var output in outputResources) {
            if (output.prefab == null) continue;
            for (int i = 0; i < output.count; i++) {
                Vector3 origin = transform.position;
                Vector3 spawnTarget = (spawnPoint != null ? spawnPoint.position : origin) + new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0);
                GameObject res = Instantiate(output.prefab, origin, Quaternion.identity);
                if (res.CompareTag("Untagged")) res.tag = "Resource";
                _spawnedResources.Add(res);
                StartCoroutine(TossResource(res.transform, origin, spawnTarget));
            }
        }
    }

    protected IEnumerator TossResource(Transform tr, Vector3 start, Vector3 end) {
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
