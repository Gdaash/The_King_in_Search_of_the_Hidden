using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Porter : MonoBehaviour, IEnemyAI 
{
    [Header("Настройки")]
    [SerializeField] private string resourceTag = "Resource";
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private SpriteRenderer carrySlotRenderer;

    private EnemyMovement _movement;
    private Transform _currentTarget;
    private ResourceType _targetResourceType;
    private ResourceItem _carriedResourceItem;
    private ResourceItem _targetedResourceItem; 
    private bool _hasResourceInHands = false;
    private ResourceRequester _currentJob;

    // --- Реализация интерфейса IEnemyAI ---
    public Transform GetTarget() => _currentTarget;
    
    public void FinishAttack() { } // Носильщик не атакует

    public void OnTakeDamage(Transform attacker) { } // Заглушка для урона
    // ---------------------------------------

    void Awake() => _movement = GetComponent<EnemyMovement>();

    void Update() 
    {
        // Если цель исчезла (например, кто-то другой подобрал ресурс)
        if (_currentTarget != null && !_currentTarget.gameObject.activeInHierarchy) ResetTargetAndJob();
        
        if (_currentTarget == null) 
        {
            if (!_hasResourceInHands) HandleJobSeeking();
            else HandleDelivery();
        }

        // Прямое движение через исправленный EnemyMovement
        if (_movement != null) _movement.SetMove(_currentTarget != null);
        
        CheckArrival();
    }

    private void HandleJobSeeking() 
    {
        if (_currentJob != null && _currentTarget != null) return;
        
        _currentJob = FindBestRequest();
        
        if (_currentJob != null) 
        {
            List<ResourceType> actuallyNeededTypes = _currentJob.requirements
                .Where(r => (r.currentAmount + r.reservedAmount) < r.requiredAmount)
                .Select(r => r.resourceType).ToList();

            ResourceItem groundItem = FindResourceFromListOnGround(actuallyNeededTypes);
            if (groundItem != null) 
            {
                _targetResourceType = groundItem.type;
                _currentJob.ReserveResource(_targetResourceType); 
                ReserveResource(groundItem);
                SetTarget(groundItem.transform);
                return;
            }

            Warehouse w = FindWarehouseWithAnyResource(actuallyNeededTypes, out ResourceType foundType);
            if (w != null) 
            {
                _targetResourceType = foundType;
                _currentJob.ReserveResource(_targetResourceType);
                SetTarget(w.transform);
                return;
            }
            _currentJob = null;
        }
        FindAnyResourceOnGround();
    }

    private void HandleDelivery() 
    {
        if (_currentJob != null && (!_currentJob.gameObject.activeInHierarchy || !_currentJob.NeedsSpecificResource(_targetResourceType) && !_hasResourceInHands)) 
        {
            _currentJob.UnreserveResource(_targetResourceType);
            _currentJob = null; 
        }

        if (_currentJob != null) SetTarget(_currentJob.transform);
        else 
        {
            Warehouse w = FindNearestWarehouse();
            if (w != null) SetTarget(w.transform);
            else ResetTargetAndJob();
        }
    }

    private void PickUpFromGround(ResourceItem item) 
    {
        _targetResourceType = item.type;
        _hasResourceInHands = true;
        _carriedResourceItem = item;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
        
        if (_currentJob != null) _currentJob.StartPhysicalDelivery();

        item.gameObject.SetActive(false);
        UnreserveResource();
        _currentTarget = null; 
    }

    private void PickUpFromWarehouse(Warehouse w) 
    {
        if (_currentJob == null) { ResetTargetAndJob(); return; }
        
        if (w.TryTakeResource(_targetResourceType)) 
        {
            _hasResourceInHands = true;
            _currentJob.StartPhysicalDelivery();
            if (carrySlotRenderer != null) carrySlotRenderer.sprite = _targetResourceType.defaultCarrySprite;
            _currentTarget = null;
        } 
        else 
        {
            _currentJob.UnreserveResource(_targetResourceType);
            ResetTargetAndJob();
        }
    }

    private void SetTarget(Transform target) => _currentTarget = target;
    
    private void ResetTargetAndJob() 
    { 
        UnreserveResource(); 
        if (!_hasResourceInHands && _currentJob != null) _currentJob.UnreserveResource(_targetResourceType); 
        _currentTarget = null; 
        if (!_hasResourceInHands) _currentJob = null; 
    }

    private void CheckArrival() 
    {
        if (_currentTarget == null) return;

        // В Unity 6 и для прямого движения считаем строго до центра
        float distance = Vector2.Distance(transform.position, _currentTarget.position);

        if (distance <= stopDistance) 
        {
            if (_movement != null) _movement.SetMove(false);
            ExecuteAction();
        }
    }

    private void ExecuteAction() 
    {
        if (!_hasResourceInHands) 
        {
            if (_currentTarget.TryGetComponent(out ResourceItem item)) PickUpFromGround(item);
            else if (_currentTarget.TryGetComponent(out Warehouse w)) PickUpFromWarehouse(w);
            else ResetTargetAndJob();
        } 
        else 
        {
            if (_currentTarget.TryGetComponent(out ResourceRequester r)) DeliverToRequester();
            else if (_currentTarget.TryGetComponent(out Warehouse w)) DeliverToWarehouse();
            else ResetTargetAndJob();
        }
    }

    private void DeliverToRequester() 
    { 
        if (_currentJob != null) _currentJob.DeliverResource(_targetResourceType); 
        if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject); 
        ClearHands(); 
    }

    private void DeliverToWarehouse() 
    { 
        if (_currentTarget.TryGetComponent(out Warehouse w)) w.AddResource(_targetResourceType, 1, carrySlotRenderer.sprite); 
        if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject); 
        ClearHands(); 
    }

    private void ClearHands() 
    { 
        _hasResourceInHands = false; 
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null; 
        _carriedResourceItem = null; 
        _currentJob = null; 
        _currentTarget = null; 
    }

    private void ReserveResource(ResourceItem item) 
    { 
        UnreserveResource(); 
        _targetedResourceItem = item; 
        if (_targetedResourceItem != null) _targetedResourceItem.isReserved = true; 
    }

    private void UnreserveResource() 
    { 
        if (_targetedResourceItem != null) _targetedResourceItem.isReserved = false; 
        _targetedResourceItem = null; 
    }
    
    private void OnDisable() 
    { 
        UnreserveResource(); 
        if (_currentJob != null) _currentJob.UnreserveResource(_targetResourceType); 
    }

    private ResourceRequester FindBestRequest() => ResourceRequester.AllRequesters
        .Where(r => r.NeedsAnyResource())
        .OrderByDescending(r => r.priority)
        .ThenBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
        .FirstOrDefault();

    private ResourceItem FindResourceFromListOnGround(List<ResourceType> types) => GameObject.FindGameObjectsWithTag(resourceTag)
        .Select(r => r.GetComponent<ResourceItem>())
        .Where(r => r != null && r.gameObject.activeInHierarchy && !r.isReserved && types.Contains(r.type))
        .OrderBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
        .FirstOrDefault();

    private void FindAnyResourceOnGround() 
    { 
        var res = GameObject.FindGameObjectsWithTag(resourceTag)
            .Select(r => r.GetComponent<ResourceItem>())
            .Where(r => r != null && r.gameObject.activeInHierarchy && !r.isReserved)
            .OrderBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
            .FirstOrDefault(); 
        
        if (res != null) { SetTarget(res.transform); ReserveResource(res); _targetResourceType = res.type; } 
    }

    private Warehouse FindNearestWarehouse() => Object.FindObjectsByType<Warehouse>(FindObjectsSortMode.None)
        .OrderBy(w => Vector2.SqrMagnitude(w.transform.position - transform.position))
        .FirstOrDefault();

    // Исправлено: замена лямбда-выражения на циклы для корректной работы с out параметром
    private Warehouse FindWarehouseWithAnyResource(List<ResourceType> types, out ResourceType foundType) 
    { 
        foundType = null; 
        var warehouses = Object.FindObjectsByType<Warehouse>(FindObjectsSortMode.None);

        foreach (var w in warehouses) 
        {
            foreach (var type in types) 
            {
                if (w.HasResource(type)) 
                { 
                    foundType = type; 
                    return w; 
                }
            }
        }
        return null;
    }
}