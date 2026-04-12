using UnityEngine;
using UnityEngine.Events;

public class ConstructionSite : MonoBehaviour
{
    [Header("Настройки строительства")]
    public GameObject finalBuildingPrefab;
    public float buildTime = 3f;

    [Header("Состояние")]
    public bool isResourcesReady = false;
    public bool isConstructed = false;
    [HideInInspector] public bool isPlaced = false; 

    [Header("События")]
    public UnityEvent OnResourceAdded;
    public UnityEvent OnConstructionStarted;
    public UnityEvent OnConstructionFinished;

    private ResourceRequester _requester;
    private float _constructionTimer = 0f;

    void Awake() => _requester = GetComponent<ResourceRequester>();

    void OnEnable() 
    { 
        if (isPlaced) SetupLogic(); 
    }

    public void SetupLogic()
    {
        if (_requester == null) _requester = GetComponent<ResourceRequester>();
        if (_requester == null) return;

        isPlaced = true;

        _requester.OnResourceReceived.RemoveListener(HandleResourceReceived);
        _requester.OnAllResourcesReceived.RemoveListener(HandleAllResourcesCollected);
        
        _requester.OnResourceReceived.AddListener(HandleResourceReceived);
        _requester.OnAllResourcesReceived.AddListener(HandleAllResourcesCollected);
        
        // ИСПРАВЛЕНО: Используем NeedsAnyResource вместо NeedsResource
        if (!_requester.NeedsAnyResource())
        {
            HandleAllResourcesCollected();
        }
    }

    private void HandleResourceReceived()
    {
        OnResourceAdded?.Invoke();
    }

    private void HandleAllResourcesCollected()
    {
        if (isResourcesReady) return;
        
        isResourcesReady = true;
        Debug.Log($"[ConstructionSite] {gameObject.name}: Ресурсы собраны, ждем строителя.");
    }

    public void AdvanceConstruction(float amount)
    {
        if (!isResourcesReady || isConstructed) return;

        if (_constructionTimer == 0 && amount > 0) 
        {
            OnConstructionStarted?.Invoke();
        }

        _constructionTimer += amount;

        if (_constructionTimer >= buildTime)
        {
            FinishBuilding();
        }
    }

    public void FinishBuilding()
    {
        if (isConstructed) return;
        isConstructed = true;

        OnConstructionFinished?.Invoke();

        if (finalBuildingPrefab != null) 
        {
            Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        }

        if (_requester != null) _requester.FinishProcessing();

        Destroy(gameObject);
    }

    public float GetBuildProgress()
    {
        if (buildTime <= 0) return 1f;
        return Mathf.Clamp01(_constructionTimer / buildTime);
    }
}