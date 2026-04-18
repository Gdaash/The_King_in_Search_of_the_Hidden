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

    public Transform GetTarget() => _currentTarget;
    public void FinishAttack() { } 
    public void OnTakeDamage(Transform attacker) { }
    public bool GetIsAttacking() => false;

    void Awake() => _movement = GetComponent<EnemyMovement>();

    void Update() 
    {
        // Если цель была уничтожена — сбрасываем путь
        if (_currentTarget != null && !_currentTarget.gameObject.activeInHierarchy) _currentTarget = null;
        
        if (_currentTarget == null) 
        {
            if (!_hasResourceInHands) HandleJobSeeking();
            else HandleDelivery();
        }

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
                _currentTarget = groundItem.transform;
            }
        }
    }

    private void HandleDelivery() 
    {
        // Если здание пропало, пока мы несли ресурс
        if (_currentJob == null || !_currentJob.gameObject.activeInHierarchy) 
        {
            ResetTargetAndJob();
            return;
        }
        
        // Идем к зданию
        _currentTarget = _currentJob.transform;
    }

    private void PickUpFromGround(ResourceItem item) 
    {
        _targetResourceType = item.type;
        _hasResourceInHands = true;
        _carriedResourceItem = item;
        
        // Отображаем ресурс в руках
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
        
        if (_currentJob != null) _currentJob.StartPhysicalDelivery();

        // Скрываем предмет на земле, но не уничтожаем пока!
        item.gameObject.SetActive(false);
        UnreserveResource();
        _currentTarget = null; // Чтобы в Update сработал HandleDelivery
    }

    private void CheckArrival() 
    {
        if (_currentTarget == null) return;
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
            if (_currentTarget.TryGetComponent(out ResourceItem item)) 
                PickUpFromGround(item);
            else 
                ResetTargetAndJob();
        } 
        else 
        {
            if (_currentTarget.TryGetComponent(out ResourceRequester r) && r == _currentJob) 
                DeliverToRequester();
            else 
                ResetTargetAndJob();
        }
    }

    private void DeliverToRequester() 
    { 
        if (_currentJob != null) _currentJob.DeliverResource(_targetResourceType); 
        
        // Теперь можно уничтожить объект ресурса
        if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject); 
        
        ClearHands(); 
    }

    private void ResetTargetAndJob() 
    { 
        UnreserveResource(); 
        if (!_hasResourceInHands && _currentJob != null) _currentJob.UnreserveResource(_targetResourceType); 
        
        // Если мы уже что-то несем, а работы нет — просто бросаем/удаляем (так как склада нет)
        if (_hasResourceInHands) ClearHands();
        
        _currentTarget = null; 
        _currentJob = null;
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
        .Where(r => r != null && r.gameObject.activeInHierarchy && r.NeedsAnyResource())
        .OrderByDescending(r => r.priority)
        .ThenBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
        .FirstOrDefault();

    private ResourceItem FindResourceFromListOnGround(List<ResourceType> types) => GameObject.FindGameObjectsWithTag(resourceTag)
        .Select(r => r.GetComponent<ResourceItem>())
        .Where(r => r != null && r.gameObject.activeInHierarchy && !r.isReserved && types.Contains(r.type))
        .OrderBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
        .FirstOrDefault();
}