using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class Porter : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 4f;
    [SerializeField] private string resourceTag = "Resource";
    [SerializeField] private float stopDistance = 0.2f;

    [Header("События")]
    public UnityEvent OnMove;   
    public UnityEvent OnStop;   

    [Header("Визуал")]
    [SerializeField] private SpriteRenderer mainBodyRenderer;
    [SerializeField] private SpriteRenderer carrySlotRenderer;

    private Transform _target;
    private ResourceType _targetResourceType;
    private ResourceItem _carriedResourceItem;
    
    private bool _hasResourceInHands = false;
    private ResourceRequester _currentJob;

    void Update()
    {
        if (!_hasResourceInHands)
        {
            // Ищем здание, которому нужен ресурс
            ResourceRequester newJob = FindBestRequest();

            // Если здание сменилось или исчезло, снимаем старую бронь
            if (newJob != _currentJob)
            {
                if (_currentJob != null) _currentJob.UnreserveSpot();
                _currentJob = newJob;
            }

            if (_currentJob != null)
            {
                ResourceItem groundItem = FindResourceOnGround(_currentJob.neededType);
                if (groundItem != null)
                {
                    _target = groundItem.transform;
                    MoveToTargetLogic(() => PickUpFromGround(true), baseSpeed);
                    return;
                }

                Warehouse w = FindWarehouseWithResource(_currentJob.neededType);
                if (w != null)
                {
                    _target = w.transform;
                    MoveToTargetLogic(() => PickUpFromWarehouse(w, _currentJob.neededType), baseSpeed, true);
                    return;
                }
            }

            FindAnyResourceOnGround();
            if (_target != null) MoveToTargetLogic(() => PickUpFromGround(false), baseSpeed);
            else OnStop?.Invoke();
        }
        else
        {
            if (_currentJob != null)
            {
                _target = _currentJob.transform;
                float currentSpeed = baseSpeed - (_carriedResourceItem != null ? _carriedResourceItem.weight : 0.2f);
                MoveToTargetLogic(DeliverToRequester, currentSpeed, true);
            }
            else
            {
                Warehouse w = FindNearestWarehouse();
                if (w != null)
                {
                    _target = w.transform;
                    float currentSpeed = baseSpeed - (_carriedResourceItem != null ? _carriedResourceItem.weight : 0.2f);
                    MoveToTargetLogic(DeliverToWarehouse, currentSpeed, true);
                }
                else OnStop?.Invoke();
            }
        }
    }

    private ResourceRequester FindBestRequest()
    {
        return ResourceRequester.AllRequesters
            .Where(r => r.NeedsResource())
            .OrderByDescending(r => r.priority)
            .ThenBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
            .FirstOrDefault();
    }

    private ResourceItem FindResourceOnGround(ResourceType type)
    {
        GameObject[] resources = GameObject.FindGameObjectsWithTag(resourceTag);
        return resources
            .Select(r => r.GetComponent<ResourceItem>())
            .Where(r => r != null && r.gameObject.activeInHierarchy && r.type == type)
            .OrderBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
            .FirstOrDefault();
    }

    private void FindAnyResourceOnGround()
    {
        GameObject[] resources = GameObject.FindGameObjectsWithTag(resourceTag);
        _target = resources
            .Where(r => r.activeInHierarchy)
            .OrderBy(r => Vector2.SqrMagnitude(r.transform.position - transform.position))
            .Select(r => r.transform)
            .FirstOrDefault();
    }

    private Warehouse FindWarehouseWithResource(ResourceType type)
    {
        return Warehouse.AllWarehouses
            .Where(w => w.HasResource(type))
            .OrderBy(w => Vector2.SqrMagnitude(w.transform.position - transform.position))
            .FirstOrDefault();
    }

    private Warehouse FindNearestWarehouse()
    {
        return Warehouse.AllWarehouses
            .OrderBy(w => Vector2.SqrMagnitude(w.transform.position - transform.position))
            .FirstOrDefault();
    }

    private void PickUpFromWarehouse(Warehouse w, ResourceType type)
    {
        if (w.TryTakeResource(type))
        {
            _targetResourceType = type;
            if (_currentJob != null) _currentJob.ReserveSpot();
            if (carrySlotRenderer != null) carrySlotRenderer.sprite = type.defaultCarrySprite;
            _hasResourceInHands = true;
            _target = null;
        }
    }

    private void PickUpFromGround(bool isForJob)
    {
        if (_target != null && _target.TryGetComponent(out ResourceItem item))
        {
            _targetResourceType = item.type;
            if (carrySlotRenderer != null) carrySlotRenderer.sprite = item.carrySprite;
            _hasResourceInHands = true;
            if (isForJob && _currentJob != null) _currentJob.ReserveSpot();
            else _currentJob = null; 
            item.gameObject.SetActive(false);
            _carriedResourceItem = item;
            _target = null;
        }
    }

    private void DeliverToRequester()
    {
        if (_currentJob != null)
        {
            _currentJob.DeliverResource(_targetResourceType);
            if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
            ClearHands();
        }
    }

    private void DeliverToWarehouse()
    {
        Warehouse w = _target.GetComponent<Warehouse>();
        if (w != null)
        {
            w.AddResource(_targetResourceType, 1, carrySlotRenderer.sprite);
            if (_carriedResourceItem != null) Destroy(_carriedResourceItem.gameObject);
            ClearHands();
        }
    }

    private void ClearHands()
    {
        _hasResourceInHands = false;
        if (carrySlotRenderer != null) carrySlotRenderer.sprite = null;
        _target = null;
        _currentJob = null;
        _carriedResourceItem = null;
    }

    // Если носильщик выключается или умирает, он должен освободить "забронированное" место
    private void OnDisable()
    {
        if (!_hasResourceInHands && _currentJob != null)
        {
            _currentJob.UnreserveSpot();
        }
    }

    private void MoveToTargetLogic(System.Action onReached, float speed, bool useColliderEdge = false)
    {
        if (_target == null) return;
        Vector3 destination = _target.position;
        if (useColliderEdge)
        {
            Collider2D col = _target.GetComponent<Collider2D>();
            if (col != null) destination = col.ClosestPoint(transform.position);
        }

        if (Vector2.Distance(transform.position, destination) > stopDistance)
        {
            OnMove?.Invoke();
            UpdateSpriteDirection(destination.x - transform.position.x);
            transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }
        else { OnStop?.Invoke(); onReached?.Invoke(); }
    }

    private void UpdateSpriteDirection(float dirX)
    {
        if (Mathf.Abs(dirX) > 0.01f)
        {
            bool shouldFlip = dirX < 0;
            if (mainBodyRenderer != null) mainBodyRenderer.flipX = shouldFlip;
            if (carrySlotRenderer != null)
            {
                carrySlotRenderer.flipX = shouldFlip;
                Vector3 pos = carrySlotRenderer.transform.localPosition;
                pos.x = shouldFlip ? -Mathf.Abs(pos.x) : Mathf.Abs(pos.x);
                carrySlotRenderer.transform.localPosition = pos;
            }
        }
    }
}