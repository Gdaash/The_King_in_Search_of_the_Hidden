using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class AlarmWaveUnit
{
    public GameObject prefab;
    [Min(1)] public int count = 1;
}

[Serializable]
public sealed class AlarmWaveComposition
{
    public string name;
    [Range(1, 3)] public int groups = 1;
    public List<AlarmWaveUnit> units = new();
}

public partial class AlarmSystem
{
    public enum WaveStage { Dormant, Countdown, Attacking, Rest }
    [Header("Последовательные волны")]
    [SerializeField, Min(0f)] private float waveRestDuration = 5f;
    [SerializeField, Min(.1f)] private float waveClusterRadius = .65f;
    [SerializeField, Range(0f, 1f)] private float waveRelocationChance = .4f;
    public WaveStage CurrentWaveStage { get; private set; }
    public float WaveSecondsRemaining { get; private set; }
    public float WavePreparationDuration { get; private set; }
    public int WaveNumber { get; private set; }
    public static int ProvokeRewardForSeconds(float seconds) => Mathf.FloorToInt(Mathf.Max(0, seconds) / 10f);
    public int ProvokeReward => CurrentWaveStage == WaveStage.Countdown ? ProvokeRewardForSeconds(WaveSecondsRemaining) * Mathf.Max(1, _activatedThresholds.Count) : 0;
    public bool CanProvoke => isActiveAndEnabled && CurrentWaveStage == WaveStage.Countdown && ProvokeReward > 0 && !GameFoundation.MetaProgression.WorldEscapeController.ResultsActive &&
        !GameFoundation.MetaProgression.FirstRunLightTutorial.BlocksInput && !GameFoundation.MetaProgression.PortalCloudTransition.IsLoading &&
        GameFoundation.MetaProgression.PortalTowerProgression.Instance != null && GameFoundation.MetaProgression.PortalTowerProgression.Instance.CanReceiveWaveReward;
    public bool TryProvokeWave()
    {
        if (!CanProvoke) return false;
        int reward = ProvokeReward;
        // Claim the countdown before granting experience: a second click cannot grant it again.
        WaveSecondsRemaining = 0;
        GameFoundation.MetaProgression.PortalTowerProgression.Instance.AwardWaveCrystals(reward);
        WaveChanged?.Invoke();
        return true;
    }
    public event Action WaveChanged;
    public IEnumerable<GameObject> CurrentWaveEnemyTypes => directedCounts.Keys;
    public int GetWaveEnemyCount(GameObject prefab) => prefab != null && directedCounts.TryGetValue(prefab, out int count) ? count : 0;
    private readonly Dictionary<GameObject, int> directedCounts = new();
    private readonly Dictionary<AlarmThreshold, int> compositionIndices = new();
    private readonly List<DirectedSpawnGroup> directedGroups = new();
    private Coroutine directedRoutine;
    private int directedWaveId;
    private bool continuousFirstRunWaves;
    private int pendingDirectedEnemies;
    private float nextTelegraphRefresh;
    private bool hasWaveDirection, avoidPreviousSpawnPoints;
    private float waveDirectionAngle;
    private readonly List<Vector3> previousSpawnPoints = new();
    private void OnEnable() { if (spawnPool != null) EnsureDirectedWaves(); }

    private sealed class DirectedSpawnGroup
    {
        public Vector2 direction;
        public Vector3 position;
        public WaveSpawnTelegraph visual;
    }

    private void EnsureDirectedWaves()
    {
        if (spawnPool != null && directedRoutine == null && isActiveAndEnabled)
            directedRoutine = StartCoroutine(DirectedWaves());
    }

    private AlarmThreshold CurrentWaveThreshold()
    {
        AlarmThreshold selected = null;
        foreach (var threshold in thresholds)
            if (threshold != null && _activatedThresholds.Contains(threshold) &&
                (selected == null || threshold.alarmValue > selected.alarmValue)) selected = threshold;
        return selected;
    }

    private IEnumerator DirectedWaves()
    {
        while (isActiveAndEnabled)
        {
            var threshold = CurrentWaveThreshold();
            if (threshold == null) { yield return null; continue; }
            var composition = SelectComposition(threshold);
#if UNITY_EDITOR
            composition = PrepareEditorWave(composition);
#endif
            if (composition == null || composition.units.Count == 0) { yield return new WaitForSeconds(1); continue; }
            if (!continuousFirstRunWaves)
            {
                directedCounts.Clear();
                pendingDirectedEnemies = 0;
            }
            foreach (var unit in composition.units)
            {
                if (unit.prefab == null || unit.count <= 0) continue;
                directedCounts.TryGetValue(unit.prefab, out int previous);
                directedCounts[unit.prefab] = previous + unit.count;
                pendingDirectedEnemies += unit.count;
            }
            if (pendingDirectedEnemies == 0) { yield return new WaitForSeconds(1); continue; }
            foreach (var group in composition.units.Where(unit => unit.prefab != null && unit.count > 0).GroupBy(unit => unit.prefab))
                spawnPool.PlanReserve(group.Key, group.Sum(unit => unit.count));
            WaveNumber++;
            directedWaveId++;
            CreateDirectedGroups(composition.groups);
            CurrentWaveStage = WaveStage.Countdown;
            WavePreparationDuration = Mathf.Max(.1f, threshold.waveCountdown);
            WaveSecondsRemaining = WavePreparationDuration;
            WaveChanged?.Invoke();
            while (WaveSecondsRemaining > 0)
            {
                if (Time.time >= nextTelegraphRefresh)
                {
                    RefreshDirectedGroups();
                    nextTelegraphRefresh = Time.time + .2f;
                }
                WaveSecondsRemaining = Mathf.Max(0, WaveSecondsRemaining - Time.deltaTime);
                yield return null;
            }
            RefreshDirectedGroups();
            CurrentWaveStage = WaveStage.Attacking;
            GameSpeedControls.ResetForWave();
#if UNITY_EDITOR
            editorPreparedWave = null;
            editorCountdownAdditions.Clear();
#endif
            WaveChanged?.Invoke();
            // A group appears together. Small batches only distribute initialization over frames.
            int ordinal = 0;
            foreach (var unit in composition.units)
            {
                if (unit.prefab == null) continue;
                for (int i = 0; i < unit.count; i++)
                {
                    var group = directedGroups[ordinal % directedGroups.Count];
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * waveClusterRadius;
                    var instance = spawnPool.Rent(unit.prefab, group.position + (Vector3)offset, Quaternion.identity);
                    RegisterSpawnedEnemy(unit.prefab, instance, directedWaveId);
                    if (++ordinal % 8 == 0) yield return null;
                }
            }
            previousSpawnPoints.Clear();
            foreach (var group in directedGroups) previousSpawnPoints.Add(group.position);
            ClearDirectedGroups();
            // The first expedition keeps its 35-second cadence even if enemies survive.
            if (continuousFirstRunWaves) continue;
            while (pendingDirectedEnemies > 0) yield return null;
            CurrentWaveStage = WaveStage.Rest;
            WaveSecondsRemaining = 0;
            WaveChanged?.Invoke();
            yield return new WaitForSeconds(waveRestDuration);
        }
    }

    private AlarmWaveComposition SelectComposition(AlarmThreshold threshold)
    {
        compositionIndices.TryGetValue(threshold, out int index);
        compositionIndices[threshold] = index + 1;
        if (threshold.waves != null && threshold.waves.Count > 0)
            return threshold.waves[index % threshold.waves.Count];
        // Compatibility for existing authored/test tables until explicit compositions are set.
        var result = new AlarmWaveComposition { name = "Wave", groups = 1 };
        var entries = threshold.enemies;
        if (entries == null || entries.Count == 0) return result;
        int total = Mathf.Max(1, threshold.minEnemiesPerWave + (index % Mathf.Max(1, threshold.maxEnemiesPerWave - threshold.minEnemiesPerWave + 1)));
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i]?.prefab == null || entries[i].weight <= 0) continue;
            int count = total / entries.Count + (i < total % entries.Count ? 1 : 0);
            if (count > 0) result.units.Add(new AlarmWaveUnit { prefab = entries[(i + index) % entries.Count].prefab, count = count });
        }
        return result;
    }

    internal void RemoveDirectedWaveEnemy(int waveId, GameObject prefab)
    {
        if ((continuousFirstRunWaves ? waveId <= 0 || waveId > directedWaveId : waveId != directedWaveId) ||
            !directedCounts.TryGetValue(prefab, out int count) || count <= 0) return;
        directedCounts[prefab] = count - 1;
        pendingDirectedEnemies = Mathf.Max(0, pendingDirectedEnemies - 1);
        WaveChanged?.Invoke();
    }

    private void CreateDirectedGroups(int count)
    {
        ClearDirectedGroups();
        avoidPreviousSpawnPoints = hasWaveDirection && UnityEngine.Random.value < waveRelocationChance;
        if (!hasWaveDirection) waveDirectionAngle = UnityEngine.Random.Range(0f, 360f);
        else if (avoidPreviousSpawnPoints) waveDirectionAngle = Mathf.Repeat(waveDirectionAngle + UnityEngine.Random.Range(45f, 315f), 360f);
        hasWaveDirection = true;
        float angle = waveDirectionAngle;
        for (int i = 0; i < Mathf.Clamp(count, 1, 3); i++)
        {
            float radians = (angle + i * 360f / Mathf.Clamp(count, 1, 3)) * Mathf.Deg2Rad;
            var group = new DirectedSpawnGroup { direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) };
            var go = new GameObject("Wave spawn telegraph");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            group.visual = go.AddComponent<WaveSpawnTelegraph>();
            directedGroups.Add(group);
        }
        RefreshDirectedGroups();
    }

    private void RefreshDirectedGroups()
    {
        Vector2 center = GetMapCenter();
        float minimumDistance = difficultyTable != null ? difficultyTable.GetMinimumWaveSpawnDistance(GameFoundation.MetaProgression.DayCycleService.CurrentPortalDifficulty) : 0;
        var threshold = CurrentWaveThreshold();
        if (threshold != null && threshold.minimumWaveSpawnDistance > 0) minimumDistance = threshold.minimumWaveSpawnDistance;
        foreach (var group in directedGroups)
        {
            HexBlocker best = null;
            float score = float.PositiveInfinity, outerRadius = 3;
            foreach (var hex in HexBlocker.ActiveInstances)
            {
                if (hex == null) continue;
                Vector2 delta = (Vector2)hex.transform.position - center;
                float distance = delta.magnitude;
                outerRadius = Mathf.Max(outerRadius, distance);
                if (!hex.IsBlocked || distance < Mathf.Max(.1f, minimumDistance) || Vector2.Dot(delta / distance, group.direction) < .9f) continue;
                if (avoidPreviousSpawnPoints && WasPreviousSpawnPoint(hex.transform.position)) continue;
                // Prefer the frontier in this same direction; never move the warning across the map.
                float candidate = distance + (1 - Vector2.Dot(delta / distance, group.direction)) * 8;
                if (candidate < score) { best = hex; score = candidate; }
            }
            group.position = best != null ? best.transform.position : (Vector3)(center + group.direction * Mathf.Max(minimumDistance, outerRadius + 2));
            if (group.visual != null) group.visual.transform.position = group.position;
        }
    }

    private bool WasPreviousSpawnPoint(Vector3 position)
    {
        foreach (var previous in previousSpawnPoints)
            if ((position - previous).sqrMagnitude < .01f) return true;
        return false;
    }

    private void ClearDirectedGroups()
    {
        foreach (var group in directedGroups) if (group.visual != null) Destroy(group.visual.gameObject);
        directedGroups.Clear();
    }

    private void StopDirectedWaves()
    {
#if UNITY_EDITOR
        editorPreparedWave = null;
        editorQueuedEnemies.Clear();
        editorCountdownAdditions.Clear();
#endif
        if (directedRoutine != null) StopCoroutine(directedRoutine);
        directedRoutine = null;
        continuousFirstRunWaves = false;
        directedWaveId++;
        directedCounts.Clear();
        pendingDirectedEnemies = 0;
        hasWaveDirection = false;
        previousSpawnPoints.Clear();
        ClearDirectedGroups();
        CurrentWaveStage = WaveStage.Dormant;
        WaveChanged?.Invoke();
    }
}
