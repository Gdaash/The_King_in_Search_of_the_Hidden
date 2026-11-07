using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Warehouse : MonoBehaviour
{
    public static event System.Action<ResourceType> ResourceDeposited;
    public static Warehouse Instance { get; private set; }
    public static List<Warehouse> AllWarehouses = new List<Warehouse>();

    [Header("Настройки склада")]
    [SerializeField] private Transform spawnPoint;
    public float checkRadius = 2f;
    [SerializeField] private GameObject humanPrefab;
    [SerializeField] private ResourceType humanResourceType;
    [SerializeField] private ResourceType cartResourceType;
    
    [Header("Префаб носильщика")]
    [Tooltip("Префаб носильщика (Porter), который будет спауниться за 1 человека")]
    [SerializeField] private GameObject porterPrefab;

    private Collider2D _myCollider;
    private List<HumanUnit> _humansInside = new List<HumanUnit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _myCollider = GetComponent<Collider2D>();
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void OnEnable()
    {
        if (!AllWarehouses.Contains(this)) AllWarehouses.Add(this);
    }

    private void OnDisable()
    {
        AllWarehouses.Remove(this);
        if (Instance == this) Instance = null;
    }

    public Vector3 GetSpawnPoint() => spawnPoint != null ? spawnPoint.position : transform.position;
    public Transform GetSpawnPointTransform() => spawnPoint != null ? spawnPoint : transform;

    public HumanUnit SpawnHumanForJob(ResourceRequester job, ResourceType resourceType, bool shouldReserve = true)
    {
        if (humanPrefab == null || resourceType == null || GlobalResourceManager.Instance == null) return null;
        if (GlobalResourceManager.Instance.GetResourceAmount(resourceType) <= 0) return null;

        Vector3 spawnPos = GetSpawnPoint() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        GameObject humanObj = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        HumanUnit human = humanObj.GetComponent<HumanUnit>();
        if (human != null) human.AssignTask(job, shouldReserve);
        return human;
    }

    public void ReturnHuman(HumanUnit human)
    {
        if (human == null) return;
        if (!human.TryCompletePortalReturn()) return;
    }

    public Sprite HumanSprite => humanPrefab != null ? humanPrefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite : null;
    public ResourceType HumanResource => humanResourceType;
    public bool SendHumanHomeFrom(Vector3 position, float healthFraction = 1f)
    {
        if (humanPrefab == null) return false;
        GameObject humanObject = Instantiate(humanPrefab, position, Quaternion.identity);
        if (humanObject.TryGetComponent(out HumanUnit human)) { human.ReturnToPortal(); human.GetComponent<Health>()?.SetNormalizedHealth(healthFraction); return true; }
        Destroy(humanObject);
        return false;
    }

    public bool CanSpawnPorter()
    {
        if (GlobalResourceManager.Instance == null || humanResourceType == null || cartResourceType == null || porterPrefab == null || porterPrefab.GetComponent<Porter>() == null) return false;

        return OrderManager.CountHumansAtPortal(humanResourceType) >= 1
            && GlobalResourceManager.Instance.GetResourceAmount(cartResourceType) >= 1;
    }

    public Porter SpawnPorter()
    {
        if (!CanSpawnPorter()) return null;
        using var notification = GameFoundation.UI.GameNotifications.BeginTransfer(humanResourceType, cartResourceType);
        
        if (!GlobalResourceManager.Instance.TrySpendResource(cartResourceType, 1))
            return null;

        Vector3 spawnPos = GetSpawnPoint() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        GameObject porterObj = Instantiate(porterPrefab, spawnPos, Quaternion.identity);
        var porter = porterObj.GetComponent<Porter>();
        porter.InitializeCart(cartResourceType);
        return porter;
    }

    /// <summary>
    /// Носильщик возвращает телегу в хранилище; сам носильщик остаётся в общем населении.
    /// </summary>
    public void DespawnPorter(Porter porter)
    {
        if (porter == null || cartResourceType == null || GlobalResourceManager.Instance == null) return;
        if (!porter.TryCompletePortalReturn()) return;
        porter.gameObject.SetActive(false);
        using var notification = GameFoundation.UI.GameNotifications.BeginTransfer(humanResourceType, cartResourceType);
        
        GlobalResourceManager.Instance.AddResource(cartResourceType, 1);
        
        Debug.Log("[Warehouse] Носильщик вернул телегу в запас.");
        
        // Уничтожаем носильщика
        Destroy(porter.gameObject);
    }

    /// <summary>Commit returned carts before closing a run, and prevent late refunds for abandoned carts.</summary>
    public static void SettlePorterCarts()
    {
        foreach (var porter in Object.FindObjectsByType<Porter>())
        {
            if (!porter.OwnsCart) continue;
            if (OrderManager.IsPorterAwayFromPortal(porter)) porter.ForfeitCart();
            else
            {
                var warehouse = AllWarehouses.FirstOrDefault(w => w != null && w.gameObject.scene == porter.gameObject.scene);
                if (warehouse != null) warehouse.DespawnPorter(porter);
                else porter.ForfeitCart();
            }
        }
    }

    public void DepositResource(ResourceType type)
    {
        var tower = GameFoundation.MetaProgression.PortalTowerProgression.Instance;
        if (tower != null && tower.TryDeliverCrystal(type, transform.position)) return;
        if (GlobalResourceManager.Instance != null && type != null)
        {
            GlobalResourceManager.Instance.AddResource(type, 1);
            ResourceDeposited?.Invoke(type);
        }
    }

    public Dictionary<ResourceType, int> GetInventoryData()
    {
        return GlobalResourceManager.Instance != null ? GlobalResourceManager.Instance.GetAllResourcesData() : new Dictionary<ResourceType, int>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<HumanUnit>(out var human) && !_humansInside.Contains(human))
            _humansInside.Add(human);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<HumanUnit>(out var human))
            _humansInside.Remove(human);
    }
}
