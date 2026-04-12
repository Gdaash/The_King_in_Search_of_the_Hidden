using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Linq;

public class ConstructionSite : MonoBehaviour
{
    [Header("Настройки строительства")]
    public GameObject finalBuildingPrefab;
    [Tooltip("Время в секундах, которое требуется на постройку после сбора всех ресурсов")]
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
    private bool _isBuilding = false;

    void Awake() 
    {
        _requester = GetComponent<ResourceRequester>();
    }

    void OnEnable()
    {
        if (isPlaced) SetupLogic();
    }

    public void SetupLogic()
    {
        if (!isPlaced || _requester == null) return;

        // Очищаем старые подписки, чтобы не было дубликатов
        _requester.OnResourceReceived.RemoveListener(HandleResourceReceived);
        _requester.OnAllResourcesReceived.RemoveListener(HandleAllResourcesCollected);

        // Подписываемся на события реквестера
        _requester.OnResourceReceived.AddListener(HandleResourceReceived);
        _requester.OnAllResourcesReceived.AddListener(HandleAllResourcesCollected);
        
        Debug.Log($"[ConstructionSite] {gameObject.name}: Логика стройки подключена.");
    }

    private void HandleResourceReceived()
    {
        // Просто пробрасываем событие дальше (например, для звука или UI шкалы)
        OnResourceAdded?.Invoke();
    }

    private void HandleAllResourcesCollected()
    {
        if (isResourcesReady) return;

        isResourcesReady = true;
        _isBuilding = true;
        
        OnConstructionStarted?.Invoke();
        Debug.Log("[ConstructionSite] Все ресурсы собраны, начало строительства...");
        
        // Запускаем процесс стройки
        StartCoroutine(ConstructionProcess());
    }

    private IEnumerator ConstructionProcess()
    {
        _constructionTimer = 0f;

        while (_constructionTimer < buildTime)
        {
            _constructionTimer += Time.deltaTime;
            // Здесь можно обновлять визуальную шкалу прогресса (fillAmount)
            yield return null;
        }

        FinishBuilding();
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

        // Удаляем площадку, так как здание создано
        Destroy(gameObject);
    }

    // Вспомогательный метод для получения текущего прогресса (от 0 до 1)
    public float GetBuildProgress()
    {
        if (buildTime <= 0) return 1f;
        return Mathf.Clamp01(_constructionTimer / buildTime);
    }
}