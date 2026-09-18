using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Настройки")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float priorityBonusInterval = 10f;
    [SerializeField] private float humanSpawnCooldown = 1.0f;

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
            inTransit = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None).Count(p => p.GetCurrentJob() == requester);
            if (Instance != null) inTransit += Instance._allHumanUnits.Count(h => h.GetCurrentJob() == requester);
        }
    }

    private class OrderInternal
    {
        public ResourceRequester requester;
        public float startTime;
        public int bonusPriority;
        public OrderInternal(ResourceRequester r) { requester = r; startTime = Time.time; }
    }

    [Header("Текущая очередь заказов (Инспектор)")]
    [SerializeField] private List<OrderStatus> ordersQueue = new List<OrderStatus>();

    private List<OrderInternal> _internalOrders = new List<OrderInternal>();
    private List<Porter> _allPorters = new List<Porter>();
    private List<HumanUnit> _allHumanUnits = new List<HumanUnit>();
    private float _nextUpdateTime;
    private Dictionary<ResourceRequester, float> _lastHumanSpawnTime = new Dictionary<ResourceRequester, float>();

    void Awake()
    {
        if (Instance == null) { Instance = this; FindInitialObjects(); }
        else Destroy(gameObject);
    }

    private void FindInitialObjects()
    {
        foreach (var r in Object.FindObjectsByType<ResourceRequester>(FindObjectsSortMode.None)) RegisterRequester(r);
        foreach (var p in Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)) RegisterPorter(p);
        foreach (var h in Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None)) RegisterHumanUnit(h);
    }

    public void RegisterRequester(ResourceRequester r) { if (r != null && !_internalOrders.Any(o => o.requester == r)) _internalOrders.Add(new OrderInternal(r)); }
    public void UnregisterRequester(ResourceRequester r) { _internalOrders.RemoveAll(o => o.requester == r); _lastHumanSpawnTime.Remove(r); }
    public void RegisterPorter(Porter p) { if (p != null && !_allPorters.Contains(p)) _allPorters.Add(p); }
    public void UnregisterPorter(Porter p) => _allPorters.Remove(p);
    public void RegisterHumanUnit(HumanUnit h) { if (h != null && !_allHumanUnits.Contains(h)) _allHumanUnits.Add(h); }
    public void UnregisterHumanUnit(HumanUnit h) => _allHumanUnits.Remove(h);
    public void ForceUpdateOrders() => _nextUpdateTime = 0;
    public List<HumanUnit> GetHumanUnitsForRequester(ResourceRequester r) => _allHumanUnits.Where(h => h.GetCurrentJob() == r && h.IsBusy()).ToList();

    void Update()
    {
        SyncInspectorList();
        CleanCooldowns();
        if (Time.time < _nextUpdateTime) return;
        _nextUpdateTime = Time.time + updateInterval;
        CleanLists();
        DistributeOrders();
    }

    private void SyncInspectorList()
    {
        ordersQueue.Clear();
        foreach (var io in _internalOrders)
        {
            if (io.requester == null) continue;
            float timeActive = Time.time - io.startTime;
            io.bonusPriority = Mathf.FloorToInt(timeActive / priorityBonusInterval);
            var status = new OrderStatus { requester = io.requester, waitTime = timeActive };
            status.UpdateData(io.bonusPriority);
            ordersQueue.Add(status);
        }
        ordersQueue = ordersQueue.OrderByDescending(s => s.totalPriority).ToList();
    }

    private void CleanLists()
    {
        _internalOrders.RemoveAll(o => o.requester == null || !o.requester.gameObject.activeInHierarchy);
        _allPorters.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
        _allHumanUnits.RemoveAll(h => h == null || !h.gameObject.activeInHierarchy);
    }
    
    private void CleanCooldowns()
    {
        var expired = _lastHumanSpawnTime.Where(kvp => Time.time - kvp.Value > humanSpawnCooldown).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired) _lastHumanSpawnTime.Remove(key);
    }

    private void DistributeOrders()
    {
        var freePorters = _allPorters.Where(p => !p.IsBusy() && !p.IsReturningToWarehouse()).ToList();
        var returningHumans = _allHumanUnits.Where(h => h.IsReturningToWarehouse()).ToList();
        var freeHumans = _allHumanUnits.Where(h => !h.IsBusy() && !h.IsReturningToWarehouse()).ToList();

        var activeJobs = _internalOrders
            .Where(o => o.requester.HasLogisticFlag() && o.requester.NeedsAnyResource())
            .OrderByDescending(o => o.requester.priority + o.bonusPriority)
            .ToList();

        var allResources = Object.FindObjectsByType<ResourceItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(i => !i.isReserved && i.gameObject.activeInHierarchy)
            .ToList();

        // === 1. РАСПРЕДЕЛЕНИЕ ДЛЯ НОСИЛЬЩИКОВ (ПРИОРИТЕТ: ЗДАНИЯ) ===
        if (activeJobs.Count > 0)
        {
            foreach (var order in activeJobs)
            {
                if (!order.requester.NeedsAnyResource()) continue;

                var neededTypes = order.requester.requirements
                    .Where(req => (req.currentAmount + req.reservedAmount) < req.requiredAmount && !req.resourceType.isHumanResource)
                    .Select(req => req.resourceType).ToList();

                foreach (var neededType in neededTypes)
                {
                    // Ищем ресурс на карте
                    var bestResource = allResources.Where(res => res.type == neededType)
                        .OrderBy(res => Vector2.SqrMagnitude(res.transform.position - order.requester.transform.position))
                        .FirstOrDefault();

                    bool assigned = false;

                    if (bestResource != null)
                    {
                        // === ПРИОРИТЕТ 1: Ресурс есть на карте ===
                        var freePorter = freePorters.FirstOrDefault();
                        if (freePorter != null)
                        {
                            freePorter.AssignTask(order.requester, bestResource);
                            bestResource.isReserved = true;
                            order.requester.ReserveResource(bestResource.type);
                            allResources.Remove(bestResource);
                            freePorters.Remove(freePorter);
                            assigned = true;
                        }
                        else if (Warehouse.Instance != null && Warehouse.Instance.CanSpawnPorter())
                        {
                            var spawnedPorter = Warehouse.Instance.SpawnPorter();
                            if (spawnedPorter != null)
                            {
                                RegisterPorter(spawnedPorter);
                                spawnedPorter.AssignTask(order.requester, bestResource);
                                bestResource.isReserved = true;
                                order.requester.ReserveResource(bestResource.type);
                                allResources.Remove(bestResource);
                                assigned = true;
                            }
                        }
                    }
                    else
                    {
                        // === ПРИОРИТЕТ 2: Ресурса на карте нет, но есть на складе ===
                        if (GlobalResourceManager.Instance != null && 
                            GlobalResourceManager.Instance.GetResourceAmount(neededType) > 0)
                        {
                            var freePorter = freePorters.FirstOrDefault();
                            if (freePorter != null)
                            {
                                // Назначаем свободного носильщика идти на склад
                                freePorter.AssignWarehouseTask(order.requester, neededType);
                                order.requester.ReserveResource(neededType);
                                freePorters.Remove(freePorter);
                                assigned = true;
                                Debug.Log($"[OrderManager] Свободный носильщик отправлен на склад за {neededType.resourceName} для {order.requester.gameObject.name}", this);
                            }
                            else if (Warehouse.Instance != null && Warehouse.Instance.CanSpawnPorter())
                            {
                                // Спауним нового носильщика для похода на склад
                                var spawnedPorter = Warehouse.Instance.SpawnPorter();
                                if (spawnedPorter != null)
                                {
                                    RegisterPorter(spawnedPorter);
                                    spawnedPorter.AssignWarehouseTask(order.requester, neededType);
                                    order.requester.ReserveResource(neededType);
                                    assigned = true;
                                    Debug.Log($"[OrderManager] Заспаунен носильщик для похода на склад за {neededType.resourceName} для {order.requester.gameObject.name}", this);
                                }
                            }
                        }
                    }

                    if (assigned) break;
                }
            }
        }

        // === 2. РАСПРЕДЕЛЕНИЕ ДЛЯ ЛЮДЕЙ (HumanUnit) ===
        if (activeJobs.Count > 0)
        {
            foreach (var order in activeJobs)
            {
                var humanReq = order.requester.requirements.FirstOrDefault(r => r.resourceType.isHumanResource);
                if (humanReq == null || !order.requester.NeedsAnyResource()) continue;

                int neededHumans = humanReq.requiredAmount - (humanReq.currentAmount + humanReq.reservedAmount);
                if (neededHumans <= 0) continue;

                if (_lastHumanSpawnTime.ContainsKey(order.requester))
                {
                    if (Time.time - _lastHumanSpawnTime[order.requester] < humanSpawnCooldown) continue;
                }

                var returningHuman = returningHumans.FirstOrDefault();
                if (returningHuman != null)
                {
                    order.requester.ReserveResource(humanReq.resourceType);
                    returningHuman.ReassignToJob(order.requester, false);
                    returningHumans.Remove(returningHuman);
                    _lastHumanSpawnTime[order.requester] = Time.time;
                    break;
                }

                var freeHuman = freeHumans.FirstOrDefault();
                if (freeHuman != null)
                {
                    order.requester.ReserveResource(humanReq.resourceType);
                    freeHuman.AssignTask(order.requester, false);
                    freeHumans.Remove(freeHuman);
                    _lastHumanSpawnTime[order.requester] = Time.time;
                    break;
                }

                if (Warehouse.Instance != null)
                {
                    order.requester.ReserveResource(humanReq.resourceType);
                    var spawned = Warehouse.Instance.SpawnHumanForJob(order.requester, humanReq.resourceType, false);
                    if (spawned != null)
                    {
                        RegisterHumanUnit(spawned);
                        _lastHumanSpawnTime[order.requester] = Time.time;
                    }
                    else order.requester.ForceCancelReservation(humanReq.resourceType);
                    break;
                }
            }
        }

        // === 3. СБОР БЕСХОЗНЫХ РЕСУРСОВ ДЛЯ СКЛАДА ===
        if (Warehouse.Instance != null)
        {
            var neededByBuildings = new HashSet<ResourceType>();
            foreach (var order in activeJobs)
            {
                foreach (var req in order.requester.requirements)
                {
                    if ((req.currentAmount + req.reservedAmount) < req.requiredAmount && !req.resourceType.isHumanResource)
                        neededByBuildings.Add(req.resourceType);
                }
            }

            var unneededResources = allResources.Where(r => !neededByBuildings.Contains(r.type)).ToList();

            if (unneededResources.Count > 0)
            {
                var idleWarehousePorters = _allPorters.Where(p => 
                    !p.IsBusy() && 
                    Vector2.Distance(p.transform.position, Warehouse.Instance.transform.position) <= Warehouse.Instance.checkRadius
                ).ToList();

                if (idleWarehousePorters.Count() == 0 && Warehouse.Instance.CanSpawnPorter())
                {
                    var spawnedPorter = Warehouse.Instance.SpawnPorter();
                    if (spawnedPorter != null)
                    {
                        RegisterPorter(spawnedPorter);
                        idleWarehousePorters.Add(spawnedPorter);
                        Debug.Log($"[OrderManager] Заспаунен носильщик для сбора ресурсов", this);
                    }
                }

                if (idleWarehousePorters.Count() > 0)
                {
                    foreach (var porter in idleWarehousePorters)
                    {
                        var resource = unneededResources.FirstOrDefault();
                        if (resource != null)
                        {
                            porter.AssignWarehouseGathering(resource);
                            resource.isReserved = true;
                            unneededResources.Remove(resource);
                            allResources.Remove(resource);
                            Debug.Log($"[OrderManager] Носильщик назначен на сбор {resource.type.resourceName}", this);
                        }
                        else break;
                    }
                }
            }
        }
    }
}