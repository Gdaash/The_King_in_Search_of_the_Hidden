using UnityEngine;

public class Porter : MonoBehaviour, IEnemyAI 
{
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private SpriteRenderer carrySlotRenderer;

    private EnemyMovement _movement;
    private Rigidbody2D _rb;
    private Transform _currentTarget;
    private ResourceType _targetResourceType;
    private ResourceItem _carriedResourceItem; 
    private bool _hasResourceInHands = false;
    private ResourceRequester _currentJob; 

    public bool IsBusy() => _currentTarget != null || _hasResourceInHands;
    public Transform GetTarget() => _currentTarget;
    public ResourceRequester GetCurrentJob() => _currentJob; 
    public ResourceType GetCarriedResourceType() => _targetResourceType;
    public bool IsCarryingResource() => _hasResourceInHands;
    public bool GetIsAttacking() => false;
    public void FinishAttack() { } 
    public void OnTakeDamage(Transform attacker) { }

    void Awake() 
    {
        _movement = GetComponent<EnemyMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable() => OrderManager.Instance?.RegisterPorter(this);
    void OnDisable() => OrderManager.Instance?.UnregisterPorter(this);

    // Метод, который вызывает OrderManager
    public void AssignTask(ResourceRequester job, ResourceItem resource)
    {
        _currentJob = job;
        _targetResourceType = resource.type;
        _currentTarget = resource.transform;
        
        _currentJob.ReserveResource(_targetResourceType);
        if (_rb != null) _rb.WakeUp();
    }

    void Update() 
    {
        // Если здание уничтожилось, пока мы шли
        if (_currentJob != null && !_currentJob.gameObject.activeInHierarchy)
        {
            ResetTask();
            return;
        }

        // Если флаг убрали ДО того как мы подняли ресурс — бросаем задачу
        if (!_hasResourceInHands && _currentJob != null && !_currentJob.HasLogisticFlag())
        {
            ResetTask();
            return;
        }

        if (_movement != null) _movement.SetMove(_currentTarget != null);
        CheckArrival();
    }

    private void CheckArrival() 
    {
        if (_currentTarget == null) return;
        if (Vector2.Distance(transform.position, _currentTarget.position) <= stopDistance) 
        {
            if (!_hasResourceInHands) PickUp();
            else Deliver();
        }
    }

    private void PickUp() 
    {
        if (_currentTarget.TryGetComponent(out ResourceItem item))
        {
            _hasResourceInHands = true;
            _carriedResourceItem = item;
            if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
            _currentJob.StartPhysicalDelivery();
            item.gameObject.SetActive(false);
            _currentTarget = _currentJob.transform; // Переключаемся на здание
        }
    }

    private void Deliver() 
    {
        _currentJob.DeliverResource(_targetResourceType);
        if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
        ClearAll();
    }

    private void ResetTask()
    {
        if (!_hasResourceInHands && _currentJob != null) _currentJob.ForceCancelReservation(_targetResourceType);
        if (_hasResourceInHands && _carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
        ClearAll();
    }

    private void ClearAll()
    {
        _hasResourceInHands = false;
        _currentTarget = null;
        _currentJob = null;
        _carriedResourceItem = null;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null;
        if (_movement != null) _movement.SetMove(false);
    }
}
