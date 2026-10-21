using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned pool for alarm enemies and their spawn warnings.</summary>
public sealed class WorldSpawnPool : MonoBehaviour
{
    private readonly Dictionary<GameObject, Stack<WorldPooledObject>> available = new();
    private readonly HashSet<WorldPooledObject> members = new();
    private bool disposing;
    private sealed class Reserve
    {
        public GameObject prefab;
        public int count;
        public Stack<WorldPooledObject> available;
    }
    private readonly List<Reserve> reserves = new();
    private int nextReserve;

    public void Prewarm(GameObject prefab, int count)
    {
        PlanReserve(prefab, count);
        if (prefab == null) return;
        var queue = available[prefab];
        while (queue.Count < count) Create(prefab);
    }

    public void PlanReserve(GameObject prefab, int count)
    {
        if (prefab == null) return;
        if (!available.TryGetValue(prefab, out var queue)) available[prefab] = queue = new();
        var reserve = reserves.Find(r => r.prefab == prefab);
        if (reserve == null) reserves.Add(new Reserve { prefab = prefab, count = count, available = queue });
        else reserve.count = Mathf.Max(reserve.count, count);
    }

    private void LateUpdate()
    {
        if (disposing || Time.timeScale <= 0f) return;
        // Replenish before the next telegraph/spawn, at most one object per frame.
        // Active enemies are never recycled to make room for another wave.
        for (int i = 0; i < reserves.Count; i++)
        {
            var reserve = reserves[nextReserve++ % reserves.Count];
            if (nextReserve >= reserves.Count) nextReserve = 0;
            if (reserve.prefab == null || reserve.available.Count >= reserve.count) continue;
            Create(reserve.prefab);
            break;
        }
    }

    private WorldPooledObject Create(GameObject prefab)
    {
        // Awake/OnEnable run once during warmup; no Start/Update/render happens before deactivation.
        var instance = Instantiate(prefab, transform);
        if (instance.TryGetComponent<Health>(out _))
        {
            if (!instance.TryGetComponent<GameFoundation.MetaProgression.EnemyLevel>(out _)) instance.AddComponent<GameFoundation.MetaProgression.EnemyLevel>();
            if (!instance.TryGetComponent<AlarmSpawnedEnemy>(out _)) instance.AddComponent<AlarmSpawnedEnemy>();
        }
        var member = instance.AddComponent<WorldPooledObject>();
        member.Initialize(this, prefab);
        members.Add(member);
        instance.SetActive(false);
        Return(member);
        return member;
    }

    public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!available.TryGetValue(prefab, out var queue)) available[prefab] = queue = new();
        WorldPooledObject member = null;
        while (queue.Count > 0 && member == null) member = queue.Pop();
        if (member == null) { Create(prefab); member = queue.Pop(); }
        member.Queued = false;
        member.Prepare(position, rotation);
        member.gameObject.SetActive(true);
        return member.gameObject;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null) return;
        if (instance.TryGetComponent<WorldPooledObject>(out var member) && member.Owner != null)
        {
            instance.SetActive(false);
            member.Owner.Return(member);
        }
        else Destroy(instance);
    }

    internal void Return(WorldPooledObject member)
    {
        if (disposing || member == null || member.Queued) return;
        member.Queued = true;
        if (!available.TryGetValue(member.Prefab, out var queue)) available[member.Prefab] = queue = new();
        queue.Push(member);
    }

    internal void Forget(WorldPooledObject member) => members.Remove(member);
    private void OnDestroy()
    {
        disposing = true;
        // All members stay parented to this scene-owned pool and are destroyed with it.
        available.Clear(); members.Clear(); reserves.Clear();
    }
}
