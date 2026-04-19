using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Настройки")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float priorityBonusInterval = 10f;

    // Этот класс теперь виден в инспекторе
    [System.Serializable]
    public class OrderStatus
    {
        public string buildingName;
        public bool hasFlag;
        public int totalPriority;
        public float waitTime;
        public int inTransit;
        [HideInInspector] public ResourceRequester requester;

        public void UpdateData(float bonus)
        {
            if (requester == null) return;
            buildingName = requester.gameObject.name;
            hasFlag = requester.HasLogisticFlag();
            totalPriority = requester.priority + (int)bonus;
            inTransit = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)
                .Count(p => p.GetCurrentJob() == requester);
        }
    }

    private class OrderInternal
    {
        public ResourceRequester requester;
        public float startTime;
        public int bonusPriority;

        public OrderInternal(ResourceRequester r)
        {
            requester = r;
            startTime = Time.time;
        }
    }

    [Header("Текущая очередь заказов")]
    [SerializeField] private List<OrderStatus> ordersQueue = new List<OrderStatus>();

    private List<OrderInternal> _internalOrders = new List<OrderInternal>();
    private List<Porter> _allPorters = new List<Porter>();
    private float _nextUpdateTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterRequester(ResourceRequester r)
    {
        if (!_internalOrders.Any(o => o.requester == r))
            _internalOrders.Add(new OrderInternal(r));
    }

    public void UnregisterRequester(ResourceRequester r) => _internalOrders.RemoveAll(o => o.requester == r);
    public void RegisterPorter(Porter p) { if (!_allPorters.Contains(p)) _allPorters.Add(p); }
    public void UnregisterPorter(Porter p) => _allPorters.Remove(p);
    public void ForceUpdateOrders() => _nextUpdateTime = 0;

    void Update()
    {
        // Обновляем бонусы и визуальный список для инспектора
        SyncInspectorList();

        if (Time.time < _nextUpdateTime) return;
        _nextUpdateTime = Time.time + updateInterval;

        CleanLists();
        DistributeOrders();
    }

    private void SyncInspectorList()
    {
        // Очищаем и заново наполняем список для отображения в инспекторе
        ordersQueue.Clear();
        foreach (var internalOrder in _internalOrders)
        {
            if (internalOrder.requester == null) continue;
            
            float timeActive = Time.time - internalOrder.startTime;
            internalOrder.bonusPriority = Mathf.FloorToInt(timeActive / priorityBonusInterval);

            var status = new OrderStatus { requester = internalOrder.requester };
            status.waitTime = timeActive;
            status.UpdateData(internalOrder.bonusPriority);
            ordersQueue.Add(status);
        }
        
        // Сортируем список в инспекторе так же, как его видят носильщики
        ordersQueue = ordersQueue.OrderByDescending(s => s.totalPriority).ToList();
    }

    private void CleanLists()
    {
        _internalOrders.RemoveAll(o => o.requester == null || !o.requester.gameObject.activeInHierarchy);
        _allPorters.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
    }

    private void DistributeOrders()
    {
        var freePorters = _allPorters.Where(p => !p.IsBusy()).ToList();
        if (freePorters.Count == 0) return;

        var activeJobs = _internalOrders
            .Where(o => o.requester.HasLogisticFlag() && o.requester.NeedsAnyResource())
            .OrderByDescending(o => o.requester.priority + o.bonusPriority)
            .ToList();

        if (activeJobs.Count == 0) return;

        var allResources = Object.FindObjectsByType<ResourceItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(i => !i.isReserved && i.gameObject.activeInHierarchy)
            .ToList();

        foreach (var porter in freePorters)
        {
            bool porterAssigned = false;
            foreach (var order in activeJobs)
            {
                if (porterAssigned) break;

                var neededTypes = order.requester.requirements
                    .Where(req => (req.currentAmount + req.reservedAmount) < req.requiredAmount)
                    .Select(req => req.resourceType).ToList();

                var bestResource = allResources
                    .Where(res => neededTypes.Contains(res.type))
                    .OrderBy(res => Vector2.SqrMagnitude(res.transform.position - porter.transform.position))
                    .FirstOrDefault();

                if (bestResource != null)
                {
                    porter.AssignTask(order.requester, bestResource);
                    bestResource.isReserved = true;
                    allResources.Remove(bestResource);
                    porterAssigned = true;
                }
            }
        }
    }
}
