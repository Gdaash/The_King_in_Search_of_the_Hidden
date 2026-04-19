using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Porter : MonoBehaviour, IEnemyAI 
{
    [Header("Настройки")]
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private SpriteRenderer carrySlotRenderer;

    private EnemyMovement _movement;
    private Rigidbody2D _rb;
    private Transform _currentTarget;
    private ResourceType _targetResourceType;
    private ResourceItem _carriedResourceItem; 
    private bool _hasResourceInHands = false;
    private ResourceRequester _currentJob; 

    // Интерфейс и доступ для OrderManager/ResourceRequester
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

    // --- МЕТОД ОПОВЕЩЕНИЯ (ТОТ САМЫЙ, КОТОРОГО НЕ ХВАТАЛО) ---
    public static void NotifyAllPorters()
    {
        var porters = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None);
        foreach (var p in porters)
        {
            if (p._rb != null) p._rb.WakeUp();
            // В новой архитектуре носильщики просто ждут команды от OrderManager,
            // поэтому пробуждения Rigidbody достаточно.
        }
    }

    // Метод, который вызывает OrderManager при раздаче задач
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
        // Если здание уничтожилось или выключилось
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

        // Управление движением
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
            _currentJob.UpdateIndicator(); // Обновляем иконки в здании
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
        if (_currentJob != null) _currentJob.ForceCancelReservation(_targetResourceType);
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