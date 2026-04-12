using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class ResourceRequester : MonoBehaviour
{
    public static List<ResourceRequester> AllRequesters = new List<ResourceRequester>();

    [Header("Настройки запроса")]
    public ResourceType neededType;
    public int priority = 1;
    public int capacity = 1;

    [Header("Выходной ресурс (Результат)")]
    [SerializeField] private GameObject resultResourcePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpread = 0.5f;

    [Header("Настройки анимации вылета")]
    [SerializeField] private float tossArcHeight = 1.0f;
    [SerializeField] private float tossDuration = 0.6f;

    [Header("Визуал иконки запроса")]
    [SerializeField] private SpriteRenderer requestSpriteDisplay;
    [SerializeField] private float bobbingAmount = 0.1f;
    [SerializeField] private float bobbingSpeed = 2f;

    [Header("События")]
    public UnityEvent OnResourceReceived;
    public UnityEvent OnActionExecuted;

    private int _currentAmount = 0;
    private int _reservedAmount = 0;
    private bool _isProcessing = false;
    private Vector3 _originalIconPos;

    // Важно: OnDisable срабатывает и при Destroy, и при SetActive(false)
    void OnEnable() => AllRequesters.Add(this);
    void OnDisable() => AllRequesters.Remove(this);

    void Start()
    {
        if (requestSpriteDisplay != null)
        {
            _originalIconPos = requestSpriteDisplay.transform.localPosition;
            if (neededType != null) requestSpriteDisplay.sprite = neededType.defaultCarrySprite;
        }
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

    public bool NeedsResource() => !_isProcessing && (_currentAmount + _reservedAmount) < capacity;

    public void ReserveSpot() 
    { 
        _reservedAmount++;
        UpdateIndicator();
    }

    // Новый метод: отмена брони, если носильщик погиб или передумал
    public void UnreserveSpot()
    {
        _reservedAmount = Mathf.Max(0, _reservedAmount - 1);
        UpdateIndicator();
    }

    public void DeliverResource(ResourceType type)
    {
        if (type == neededType)
        {
            _currentAmount++;
            _reservedAmount = Mathf.Max(0, _reservedAmount - 1);
            if (_currentAmount >= capacity) _isProcessing = true;
            OnResourceReceived?.Invoke();
            UpdateIndicator();
        }
    }

    public void FinishProcessing()
    {
        SpawnResult();
        _isProcessing = false;
        _currentAmount = 0;
        OnActionExecuted?.Invoke();
        UpdateIndicator();
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
        Collider2D col = resource.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / tossDuration;
            Vector3 currentPos = Vector3.Lerp(start, end, percent);
            float arc = Mathf.Sin(percent * Mathf.PI) * tossArcHeight;
            currentPos.y += arc;
            if (resource != null)
            {
                resource.position = currentPos;
                resource.Rotate(0, 0, Time.deltaTime * 360f);
            }
            yield return null;
        }

        if (resource != null)
        {
            resource.position = end;
            resource.rotation = Quaternion.identity;
            if (col != null) col.enabled = true;
        }
    }

    private void UpdateIndicator()
    {
        if (requestSpriteDisplay == null) return;
        bool shouldShow = !_isProcessing && _currentAmount < capacity && _reservedAmount == 0;
        requestSpriteDisplay.gameObject.SetActive(shouldShow);
    }
}