using UnityEngine;
using UnityEngine.Events;

public class ConstructionSite : MonoBehaviour
{
    [Header("Настройки строительства")]
    public GameObject finalBuildingPrefab;
    public float buildTime = 3f;

    [Header("Состояние")]
    public int currentResources = 0;
    public bool isResourcesReady = false;
    public bool isConstructed = false;
    [HideInInspector] public bool isPlaced = false; 

    [Header("События")]
    public UnityEvent OnResourceAdded;
    public UnityEvent OnConstructionStarted;
    public UnityEvent OnConstructionFinished;

    private ResourceRequester _requester;

    void Awake() 
    {
        // Ищем заранее настроенный в инспекторе компонент
        _requester = GetComponent<ResourceRequester>();
    }

    void OnEnable()
    {
        if (isPlaced) SetupLogic();
    }

    public void SetupLogic()
    {
        if (!isPlaced || _requester == null) return;

        // Подписываемся на уже существующий и настроенный компонент
        _requester.OnResourceReceived.RemoveListener(HandleResourceReceived); // Защита от дублей
        _requester.OnResourceReceived.AddListener(HandleResourceReceived);
        
        Debug.Log("[ConstructionSite] Логика стройки подключена к ручному ResourceRequester.");
    }

    private void HandleResourceReceived()
    {
        currentResources++;
        OnResourceAdded?.Invoke();

        // Берем данные о нужном количестве прямо из настроек ResourceRequester
        if (currentResources >= _requester.capacity)
        {
            isResourcesReady = true;
            Debug.Log("[ConstructionSite] Ресурсы собраны!");
        }
    }

    public void FinishBuilding()
    {
        if (isConstructed) return;
        isConstructed = true;

        OnConstructionFinished?.Invoke();

        if (finalBuildingPrefab != null) 
            Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}