using UnityEngine;
using System.Linq;

[RequireComponent(typeof(EnemyMovement))]
public class HumanUnit : MonoBehaviour, IEnemyAI
{
    [Header("Настройки")]
    [SerializeField] private ResourceType humanResourceType;
    [SerializeField] private float stopDistance = 0.3f;

    private EnemyMovement _movement;
    private Rigidbody2D _rb;
    private Transform _currentTarget;
    private ResourceRequester _currentJob;
    private bool _isReserved = false;
    private bool _isReturningToWarehouse = false;

    public bool IsBusy() => _currentTarget != null;
    public Transform GetTarget() => _currentTarget;
    public ResourceRequester GetCurrentJob() => _currentJob;
    public ResourceType GetCarriedResourceType() => humanResourceType;
    public bool IsCarryingResource() => true;
    public bool GetIsAttacking() => false;
    public void FinishAttack() { }
    public void OnTakeDamage(Transform attacker) { }
    public bool IsReturningToWarehouse() => _isReturningToWarehouse;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.RegisterHumanUnit(this);
        }
        
        if (_currentJob == null)
        {
            TryReturnToWarehouse();
        }
    }

    private void OnDisable()
    {
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.UnregisterHumanUnit(this);
        }
        
        if (_isReserved && _currentJob != null)
        {
            _currentJob.ForceCancelReservation(humanResourceType);
        }
    }

    private void Update()
    {
        if (_currentJob == null && !_isReturningToWarehouse)
        {
            TryReturnToWarehouse();
        }
        
        if (_currentJob != null)
        {
            bool jobInvalid = !_currentJob.gameObject.activeInHierarchy || !_currentJob.HasLogisticFlag();
            
            if (!jobInvalid)
            {
                var humanReq = _currentJob.requirements.FirstOrDefault(r => r.resourceType == humanResourceType);
                if (humanReq != null && humanReq.currentAmount >= humanReq.requiredAmount)
                {
                    jobInvalid = true;
                }
            }
            
            if (jobInvalid)
            {
                var job = _currentJob;
                _currentJob = null;
                _currentTarget = null;
                
                if (_isReserved)
                {
                    job.ForceCancelReservation(humanResourceType);
                    job.UpdateIndicator();
                    _isReserved = false;
                }
                
                TryReturnToWarehouse();
            }
        }

        if (_currentTarget != null)
        {
            CheckArrival();
        }

        if (_movement != null)
        {
            _movement.SetMove(_currentTarget != null);
        }
    }

    public void AssignTask(ResourceRequester job, bool reserveResource = true)
    {
        _currentJob = job;
        _currentTarget = job.transform;
        _isReserved = true;
        _isReturningToWarehouse = false;

        if (reserveResource)
        {
            _currentJob.ReserveResource(humanResourceType);
        }

        if (_rb != null) _rb.WakeUp();
    }

    /// <summary>
    /// НОВОЕ: Перенаправляет возвращающегося человека на новую задачу
    /// </summary>
    public void ReassignToJob(ResourceRequester job, bool reserveResource = true)
    {
        // Отменяем возврат на склад
        _isReturningToWarehouse = false;
        
        // Назначаем новую задачу
        _currentJob = job;
        _currentTarget = job.transform;
        _isReserved = true;

        if (reserveResource)
        {
            _currentJob.ReserveResource(humanResourceType);
        }

        if (_rb != null) _rb.WakeUp();
        
        Debug.Log($"[HumanUnit] Перенаправлен на {job.gameObject.name}", this);
    }

    private void CheckArrival()
    {
        if (_currentTarget == null) return;

        if (Vector2.Distance(transform.position, _currentTarget.position) <= stopDistance)
        {
            if (_isReturningToWarehouse)
            {
                ArriveAtWarehouse();
            }
            else
            {
                DeliverToBuilding();
            }
        }
    }

    private void ArriveAtWarehouse()
    {
        var job = _currentJob;
        _currentJob = null;
        _currentTarget = null;
        _isReturningToWarehouse = false;
        
        if (job != null && _isReserved)
        {
            job.ForceCancelReservation(humanResourceType);
            job.UpdateIndicator();
            _isReserved = false;
        }
        
        if (Warehouse.Instance != null)
        {
            Warehouse.Instance.ReturnHuman(this);
        }
        
        Destroy(gameObject);
    }

    private void DeliverToBuilding()
    {
        if (_currentJob != null && _currentJob.gameObject.activeInHierarchy && _currentJob.HasLogisticFlag())
        {
            _currentJob.DeliverResource(humanResourceType);
            _isReserved = false;
        }
        else
        {
            if (_currentJob != null && _isReserved)
            {
                _currentJob.ForceCancelReservation(humanResourceType);
            }
            _isReserved = false;
            TryReturnToWarehouse();
            return;
        }

        Destroy(gameObject);
    }

    private void TryReturnToWarehouse()
    {
        if (Warehouse.Instance == null) return;
        if (_isReturningToWarehouse) return;
        
        _isReturningToWarehouse = true;
        _currentTarget = Warehouse.Instance.GetSpawnPointTransform();
        
        if (_rb != null) _rb.WakeUp();
    }

    public void ResetTask()
    {
        if (_currentJob != null && _isReserved)
        {
            _currentJob.ForceCancelReservation(humanResourceType);
        }
        _isReserved = false;
        _currentTarget = null;
        _currentJob = null;
        _isReturningToWarehouse = false;
        
        if (_movement != null) _movement.SetMove(false);
        
        TryReturnToWarehouse();
    }
}
