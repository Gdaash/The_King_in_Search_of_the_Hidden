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
    private bool _isGoingToWarehouse = false;
    private bool _isReturningToWarehouse = false;
    private bool _isDeliveringToWarehouse = false;
    private bool _isGoingToResource = false;

    public bool IsBusy() => _currentTarget != null || _hasResourceInHands;
    public Transform GetTarget() => _currentTarget;
    public ResourceRequester GetCurrentJob() => _currentJob; 
    public ResourceType GetCarriedResourceType() => _targetResourceType;
    public bool IsCarryingResource() => _hasResourceInHands;
    public bool GetIsAttacking() => false;
    public void FinishAttack() { } 
    public void OnTakeDamage(Transform attacker) { }
    public bool IsReturningToWarehouse() => _isReturningToWarehouse;

    void Awake() 
    {
        _movement = GetComponent<EnemyMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable() => OrderManager.Instance?.RegisterPorter(this);
    void OnDisable() => OrderManager.Instance?.UnregisterPorter(this);

    public static void NotifyAllPorters()
    {
        var porters = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None);
        foreach (var p in porters) if (p._rb != null) p._rb.WakeUp();
    }

    public void AssignTask(ResourceRequester job, ResourceItem resource)
    {
        _currentJob = job;
        _targetResourceType = resource.type;
        _currentTarget = resource.transform;
        _isGoingToWarehouse = false;
        _isReturningToWarehouse = false;
        _isDeliveringToWarehouse = false;
        _isGoingToResource = true;
        
        _currentJob.ReserveResource(_targetResourceType);
        if (_rb != null) _rb.WakeUp();
    }

    public void AssignWarehouseTask(ResourceRequester job, ResourceType resourceType)
    {
        if (Warehouse.Instance == null) return;
        _currentJob = job;
        _targetResourceType = resourceType;
        _isGoingToWarehouse = true;
        _isReturningToWarehouse = false;
        _isDeliveringToWarehouse = false;
        _isGoingToResource = false;
        _currentTarget = Warehouse.Instance.transform;
        
        _currentJob.ReserveResource(_targetResourceType);
        if (_rb != null) _rb.WakeUp();
    }

    public void AssignWarehouseGathering(ResourceItem resource)
    {
        _currentJob = null;
        _targetResourceType = resource.type;
        _currentTarget = resource.transform;
        _isGoingToWarehouse = false;
        _isReturningToWarehouse = false;
        _isDeliveringToWarehouse = false;
        _isGoingToResource = true;
        
        if (_rb != null) _rb.WakeUp();
    }

    void Update() 
    {
        if (_currentJob != null && !_currentJob.gameObject.activeInHierarchy)
        {
            ResetTask();
            return;
        }
        if (!_hasResourceInHands && _currentJob != null && !_currentJob.HasLogisticFlag())
        {
            ResetTask();
            return;
        }

        if (_currentJob == null && !_hasResourceInHands && !_isReturningToWarehouse && !_isDeliveringToWarehouse && !_isGoingToWarehouse && !_isGoingToResource)
        {
            TryReturnToWarehouse();
        }

        if (_movement != null) _movement.SetMove(_currentTarget != null);
        CheckArrival();
    }

    private void TryReturnToWarehouse()
    {
        if (Warehouse.Instance == null) return;
        _isReturningToWarehouse = true;
        _currentTarget = Warehouse.Instance.GetSpawnPointTransform();
        if (_rb != null) _rb.WakeUp();
    }

    private void CheckArrival() 
    {
        if (_currentTarget == null) return;
        if (Vector2.Distance(transform.position, _currentTarget.position) <= stopDistance) 
        {
            if (_isGoingToWarehouse) ArriveAtWarehouse();
            else if (_isGoingToResource) PickUp();
            else if (_isReturningToWarehouse || _isDeliveringToWarehouse) ArriveAtWarehouseWithResource();
            else if (!_hasResourceInHands) PickUp();
            else Deliver();
        }
    }

    private void ArriveAtWarehouse()
    {
        if (GlobalResourceManager.Instance != null && _targetResourceType != null)
        {
            if (GlobalResourceManager.Instance.TrySpendResource(_targetResourceType, 1))
            {
                _hasResourceInHands = true;
                _isGoingToWarehouse = false;
                if (carrySlotRenderer != null && _targetResourceType.defaultCarrySprite != null)
                    carrySlotRenderer.sprite = _targetResourceType.defaultCarrySprite;

                _currentTarget = _currentJob.transform;
                _currentJob.StartPhysicalDelivery();
                _currentJob.UpdateIndicator();
            }
            else ResetTask();
        }
        else ResetTask();
    }

    /// <summary>
    /// НОВОЕ: Прибытие на склад с ресурсом ИЛИ в простое
    /// </summary>
    private void ArriveAtWarehouseWithResource()
    {
        // Если носильщик пришёл В ПРОСТОЕ (без ресурса) — деспауним его
        if (_isReturningToWarehouse && !_hasResourceInHands)
        {
            Debug.Log($"[Porter] Носильщик прибыл на склад в простое, деспаунится", this);
            
            if (Warehouse.Instance != null)
            {
                Warehouse.Instance.DespawnPorter(this);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }

        // Если носильщик пришёл С РЕСУРСОМ — сдаём его в хранилище
        if (_hasResourceInHands && _targetResourceType != null)
        {
            Warehouse.Instance.DepositResource(_targetResourceType);
            ClearHands();
        }

        _isReturningToWarehouse = false;
        _isDeliveringToWarehouse = false;
        _currentTarget = null;
        _currentJob = null;
        
        if (_movement != null) _movement.SetMove(false);
        
        Debug.Log($"[Porter] Носильщик сдал ресурс на склад и ждёт новую задачу", this);
    }

    private void PickUp() 
    {
        if (_currentTarget.TryGetComponent(out ResourceItem item))
        {
            _hasResourceInHands = true;
            _carriedResourceItem = item;
            if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
            
            if (_currentJob != null) _currentJob.StartPhysicalDelivery();
            item.gameObject.SetActive(false); 
            
            if (_isGoingToResource && _currentJob == null)
            {
                _isGoingToResource = false;
                _isDeliveringToWarehouse = true;
                _currentTarget = Warehouse.Instance.transform;
            }
            else if (!_isDeliveringToWarehouse && _currentJob != null)
            {
                _isGoingToResource = false;
                _currentTarget = _currentJob.transform; 
            }
                
            if (_currentJob != null) _currentJob.UpdateIndicator(); 
        }
    }

    private void Deliver() 
    {
        if (_currentJob != null)
        {
            _currentJob.DeliverResource(_targetResourceType);
        }
        if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
        ClearAll();
    }

    public void ResetTask()
    {
        if (_currentJob != null) 
        {
            _currentJob.ForceCancelReservation(_targetResourceType);
            _currentJob.UpdateIndicator();
        }
        if (_hasResourceInHands && _carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
        ClearAll();
        TryReturnToWarehouse();
    }

    private void ClearAll()
    {
        _hasResourceInHands = false;
        _isGoingToWarehouse = false;
        _isReturningToWarehouse = false;
        _isDeliveringToWarehouse = false;
        _isGoingToResource = false;
        _currentTarget = null;
        _currentJob = null;
        _carriedResourceItem = null;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null;
        if (_movement != null) _movement.SetMove(false);
    }

    private void ClearHands()
    {
        _hasResourceInHands = false;
        _carriedResourceItem = null;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null;
    }
}
