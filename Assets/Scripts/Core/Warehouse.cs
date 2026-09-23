using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Warehouse : MonoBehaviour
{
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
        if (!GlobalResourceManager.Instance.TrySpendResource(resourceType, 1)) return null;

        Vector3 spawnPos = GetSpawnPoint() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        GameObject humanObj = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
        HumanUnit human = humanObj.GetComponent<HumanUnit>();
        if (human != null) human.AssignTask(job, shouldReserve);
        return human;
    }

    public void ReturnHuman(HumanUnit human)
    {
        if (human == null || GlobalResourceManager.Instance == null || humanResourceType == null) return;
        GlobalResourceManager.Instance.AddResource(humanResourceType, 1);
    }

    public bool SendHumanHomeFrom(Vector3 position)
    {
        if (humanPrefab == null) return false;
        GameObject humanObject = Instantiate(humanPrefab, position, Quaternion.identity);
        if (humanObject.TryGetComponent(out HumanUnit human)) return true;
        Destroy(humanObject);
        return false;
    }

    public bool CanSpawnPorter()
    {
        if (GlobalResourceManager.Instance == null || humanResourceType == null || cartResourceType == null || porterPrefab == null) return false;

        return GlobalResourceManager.Instance.GetResourceAmount(humanResourceType) >= 1
            && GlobalResourceManager.Instance.GetResourceAmount(cartResourceType) >= 1;
    }

    public Porter SpawnPorter()
    {
        if (!CanSpawnPorter()) return null;
        
        if (!GlobalResourceManager.Instance.TrySpendResource(humanResourceType, 1)) return null;
        if (!GlobalResourceManager.Instance.TrySpendResource(cartResourceType, 1))
        {
            GlobalResourceManager.Instance.AddResource(humanResourceType, 1);
            return null;
        }

        Vector3 spawnPos = GetSpawnPoint() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        GameObject porterObj = Instantiate(porterPrefab, spawnPos, Quaternion.identity);
        return porterObj.GetComponent<Porter>();
    }

    /// <summary>
    /// Носильщик возвращает человека и телегу в глобальное хранилище.
    /// </summary>
    public void DespawnPorter(Porter porter)
    {
        if (porter == null || humanResourceType == null || cartResourceType == null || GlobalResourceManager.Instance == null) return;
        
        GlobalResourceManager.Instance.AddResource(humanResourceType, 1);
        GlobalResourceManager.Instance.AddResource(cartResourceType, 1);
        
        Debug.Log($"[Warehouse] Носильщик вернул человека и телегу.");
        
        // Уничтожаем носильщика
        Destroy(porter.gameObject);
    }

    public void DepositResource(ResourceType type)
    {
        if (GlobalResourceManager.Instance != null && type != null)
        {
            GlobalResourceManager.Instance.AddResource(type, 1);
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
