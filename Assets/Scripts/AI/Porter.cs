using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Porter : MonoBehaviour, IEnemyAI 
{
    [Header("Настройки")]
    [SerializeField] private string resourceTag = "Resource";
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private SpriteRenderer carrySlotRenderer;
    [SerializeField] private float searchInterval = 0.5f;

    private EnemyMovement _movement;
    private Rigidbody2D _rb;
    private Transform _currentTarget;
    private ResourceType _targetResourceType;
    private ResourceItem _carriedResourceItem; 
    private bool _hasResourceInHands = false;
    private ResourceRequester _currentJob; 
    private float _nextSearchTime;

    public Transform GetTarget() => _currentTarget;
    public ResourceRequester GetCurrentJob() => _currentJob; 
    
    // --- НОВЫЕ МЕТОДЫ ДЛЯ СИНХРОНИЗАЦИИ ИКОНОК ---
    public ResourceType GetCarriedResourceType() => _targetResourceType;
    public bool IsCarryingResource() => _hasResourceInHands;
    // ---------------------------------------------

    public void FinishAttack() { } 
    public void OnTakeDamage(Transform attacker) { }
    public bool GetIsAttacking() => false;

    void Awake() 
    {
        _movement = GetComponent<EnemyMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update() 
    {
        if (_currentTarget != null && !_currentTarget.gameObject.activeInHierarchy) 
        {
            ResetTargetAndJob();
        }
        
        if (_currentTarget == null) 
        {
            if (!_hasResourceInHands) 
            {
                if (Time.time >= _nextSearchTime) 
                {
                    FindNewTask();
                    _nextSearchTime = Time.time + searchInterval;
                }
            }
            else HandleDelivery();
        }

        if (_movement != null) _movement.SetMove(_currentTarget != null);
        CheckArrival();
    }

    private void FindNewTask() 
    {
        var allRequesters = Object.FindObjectsByType<ResourceRequester>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var validJobs = allRequesters
            .Where(r => r.NeedsAnyResource())
            .OrderByDescending(r => r.priority)
            .ThenBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
            .ToList();

        foreach (var job in validJobs)
        {
            var neededTypes = job.requirements
                .Where(req => (req.currentAmount + req.reservedAmount) < req.requiredAmount)
                .Select(req => req.resourceType).ToList();

            ResourceItem groundItem = FindBestResourceOnGround(neededTypes);
            if (groundItem != null) 
            {
                _currentJob = job;
                _targetResourceType = groundItem.type;
                _currentJob.ReserveResource(_targetResourceType); 
                groundItem.isReserved = true;
                _currentTarget = groundItem.transform;
                if (_rb != null) _rb.WakeUp();
                return;
            }
        }
    }

    private void HandleDelivery() 
    {
        if (_currentJob == null || !_currentJob.gameObject.activeInHierarchy) 
        {
            ResetTargetAndJob();
            return;
        }
        _currentTarget = _currentJob.transform;
    }

    private void PickUpFromGround(ResourceItem item) 
    {
        _targetResourceType = item.type;
        _hasResourceInHands = true;
        _carriedResourceItem = item;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
        if (_currentJob != null) _currentJob.StartPhysicalDelivery();
        
        item.gameObject.SetActive(false);
        _currentTarget = null; 
        
        // После подбора ресурса обновляем иконки у здания, 
        // чтобы оно сразу скрыло иконку нужного типа
        if (_currentJob != null) _currentJob.UpdateIndicator();
    }

    private void CheckArrival() 
    {
        if (_currentTarget == null) return;
        if (Vector2.Distance(transform.position, _currentTarget.position) <= stopDistance) 
        {
            ExecuteAction();
        }
    }

    private void ExecuteAction() 
    {
        if (!_hasResourceInHands) 
        {
            if (_currentTarget.TryGetComponent(out ResourceItem item)) PickUpFromGround(item);
            else ResetTargetAndJob();
        } 
        else 
        {
            if (_currentTarget.TryGetComponent(out ResourceRequester r) && r == _currentJob) 
            {
                r.DeliverResource(_targetResourceType);
                if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
                ClearHands();
            }
            else ResetTargetAndJob();
        }
    }

    private void ResetTargetAndJob() 
    { 
        if (_currentJob != null) 
        {
            _currentJob.ForceCancelReservation(_targetResourceType); 
        }

        if (_hasResourceInHands && _carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
        ClearHands();
    }

    private void ClearHands() 
    { 
        _hasResourceInHands = false; 
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null; 
        _carriedResourceItem = null; 
        _currentJob = null; 
        _currentTarget = null; 
        if (_movement != null) _movement.SetMove(false);
        if (_rb != null) _rb.WakeUp();
        _nextSearchTime = 0; 
    }

    private ResourceItem FindBestResourceOnGround(List<ResourceType> types)
    {
        var allItems = Object.FindObjectsByType<ResourceItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return allItems
            .Where(i => i.gameObject.activeInHierarchy && !i.isReserved && types.Contains(i.type))
            .OrderBy(i => Vector2.SqrMagnitude(i.transform.position - transform.position))
            .FirstOrDefault();
    }
}