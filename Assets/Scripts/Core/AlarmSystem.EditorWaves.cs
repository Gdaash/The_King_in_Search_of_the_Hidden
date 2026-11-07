#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class AlarmSystem
{
    private readonly Dictionary<GameObject, int> editorQueuedEnemies = new();
    private readonly Dictionary<GameObject, int> editorCountdownAdditions = new();
    private AlarmWaveComposition editorPreparedWave;
    public bool EditorEditingCountdown => CurrentWaveStage == WaveStage.Countdown && editorPreparedWave != null;
    private Dictionary<GameObject, int> EditorAdditions => EditorEditingCountdown ? editorCountdownAdditions : editorQueuedEnemies;
    public int EditorAddedCount(GameObject prefab) => prefab != null && EditorAdditions.TryGetValue(prefab, out int count) ? count : 0;

    public void EditorAdjustNextWave(GameObject prefab, int delta)
    {
        if (prefab == null || !prefab.CompareTag("Enemy1") || prefab.GetComponent<Health>() == null || delta == 0) return;
        var additions = EditorAdditions;
        int before = EditorAddedCount(prefab);
        int after = (int)Math.Max(0, Math.Min(int.MaxValue, (long)before + delta));
        int change = after - before;
        if (change == 0) return;
        if (after == 0) additions.Remove(prefab); else additions[prefab] = after;
        if (EditorEditingCountdown)
        {
            var entry = editorPreparedWave.units.FindLast(unit => unit.prefab == prefab);
            if (entry == null) { entry = new AlarmWaveUnit { prefab = prefab, count = 0 }; editorPreparedWave.units.Add(entry); }
            entry.count += change;
            directedCounts.TryGetValue(prefab, out int count);
            if (count + change <= 0) directedCounts.Remove(prefab); else directedCounts[prefab] = count + change;
            pendingDirectedEnemies += change;
            if (change > 0) spawnPool?.PlanReserve(prefab, change);
        }
        WaveChanged?.Invoke();
    }
    public void EditorClearNextWaveAdditions()
    {
        foreach (var prefab in new List<GameObject>(EditorAdditions.Keys)) EditorAdjustNextWave(prefab, -EditorAddedCount(prefab));
    }
    private AlarmWaveComposition PrepareEditorWave(AlarmWaveComposition source)
    {
        if (source == null) return null;
        // Never modify the difficulty asset or an earlier wave's list.
        var copy = new AlarmWaveComposition { name = source.name, groups = source.groups };
        foreach (var unit in source.units)
            if (unit != null) copy.units.Add(new AlarmWaveUnit { prefab = unit.prefab, count = unit.count });
        editorCountdownAdditions.Clear();
        foreach (var pair in editorQueuedEnemies)
        {
            copy.units.Add(new AlarmWaveUnit { prefab = pair.Key, count = pair.Value });
            editorCountdownAdditions[pair.Key] = pair.Value;
        }
        editorQueuedEnemies.Clear();
        editorPreparedWave = copy;
        return copy;
    }
}
#endif
