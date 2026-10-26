using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Physical representation of the residents already counted by a production recipe.</summary>
public sealed class ResourceWorkforce : MonoBehaviour
{
    public enum WorkerDisplay { Center, Wander }
    [SerializeField] private WorkerDisplay display = WorkerDisplay.Wander;
    [SerializeField] private bool shelterWorkers;
    private ResourceRequester requester;
    private readonly List<ResourceWorker> workers = new();
    private float nextRefresh;
    private Health buildingHealth;
    private bool quitting;
    // Occupancy, rather than production state, determines whether a shelter is a target.
    public bool CanBeAttacked => !shelterWorkers || (requester != null && requester.ResidentHumanCount > 0);
    public static bool EvacuateExposedTarget(Transform target)
    {
        if (target == null || !target.TryGetComponent<ResourceWorker>(out var worker)) return false;
        worker.FleeWithColleagues();
        return true;
    }
    private void Start()
    {
        requester = GetComponent<ResourceRequester>();
        if (requester != null) display = requester.WorkerDisplay;
        shelterWorkers = requester != null && requester.SheltersWorkers;
        if (shelterWorkers)
        {
            buildingHealth = GetComponent<Health>();
            if (buildingHealth == null) { buildingHealth = gameObject.AddComponent<Health>(); buildingHealth.ConfigureFallbackHealth(requester.ShelterHealth); }
            WorldCivilianTarget.Ensure(gameObject, null);
            buildingHealth.OnHealthChanged ??= new UnityEvent<float>();
            buildingHealth.OnHealthChanged.AddListener(OnBuildingHealth);
        }
    }
    private void OnBuildingHealth(float fraction) { if (fraction <= 0) EvacuateAll(); }
    private void Update()
    {
        if (requester == null || Time.time < nextRefresh || Warehouse.Instance == null) return;
        nextRefresh = Time.time + .2f;
        workers.RemoveAll(w => w == null);
        int wanted = requester.ResidentHumanCount;
        while (workers.Count > wanted)
        {
            var worker = workers[workers.Count - 1]; workers.RemoveAt(workers.Count - 1);
            if (worker != null) Destroy(worker.gameObject);
        }
        if (shelterWorkers) return;
        while (workers.Count < wanted)
        {
            var go = new GameObject("Resident worker");
            go.transform.position = requester.WorkerPosition;
            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = Warehouse.Instance.HumanSprite;
            visual.sortingOrder = 20;
            var collider = go.AddComponent<CircleCollider2D>(); collider.radius = .18f; collider.isTrigger = true;
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            var worker = go.AddComponent<ResourceWorker>();
            worker.Initialize(this, requester, display == WorkerDisplay.Wander);
            WorldCivilianTarget.Ensure(go, null);
            workers.Add(worker);
        }
    }
    public void Evacuate(ResourceWorker worker, bool died, float fraction)
    {
        if (!workers.Remove(worker)) return;
        var type = requester.RemoveResidentHuman();
        if (died)
        {
            GameFoundation.MetaProgression.WorldCasualties.Death(type,worker.gameObject);
            if (type != null) GlobalResourceManager.Instance?.TrySpendResource(type, 1);
        }
        else Warehouse.Instance?.SendHumanHomeFrom(worker.transform.position, fraction);
    }
    public void FleeExposedWorkers()
    {
        if (shelterWorkers) return;
        // Evacuation removes workers from the list, so walk it backwards.
        for (int i = workers.Count - 1; i >= 0; i--)
            if (workers[i] != null) workers[i].OnTakeDamage(null);
    }
    private void EvacuateAll()
    {
        if (requester == null) return;
        if (!Application.isPlaying || !gameObject.scene.isLoaded || Warehouse.Instance == null || !Warehouse.Instance.isActiveAndEnabled) return;
        while (requester.ResidentHumanCount > 0)
        {
            requester.RemoveResidentHuman();
            Warehouse.Instance?.SendHumanHomeFrom(transform.position);
        }
        foreach (var worker in workers) if (worker != null) Destroy(worker.gameObject);
        workers.Clear();
    }
    private void OnApplicationQuit() => quitting = true;
    private void OnDisable() { if (!quitting) EvacuateAll(); }
}

public sealed class ResourceWorker : MonoBehaviour, IEnemyAI
{
    private ResourceWorkforce owner;
    private ResourceRequester job;
    private Vector3 origin;
    private bool wander, leaving;
    private float phase;
    private SpriteRenderer visual;
    public void Initialize(ResourceWorkforce source, ResourceRequester target, bool moves)
    {
        owner = source; job = target; origin = target.WorkerPosition;
        wander = moves; phase = Random.value * 6.28f;
        visual = GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        if (job == null || leaving || Time.timeScale <= 0) return;
        float t = Time.time * 2 + phase;
        Vector3 next = origin + (wander ? new Vector3(Mathf.Sin(t * .4f) * .45f, Mathf.Cos(t * .55f) * .18f, 0) : Vector3.zero)
            + Vector3.up * Mathf.Abs(Mathf.Sin(t * 3)) * .035f;
        float horizontal = next.x - transform.position.x;
        // The warehouse worker sprite faces left in its original artwork.
        if (visual != null && Mathf.Abs(horizontal) > .0001f) visual.flipX = horizontal > 0;
        transform.position = next;
    }
    public Transform GetTarget() => null;
    public bool GetIsAttacking() => false;
    public void FinishAttack() { }
    public void OnTakeDamage(Transform attacker)
    {
        if (leaving) return;
        leaving = true;
        var health = GetComponent<Health>();
        owner.Evacuate(this, health != null && health.CurrentHealth <= 0, health != null ? health.NormalizedHealth : 1);
        Destroy(gameObject);
    }
    public void FleeWithColleagues()
    {
        if (leaving || owner == null) return;
        // Evacuate living workers separately to preserve each resident's health.
        owner.FleeExposedWorkers();
    }
}
