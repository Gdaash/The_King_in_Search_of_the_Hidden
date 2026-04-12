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
    [SerializeField] private List<ResourceOutput> outputResources = new List<ResourceOutput>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpread = 0.5f;

    [Header("Визуал иконок")]
    [SerializeField] private GameObject iconPrefab; // Префаб с SpriteRenderer
    [SerializeField] private Transform iconsContainer; // Пустой объект-родитель для иконок
    [SerializeField] private float iconSpacing = 0.4f; // Расстояние между иконками
    [SerializeField] private float bobbingAmount = 0.1f;
    [SerializeField] private float bobbingSpeed = 2f;

    [Header("События")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnAllResourcesReceived;
    public UnityEvent OnActionExecuted;

    private List<GameObject> _activeIcons = new List<GameObject>();
    private int _carryingToUs = 0; 
    private bool _isProcessing = false;
    private Vector3 _containerBasePos;

    void OnEnable() { 
        AllRequesters.Add(this); 
        if (iconsContainer != null) _containerBasePos = iconsContainer.localPosition;
        UpdateIndicator(); 
    }
    
    void OnDisable() => AllRequesters.Remove(this);

    void Update() {
        // Анимация покачивания всего контейнера сразу
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

    private void CheckCompletion() {
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

    public void UpdateIndicator() {
        if (iconsContainer == null || iconPrefab == null) return;

        // Очищаем старые иконки
        foreach (var icon in _activeIcons) Destroy(icon);
        _activeIcons.Clear();

        if (_isProcessing) {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        // Собираем список всех ресурсов, которые ЕЩЕ НЕ несут физически
        // (То есть те, что нужны минус те, что уже в руках у носильщиков)
        List<ResourceType> displayTypes = new List<ResourceType>();
        int tempCarrying = _carryingToUs;

        foreach (var req in requirements) {
            int neededPhysically = req.requiredAmount - req.currentAmount;
            for (int i = 0; i < neededPhysically; i++) {
                if (tempCarrying > 0) {
                    tempCarrying--; // Пропускаем иконку, если ресурс уже "в пути" (в руках)
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

        // Создаем новые иконки и центрируем их
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