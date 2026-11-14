using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFoundation.Audio;
using GameFoundation.Bestiary;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class AlarmEnemy
{
    public GameObject prefab;
    [Min(1)] public int weight = 1;
}

[Serializable]
public class AlarmThreshold
{
    [Tooltip("0 uses the location default. Enemies still appear on a closed hex.")]
    [Min(0)] public float minimumWaveSpawnDistance;
    [Min(.1f)] public float waveCountdown = 35f;
    public List<AlarmWaveComposition> waves = new();
    [Min(0f)] public float alarmValue = 25f;
    [Min(0.1f)] public float minSpawnInterval = 5f;
    [Min(0.1f)] public float maxSpawnInterval = 10f;
    [Min(1)] public int minEnemiesPerWave = 1;
    [Min(1)] public int maxEnemiesPerWave = 2;
    public List<AlarmEnemy> enemies = new();
}

/// <summary>
/// UI шкала тревоги и бесконечные волны врагов, запускаемые её порогами.
/// Привязывайте UnityEvent&lt;float&gt; к AddAlarm или SetAlarm.
/// </summary>
public partial class AlarmSystem : MonoBehaviour
{
    public static AlarmSystem Instance { get; private set; }

    [Header("Шкала тревоги")]
    private float maximumAlarm => difficultyTable != null ? difficultyTable.GetMaximumAlarm(GameFoundation.MetaProgression.DayCycleService.CurrentPortalDifficulty) : 300f;
    [SerializeField, Min(0f)] private float currentAlarm;
    [SerializeField, Min(0f)] private float fillSpeed = 3f;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image previewImage;
    [SerializeField] private Material incomingFillMaterial;
    [SerializeField] private Transform tickContainer;
    [SerializeField] private Sprite uiSprite;
    [SerializeField] private Sprite thresholdSkullSprite;
    [SerializeField] private Sprite inactiveThresholdSkullSprite;
    [SerializeField] private Sprite[] thresholdDividerSprites = Array.Empty<Sprite>();
    [SerializeField] private float thresholdDividerYOffset;
    [SerializeField] private float thresholdSkullYOffset = -30f;
    [SerializeField] private bool createRuntimeUiWhenMissing = true;
    [SerializeField] private Vector2 uiSize = new(360f, 24f);
    [SerializeField] private Vector2 uiTopOffset = new(0f, -94f);

    [Header("Анимация поступления тревоги")]
    [SerializeField] private Sprite alarmOrbSprite;
    [SerializeField] private AlarmOrbSettings orbSettings;
    [SerializeField, Min(1)] private int maximumOrbsPerAddition = 5;
    [SerializeField, Min(0.2f)] private float orbFlightDuration = 1.7f;
    [SerializeField, Min(0.05f)] private float orbSpreadDuration = 0.35f;
    [SerializeField, Min(0f)] private float orbLaunchDelayMax = 0.12f;
    [SerializeField, Min(0f)] private float orbClusterRadius = 28f;
    [SerializeField, Min(4f)] private float orbArcHeight = 80f;
    [SerializeField, Min(4f)] private float orbSize = 36f;

    [Header("Пороги и волны")]
    // Runtime state only: wave authoring lives exclusively in the referenced asset.
    private List<AlarmThreshold> thresholds = new();
    [SerializeField] private AlarmDifficultyTable difficultyTable;
    [SerializeField, Min(1)] private int nearestBlockedHexesToUse = 8;
    [SerializeField] private Transform mapCenter;
    [SerializeField, Min(0f)] private float spawnPositionJitter = 0.15f;

    [Header("Предупреждение о появлении врагов")]
    [SerializeField] private MonsterSpawnWarningView monsterSpawnWarningPrefab;

    [Header("Уведомление об уровне опасности")]
    [SerializeField] private DangerLevelNotificationView dangerLevelNotificationPrefab;

    [Header("События")]
    public UnityEvent<float> OnAlarmChanged;
    public UnityEvent<AlarmThreshold> OnThresholdReached;

    private readonly List<Coroutine> _waveRoutines = new();
    private readonly HashSet<AlarmThreshold> _activatedThresholds = new();
    private readonly List<GameObject> _activeSpawnWarnings = new();
    private readonly Queue<int> _dangerNotificationQueue = new();
    private Coroutine _dangerNotificationRoutine;
    private AlarmThreshold firstRunWaveTemplate;
    public event Action DangerNotificationsFinished;
    private DangerLevelNotificationView _activeDangerNotification;
    private DangerLevelNotificationView _energyLightNotification;
    private readonly List<Image> _thresholdSkulls = new();
    private float _displayedFill;
    private float _pendingAlarm;
    private int alarmRevision;
    private int reportedEnemyLevel = 1;
    private float passiveAlarmElapsed;
    public const float PassiveAlarmInterval = 5f;
    public const int MaximumEnemyLevel = 50;
    public const float AlarmPerEnemyLevel = 10f;
    public int ActiveDangerLevel => _activatedThresholds.Count;
    public int EnemyLevel => LevelForAlarm(currentAlarm, maximumAlarm);
    public static int LevelForAlarm(float alarm, float maximum) => Mathf.Clamp(
        1 + Mathf.FloorToInt((Mathf.Max(0f, alarm - maximum) + .0001f) / AlarmPerEnemyLevel), 1, MaximumEnemyLevel);
    public event Action<int> EnemyLevelChanged;
    private float MaximumTotalAlarm => maximumAlarm + (MaximumEnemyLevel - 1) * AlarmPerEnemyLevel;
    private void RefreshEnemyLevel(bool notify = true)
    {
        int level = EnemyLevel;
        if (reportedEnemyLevel == level) return;
        bool increased = level > reportedEnemyLevel;
        reportedEnemyLevel = level;
        EnemyLevelChanged?.Invoke(level);
        if (notify && increased) GameFoundation.UI.GameNotifications.Post(
            string.Format(GameFoundation.UI.UnitDescriptionText.Get("enemy.level.notification", GameFoundation.Localization.GameText.Get("Враги достигли уровня {0}")), level),
            GameFoundation.UI.NotificationKind.Negative);
    }
    private readonly Dictionary<UnityEngine.Object, float> _actionAlarmReservations = new();
    private readonly HashSet<UnityEngine.Object> _pausedActionAlarmSources = new();
    private Canvas _canvas;
    private RectTransform _canvasRect;

    // Scene-local registry. Counts change on actual spawning, death and removal, not on wave planning.
    private readonly Dictionary<GameObject, int> _aliveEnemyCounts = new();
    public event Action<GameObject, int> EnemyCountChanged;
    public bool HasSpawnedEnemies { get; private set; }
    public IEnumerable<GameObject> SpawnedEnemyTypes => _aliveEnemyCounts.Keys;
    public int GetAliveEnemyCount(GameObject prefab) => prefab != null && _aliveEnemyCounts.TryGetValue(prefab, out int count) ? count : 0;

    public void RegisterSpawnedEnemy(GameObject prefab, GameObject instance, int waveId = 0)
    {
        if (prefab == null || instance == null || !instance.CompareTag("Enemy1") || instance.GetComponent<Health>() == null) return;
        var rank = instance.GetComponent<GameFoundation.MetaProgression.EnemyLevel>();
        if (rank == null) rank = instance.AddComponent<GameFoundation.MetaProgression.EnemyLevel>();
        rank.Initialize(EnemyLevel);
        BestiaryService.RegisterSpawn(prefab, instance);
        var member = instance.GetComponent<AlarmSpawnedEnemy>();
        if (member == null) member = instance.AddComponent<AlarmSpawnedEnemy>();
        member.Initialize(this, prefab, waveId);
    }

    internal void ChangeEnemyCount(GameObject prefab, int delta)
    {
        if (prefab == null) return;
        int count = Mathf.Max(0, GetAliveEnemyCount(prefab) + delta);
        _aliveEnemyCounts[prefab] = count;
        if (delta > 0) HasSpawnedEnemies = true;
        EnemyCountChanged?.Invoke(prefab, count);
    }

    private void Awake()
    {
        Instance = this;
        if (difficultyTable != null)
            thresholds = difficultyTable.GetThresholds(GameFoundation.MetaProgression.DayCycleService.CurrentPortalDifficulty).ToList();
        if (GameFoundation.MetaProgression.DayCycleService.IsFirstPortalRun)
        {
            firstRunWaveTemplate = thresholds.FirstOrDefault(t => t != null);
            thresholds = new List<AlarmThreshold>();
        }
        _canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindFirstObjectByType<Canvas>();
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
#if UNITY_EDITOR
        if (GetComponent<EditorWaveCheatPopup>() == null) gameObject.AddComponent<EditorWaveCheatPopup>();
#endif
    }

    public IReadOnlyList<AlarmThreshold> ConfiguredThresholds => Application.isPlaying ? thresholds :
        difficultyTable != null ? difficultyTable.GetThresholds(GameFoundation.MetaProgression.DayCycleService.CurrentPortalDifficulty) : Array.Empty<AlarmThreshold>();
    public AlarmDifficultyTable DifficultyTable => difficultyTable;
    public Sprite OrbSprite => orbSettings != null && orbSettings.Image != null ? orbSettings.Image.sprite : alarmOrbSprite != null ? alarmOrbSprite : uiSprite;
    public Color OrbColor => orbSettings != null && orbSettings.Image != null ? orbSettings.Image.color : Color.white;
    public Sprite SkullSprite => thresholdSkullSprite;
    public Sprite InactiveSkullSprite => inactiveThresholdSkullSprite;
    public bool HasIncomingAlarm => _pendingAlarm > 0.001f;

    public int PreviewOrbCount(float amount, float reservedForAction = 0f)
    {
        return OrbCount(AcceptedPreviewAlarm(amount, reservedForAction));
    }

    private int OrbCount(float accepted)
    {
        if (accepted <= 0f) return 0;
        float units = orbSettings != null ? orbSettings.AlarmUnitsPerOrb : 1f;
        int limit = orbSettings != null ? orbSettings.MaximumOrbsPerAddition : maximumOrbsPerAddition;
        return Mathf.Clamp(Mathf.CeilToInt(accepted / units), 1, limit);
    }

    private float AcceptedAlarm(float amount) => Mathf.Min(Mathf.Max(0f, amount),
        Mathf.Max(0f, MaximumTotalAlarm - currentAlarm - _pendingAlarm));

    public float ReservedActionAlarm
    {
        get
        {
            float total = 0f;
            foreach (var reservation in _actionAlarmReservations)
                if (reservation.Key != null) total += reservation.Value;
            return total;
        }
    }

    public void ReserveActionAlarm(UnityEngine.Object source, float amount)
    {
        if (source == null) return;
        _pausedActionAlarmSources.Remove(source);
        if (amount > 0f) _actionAlarmReservations[source] = amount;
        else _actionAlarmReservations.Remove(source);
    }

    public void CancelActionAlarm(UnityEngine.Object source)
    {
        if (!ReferenceEquals(source, null)) _actionAlarmReservations.Remove(source);
        _pausedActionAlarmSources.Remove(source);
    }

    public void SetActionAlarmPaused(UnityEngine.Object source, bool paused)
    {
        if (source == null) return;
        if (paused && _actionAlarmReservations.ContainsKey(source)) _pausedActionAlarmSources.Add(source);
        else _pausedActionAlarmSources.Remove(source);
    }

    /// <summary>Paused cycles retain their reservation for resuming, but do not advance the forecast.</summary>
    public float ActiveReservedActionAlarm
    {
        get
        {
            float total = 0f;
            foreach (var reservation in _actionAlarmReservations)
                if (reservation.Key != null && !_pausedActionAlarmSources.Contains(reservation.Key)) total += reservation.Value;
            return total;
        }
    }

    public void AddAlarmFromAction(float amount, Vector3 position, UnityEngine.Object source)
    {
        // Transfer the forecast into incoming orbs in the same call, without counting both.
        CancelActionAlarm(source);
        AddAlarmFromWorldPosition(amount, position);
    }

    public float GetReservedActionAlarm(UnityEngine.Object source) =>
        source != null && _actionAlarmReservations.TryGetValue(source, out float amount) ? amount : 0f;

    private float PreviewStartAlarm(float reservedForAction) => currentAlarm + _pendingAlarm +
        Mathf.Max(0f, ActiveReservedActionAlarm - Mathf.Max(0f, reservedForAction));
    private float AcceptedPreviewAlarm(float amount, float reservedForAction) => Mathf.Min(Mathf.Max(0f, amount),
        Mathf.Max(0f, MaximumTotalAlarm - PreviewStartAlarm(reservedForAction)));

    public bool PreviewOrbRaisesLevel(float amount, int index, float reservedForAction = 0f)
    {
        int count = PreviewOrbCount(amount, reservedForAction);
        if (index < 0 || index >= count) return false;
        float start = PreviewStartAlarm(reservedForAction);
        float step = AcceptedPreviewAlarm(amount, reservedForAction) / count;
        float before = start + step * index;
        return CrossesWaveThreshold(before, start + step * (index + 1));
    }

    private bool CrossesWaveThreshold(float before, float after)
    {
        foreach (var threshold in thresholds)
            if (threshold != null && threshold.alarmValue > before && threshold.alarmValue <= after &&
                !_activatedThresholds.Contains(threshold)) return true;
        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (spawnPool != null) Destroy(spawnPool.gameObject);
    }

    private WorldSpawnPool spawnPool;

    private void Start()
    {
        cachedMapGenerator = UnityEngine.Object.FindFirstObjectByType<HexMapGenerator>();
        var poolRoot = new GameObject("World Spawn Pool");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(poolRoot, gameObject.scene);
        spawnPool = poolRoot.AddComponent<WorldSpawnPool>();
        var warmed = new HashSet<GameObject>();
        int simultaneousWaves = Mathf.Max(1, thresholds.Count);
        int enemyReserve = Mathf.Max(4, simultaneousWaves);
        foreach (var threshold in thresholds)
            if (threshold.enemies != null) foreach (var enemy in threshold.enemies)
                if (enemy.prefab != null && warmed.Add(enemy.prefab)) spawnPool.Prewarm(enemy.prefab, enemyReserve);
        if (firstRunWaveTemplate?.enemies != null) foreach (var enemy in firstRunWaveTemplate.enemies)
            if (enemy.prefab != null && warmed.Add(enemy.prefab)) spawnPool.Prewarm(enemy.prefab, enemyReserve);
        GameAudioController.Register(this);
        CreateRuntimeUiIfNeeded();
        _displayedFill = Mathf.Clamp01(currentAlarm / maximumAlarm);
        ApplyFill(_displayedFill);
        ApplyPreview(_displayedFill);
        EvaluateThresholds();
        EnsureDirectedWaves();
    }

    private void Update()
    {
        AdvancePassiveAlarm(Time.deltaTime);
        float desiredFill = Mathf.Clamp01(currentAlarm / maximumAlarm);
        _displayedFill = Mathf.MoveTowards(_displayedFill, desiredFill, fillSpeed * Time.deltaTime);
        ApplyFill(_displayedFill);
    }

    private void AdvancePassiveAlarm(float deltaTime)
    {
        if (currentAlarm < maximumAlarm || currentAlarm >= MaximumTotalAlarm)
        { passiveAlarmElapsed = 0f; return; }
        if (deltaTime <= 0f || GameFoundation.MetaProgression.WorldEscapeController.ResultsActive ||
            (GameFoundation.MetaProgression.PortalTowerProgression.Instance != null && GameFoundation.MetaProgression.PortalTowerProgression.Instance.ExpeditionEnded)) return;
        passiveAlarmElapsed += deltaTime;
        int ticks = Mathf.FloorToInt(passiveAlarmElapsed / PassiveAlarmInterval);
        if (ticks == 0) return;
        passiveAlarmElapsed -= ticks * PassiveAlarmInterval;
        float amount = AcceptedAlarm(ticks);
        if (amount <= 0f) return;
        // Ambient danger has no world source: commit it directly, without resource-flight orbs.
        currentAlarm = Mathf.Min(MaximumTotalAlarm, currentAlarm + amount);
        RefreshEnemyLevel();
        OnAlarmChanged?.Invoke(currentAlarm);
    }

    private void OnDisable()
    {
        passiveAlarmElapsed = 0f;
        StopDirectedWaves();
        alarmRevision++;
        _pendingAlarm = 0f;
        _actionAlarmReservations.Clear();
        _pausedActionAlarmSources.Clear();
        foreach (Coroutine routine in _waveRoutines)
            if (routine != null) StopCoroutine(routine);
        _waveRoutines.Clear();
        foreach (GameObject warning in _activeSpawnWarnings)
            if (warning != null) WorldSpawnPool.Release(warning);
        _activeSpawnWarnings.Clear();
        allyDeathNotifications.Clear();
        if (allyDeathNotificationRoutine != null) StopCoroutine(allyDeathNotificationRoutine);
        allyDeathNotificationRoutine = null;
        if (activeAllyDeathNotification != null) Destroy(activeAllyDeathNotification.gameObject);
        activeAllyDeathNotification = null;
        _dangerNotificationQueue.Clear();
        if (_dangerNotificationRoutine != null) StopCoroutine(_dangerNotificationRoutine);
        _dangerNotificationRoutine = null;
        if (_activeDangerNotification != null) Destroy(_activeDangerNotification.gameObject);
        _activeDangerNotification = null;
    }

    /// <summary>Добавляет значение из UnityEvent&lt;float&gt;.</summary>
    public void AddAlarm(float amount)
    {
        AddAlarmFromWorldPosition(amount, transform.position);
    }

    /// <summary>Добавляет тревогу с анимацией полёта от заданной позиции мира.</summary>
    public void AddAlarmFromWorldPosition(float amount, Vector3 worldPosition)
    {
        if (amount <= 0f) return;

        float acceptedAmount = AcceptedAlarm(amount);
        if (acceptedAmount <= 0f) return;

        int orbCount = OrbCount(acceptedAmount);
        float startAlarm = currentAlarm + _pendingAlarm;
        _pendingAlarm += acceptedAmount;
        ApplyPreview(Mathf.Clamp01((currentAlarm + _pendingAlarm) / maximumAlarm));

        float amountPerOrb = acceptedAmount / orbCount;
        float targetFill = Mathf.Clamp01((currentAlarm + _pendingAlarm * 0.5f) / maximumAlarm);
        for (int i = 0; i < orbCount; i++)
        {
            bool reachesThreshold = CrossesWaveThreshold(startAlarm + amountPerOrb * i,
                startAlarm + amountPerOrb * (i + 1));
            StartCoroutine(FlyOrb(worldPosition, amountPerOrb, targetFill, alarmRevision, reachesThreshold));
        }
    }

    /// <summary>Устанавливает абсолютное значение тревоги.</summary>
    public void SetAlarm(float value)
    {
        passiveAlarmElapsed = 0f;
        alarmRevision++;
        currentAlarm = Mathf.Clamp(value, 0f, MaximumTotalAlarm);
        RefreshEnemyLevel();
        _pendingAlarm = 0f;
        ApplyFill(Mathf.Clamp01(currentAlarm / maximumAlarm));
        ApplyPreview(Mathf.Clamp01(currentAlarm / maximumAlarm));
        OnAlarmChanged?.Invoke(currentAlarm);
        EvaluateThresholds();
    }

    public void ResetAlarm()
    {
        passiveAlarmElapsed = 0f;
        _actionAlarmReservations.Clear();
        _pausedActionAlarmSources.Clear();
        alarmRevision++;
        currentAlarm = 0f;
        RefreshEnemyLevel(false);
        _pendingAlarm = 0f;
        ApplyFill(0f);
        ApplyPreview(0f);
        OnAlarmChanged?.Invoke(currentAlarm);
    }

    public bool ReleaseFirstRunWave()
    {
        if (firstRunWaveTemplate == null || thresholds.Count != 0) return false;
        var template = firstRunWaveTemplate;
        continuousFirstRunWaves = true;
        thresholds.Add(new AlarmThreshold
        {
            alarmValue = currentAlarm+_pendingAlarm,
            waveCountdown = template.waveCountdown,
            waves = template.waves,
            minSpawnInterval = template.minSpawnInterval,
            maxSpawnInterval = template.maxSpawnInterval,
            minEnemiesPerWave = template.minEnemiesPerWave,
            maxEnemiesPerWave = template.maxEnemiesPerWave,
            enemies = new List<AlarmEnemy>(template.enemies)
        });
        RefreshThresholdMarkers();
        if(_pendingAlarm<=0)StartCoroutine(DelayedIncomingEvents(currentAlarm,alarmRevision));
        return true;
    }

    private void EvaluateThresholds(float eligibleAlarm=float.PositiveInfinity)
    {
        foreach (AlarmThreshold threshold in ConfiguredThresholds)
        {
            if (threshold == null || _activatedThresholds.Contains(threshold) || Mathf.Min(currentAlarm,eligibleAlarm) < threshold.alarmValue)
                continue;

            _activatedThresholds.Add(threshold);
            OnThresholdReached?.Invoke(threshold);
            EnqueueDangerLevelNotification(thresholds.IndexOf(threshold) + 1);
            EnsureDirectedWaves();
        }
    }

    private void EnqueueDangerLevelNotification(int level)
    {
        GameFoundation.UI.GameNotifications.Post(GameFoundation.Localization.GameText.Get("Уровень тревоги: ") + level, GameFoundation.UI.NotificationKind.Negative);
        if (dangerLevelNotificationPrefab == null) { DangerNotificationsFinished?.Invoke(); return; }
        _dangerNotificationQueue.Enqueue(level);
        if (_dangerNotificationRoutine == null)
            _dangerNotificationRoutine = StartCoroutine(ShowDangerLevelNotifications());
    }

    private IEnumerator ShowDangerLevelNotifications()
    {
        while (_dangerNotificationQueue.Count > 0)
        {
            int level = _dangerNotificationQueue.Dequeue();
            Transform parent = _canvas != null ? _canvas.transform : transform.parent;
            _activeDangerNotification = Instantiate(dangerLevelNotificationPrefab, parent, false);
            var bar = fillImage != null ? fillImage.rectTransform.parent as RectTransform : transform as RectTransform;
            _activeDangerNotification.FollowBar(bar, false);
            yield return _activeDangerNotification.Play(level, thresholds.Count);
            if (_activeDangerNotification != null) Destroy(_activeDangerNotification.gameObject);
            _activeDangerNotification = null;
        }
        _dangerNotificationRoutine = null;
        DangerNotificationsFinished?.Invoke();
    }

    private readonly Queue<Sprite> allyDeathNotifications = new();
    private Coroutine allyDeathNotificationRoutine;
    private DangerLevelNotificationView activeAllyDeathNotification;

    public void ShowAllyDeathNotification(Sprite icon)
    {
        if (dangerLevelNotificationPrefab == null || !isActiveAndEnabled) return;
        allyDeathNotifications.Enqueue(icon);
        if (allyDeathNotificationRoutine == null)
            allyDeathNotificationRoutine = StartCoroutine(PlayAllyDeathNotifications());
    }

    private IEnumerator PlayAllyDeathNotifications()
    {
        var energyBar = FindAnyObjectByType<GameFoundation.MetaProgression.PortalTowerExperienceBar>();
        Transform parent = _canvas != null ? _canvas.transform : transform.parent;
        while (allyDeathNotifications.Count > 0)
        {
            activeAllyDeathNotification = Instantiate(dangerLevelNotificationPrefab, parent, false);
            activeAllyDeathNotification.name = "Ally death notification";
            if (energyBar != null)
                activeAllyDeathNotification.FollowBar((RectTransform)energyBar.transform, true, energyBar.TopCrystal);
            yield return activeAllyDeathNotification.PlayAllyDeath(allyDeathNotifications.Dequeue(), () => _energyLightNotification);
            if (activeAllyDeathNotification != null) Destroy(activeAllyDeathNotification.gameObject);
            activeAllyDeathNotification = null;
        }
        allyDeathNotificationRoutine = null;
    }

    public void ShowEnergyLightNotification(int change, int lightCount, Sprite crystalIcon, RectTransform bar, RectTransform crystal)
    {
        if (change == 0 || dangerLevelNotificationPrefab == null || !isActiveAndEnabled) return;
        if (_energyLightNotification != null)
        {
            _energyLightNotification.gameObject.SetActive(false);
            Destroy(_energyLightNotification.gameObject);
        }
        Transform parent = _canvas != null ? _canvas.transform : transform.parent;
        _energyLightNotification = Instantiate(dangerLevelNotificationPrefab, parent, false);
        _energyLightNotification.name = "Energy Light Notification";
        _energyLightNotification.FollowBar(bar, true, crystal);
        _energyLightNotification.ShowLightChange(change, lightCount, crystalIcon);
    }

    private IEnumerator SpawnWavesForever(AlarmThreshold threshold)
    {
        while (true)
        {
            yield return SpawnWave(threshold);
            float min = Mathf.Min(threshold.minSpawnInterval, threshold.maxSpawnInterval);
            float max = Mathf.Max(threshold.minSpawnInterval, threshold.maxSpawnInterval);
            yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
        }
    }

    private IEnumerator SpawnWave(AlarmThreshold threshold)
    {
        if (threshold.enemies == null || threshold.enemies.Count == 0)
            yield break;

        Vector2 center = GetMapCenter();
        List<HexBlocker> candidates = HexBlocker.ActiveInstances
            .Where(hex => hex.IsBlocked)
            .OrderBy(hex => ((Vector2)hex.transform.position - center).sqrMagnitude)
            .Take(nearestBlockedHexesToUse)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[AlarmSystem] Нет заблокированных гексов для появления врагов.", this);
            yield break;
        }

        int minCount = Mathf.Min(threshold.minEnemiesPerWave, threshold.maxEnemiesPerWave);
        int maxCount = Mathf.Max(threshold.minEnemiesPerWave, threshold.maxEnemiesPerWave);
        int count = UnityEngine.Random.Range(minCount, maxCount + 1);

        var spawnEntries = new List<SpawnEntry>(count);
        var warnings = new Dictionary<HexBlocker, MonsterSpawnWarningView>();
        for (int i = 0; i < count; i++)
        {
            GameObject enemyPrefab = GetWeightedEnemy(threshold.enemies);
            if (enemyPrefab == null) continue;

            HexBlocker hex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnPositionJitter;
            spawnEntries.Add(new SpawnEntry(enemyPrefab, hex, hex.transform.position + (Vector3)offset));
            if (!warnings.ContainsKey(hex)) warnings.Add(hex, CreateSpawnWarning(hex.transform.position));
        }

        if (spawnEntries.Count == 0) yield break;
        yield return AnimateWarningsIn(warnings.Values);
        GameSpeedControls.ResetForWave();

        foreach (SpawnEntry entry in spawnEntries)
        {
            MonsterSpawnWarningView warning = warnings[entry.Hex];
            yield return PrepareMonsterSpawn(warning);
            GameObject enemy = spawnPool.Rent(entry.Prefab, entry.Position, Quaternion.identity);
            RegisterSpawnedEnemy(entry.Prefab, enemy);
            if (warning != null) warning.HideLightImmediate();
            yield return BumpWarning(warning);
        }

        yield return FadeWarningsOut(warnings.Values);
    }

    private MonsterSpawnWarningView CreateSpawnWarning(Vector3 position)
    {
        if (monsterSpawnWarningPrefab == null)
        {
            Debug.LogError("[AlarmSystem] Не назначен префаб эффекта появления монстров.", this);
            return null;
        }
        MonsterSpawnWarningView warning = spawnPool.Rent(monsterSpawnWarningPrefab.gameObject, position, Quaternion.identity).GetComponent<MonsterSpawnWarningView>();
        warning.name = monsterSpawnWarningPrefab.name;
        warning.Prepare();
        _activeSpawnWarnings.Add(warning.gameObject);
        return warning;
    }

    private IEnumerator AnimateWarningsIn(IEnumerable<MonsterSpawnWarningView> warnings)
    {
        float elapsed = 0f;
        float duration = warnings.Where(x => x != null).Select(x => x.AppearDuration).DefaultIfEmpty(0.01f).Max();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (MonsterSpawnWarningView warning in warnings)
            {
                if (warning == null || warning.Renderer == null) continue;
                float t = Mathf.Clamp01(elapsed / warning.AppearDuration);
                float eased = t * t * (3f - 2f * t);
                warning.transform.localScale = Vector3.LerpUnclamped(warning.InitialScale, warning.FinalScale, eased);
                Color color = warning.VisibleColor;
                color.a *= eased;
                warning.Renderer.color = color;
            }
            yield return null;
        }
    }

    private IEnumerator PrepareMonsterSpawn(MonsterSpawnWarningView warning)
    {
        if (warning == null) yield break;

        warning.HideLightImmediate();
        float fadeDuration = Mathf.Min(warning.LightFadeDuration, warning.SpawnDelayAfterAppearance);
        float delayBeforeLight = Mathf.Max(0f, warning.SpawnDelayAfterAppearance - fadeDuration);
        if (delayBeforeLight > 0f) yield return new WaitForSeconds(delayBeforeLight);

        GameAudioController.PlayAt(GameAudioCue.MonsterSpawn, warning.transform.position, 0.78f, 0.94f, 1.05f, 0.05f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            warning.SetLightVisibility(t * t * (3f - 2f * t));
            yield return null;
        }
        warning.SetLightVisibility(1f);
    }

    private IEnumerator BumpWarning(MonsterSpawnWarningView warning)
    {
        if (warning == null) yield break;
        float elapsed = 0f;
        while (elapsed < warning.BumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / warning.BumpDuration);
            float bump = 1f + Mathf.Sin(t * Mathf.PI) * (warning.BumpScale - 1f);
            warning.transform.localScale = warning.FinalScale * bump;
            yield return null;
        }
        if (warning != null) warning.transform.localScale = warning.FinalScale;
    }

    private IEnumerator FadeWarningsOut(IEnumerable<MonsterSpawnWarningView> warnings)
    {
        float elapsed = 0f;
        float duration = warnings.Where(x => x != null).Select(x => x.FadeDuration).DefaultIfEmpty(0.01f).Max();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (MonsterSpawnWarningView warning in warnings)
            {
                if (warning == null || warning.Renderer == null) continue;
                Color color = warning.VisibleColor;
                color.a *= 1f - Mathf.Clamp01(elapsed / warning.FadeDuration);
                warning.Renderer.color = color;
            }
            yield return null;
        }

        foreach (MonsterSpawnWarningView warning in warnings)
        {
            if (warning == null) continue;
            _activeSpawnWarnings.Remove(warning.gameObject);
            WorldSpawnPool.Release(warning.gameObject);
        }
    }

    private readonly struct SpawnEntry
    {
        public readonly GameObject Prefab;
        public readonly HexBlocker Hex;
        public readonly Vector3 Position;

        public SpawnEntry(GameObject prefab, HexBlocker hex, Vector3 position)
        {
            Prefab = prefab;
            Hex = hex;
            Position = position;
        }
    }

    private static GameObject GetWeightedEnemy(List<AlarmEnemy> enemies)
    {
        int totalWeight = enemies.Where(enemy => enemy?.prefab != null).Sum(enemy => Mathf.Max(0, enemy.weight));
        if (totalWeight <= 0) return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (AlarmEnemy enemy in enemies)
        {
            if (enemy?.prefab == null) continue;
            roll -= Mathf.Max(0, enemy.weight);
            if (roll < 0) return enemy.prefab;
        }
        return null;
    }

    private HexMapGenerator cachedMapGenerator;
    private Vector3 GetMapCenter()
    {
        if (mapCenter != null) return mapCenter.position;
        if (cachedMapGenerator == null) cachedMapGenerator = UnityEngine.Object.FindFirstObjectByType<HexMapGenerator>();
        return cachedMapGenerator != null ? cachedMapGenerator.centerPosition : Vector3.zero;
    }

    private void ApplyFill(float value)
    {
        if (fillImage != null) fillImage.fillAmount = value;
        RefreshThresholdSkulls();
    }

    private void ApplyPreview(float value)
    {
        if (previewImage != null) { if(fillImage!=null)previewImage.sprite=fillImage.sprite;previewImage.material=incomingFillMaterial;previewImage.color=Color.white;previewImage.fillAmount = value; }
    }

    private IEnumerator FlyOrb(Vector3 worldPosition, float amount, float targetFill, int revision, bool reachesThreshold)
    {
        if (_canvasRect == null || fillImage == null)
        {
            if (revision == alarmRevision) CommitIncomingAlarm(amount);
            yield break;
        }

        GameObject orb;
        AlarmOrbSettings spawnedSettings = null;
        if (orbSettings != null)
        {
            orb = (GameObject)UnityEngine.Object.Instantiate(orbSettings.gameObject);
            orb.transform.SetParent(_canvasRect, false);
            spawnedSettings = orb.GetComponent<AlarmOrbSettings>();
        }
        else
        {
            orb = new GameObject("Alarm orb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            orb.transform.SetParent(_canvasRect, false);
        }

        Image orbImage = spawnedSettings != null ? spawnedSettings.Image : orb.GetComponent<Image>();
        if (spawnedSettings == null)
        {
            orbImage.sprite = alarmOrbSprite != null ? alarmOrbSprite : uiSprite;
            orbImage.color = Color.white;
        }
        orbImage.raycastTarget = false;
        var skull = reachesThreshold ? thresholdSkullSprite : inactiveThresholdSkullSprite;
        if (skull != null)
        {
            orbImage.sprite = skull;
            orbImage.color = Color.white;
        }
        RectTransform orbRect = orb.GetComponent<RectTransform>();
        orbRect.localScale = Vector3.one;
        float configuredSize = spawnedSettings != null ? spawnedSettings.Size : orbSize;
        float configuredDuration = spawnedSettings != null ? spawnedSettings.TotalDuration : orbFlightDuration;
        float configuredSpreadDuration = spawnedSettings != null ? spawnedSettings.SpreadDuration : orbSpreadDuration;
        float configuredLaunchDelay = spawnedSettings != null ? spawnedSettings.LaunchDelayMax : orbLaunchDelayMax;
        float configuredClusterRadius = spawnedSettings != null ? spawnedSettings.ClusterRadius : orbClusterRadius;
        float configuredArcHeight = spawnedSettings != null ? spawnedSettings.ArcHeight : orbArcHeight;
        orbRect.sizeDelta = orbImage.sprite != null ? orbImage.sprite.rect.size * 2f : Vector2.one * configuredSize;
        orbImage.preserveAspect = true;

        Camera cameraForCanvas = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        Vector2 screenStart = Camera.main != null ? (Vector2)Camera.main.WorldToScreenPoint(worldPosition) : (Vector2)fillImage.rectTransform.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenStart, cameraForCanvas, out Vector2 start);
        Vector3 targetWorld = fillImage.rectTransform.TransformPoint(new Vector3(
            Mathf.Lerp(-fillImage.rectTransform.rect.width * 0.5f, fillImage.rectTransform.rect.width * 0.5f, targetFill), 0f, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, targetWorld, cameraForCanvas, out Vector2 target);

        Vector2 clusterPosition = start + UnityEngine.Random.insideUnitCircle * configuredClusterRadius;
        float spreadDuration = Mathf.Min(configuredSpreadDuration, configuredDuration * 0.45f);
        float arcDuration = Mathf.Max(0.05f, configuredDuration - spreadDuration);
        Vector2 control = (clusterPosition + target) * 0.5f + new Vector2(UnityEngine.Random.Range(-40f, 40f), configuredArcHeight + UnityEngine.Random.Range(-20f, 20f));

        float elapsed = 0f;
        while (elapsed < spreadDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spreadDuration);
            orbRect.anchoredPosition = Vector2.Lerp(start, clusterPosition, t * t * (3f - 2f * t));
            yield return null;
        }

        float launchDelay = UnityEngine.Random.Range(0f, configuredLaunchDelay);
        if (launchDelay > 0f) yield return new WaitForSeconds(launchDelay);

        elapsed = 0f;
        arcDuration = Mathf.Max(0.05f, arcDuration - launchDelay);
        while (elapsed < arcDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcDuration);
            float easedT = t * t * (3f - 2f * t);
            orbRect.anchoredPosition = Vector2.Lerp(Vector2.Lerp(clusterPosition, control, easedT), Vector2.Lerp(control, target, easedT), easedT);
            orbRect.localScale = Vector3.one;
            yield return null;
        }

        Destroy(orb);
        if (revision == alarmRevision) CommitIncomingAlarm(amount);
    }

    private void CommitIncomingAlarm(float amount)
    {
        _pendingAlarm = Mathf.Max(0f, _pendingAlarm - amount);
        currentAlarm = Mathf.Clamp(currentAlarm + amount, 0f, MaximumTotalAlarm);
        float value = Mathf.Clamp01(currentAlarm / maximumAlarm);
        _displayedFill=value;
        ApplyFill(value);
        ApplyPreview(Mathf.Clamp01((currentAlarm + _pendingAlarm) / maximumAlarm));
        OnAlarmChanged?.Invoke(currentAlarm);
        StartCoroutine(DelayedIncomingEvents(currentAlarm,alarmRevision));
    }
    private IEnumerator DelayedIncomingEvents(float arrivedAlarm,int revision)
    {
        yield return new WaitForSecondsRealtime(.5f);
        if(revision!=alarmRevision || !isActiveAndEnabled)yield break;
        RefreshEnemyLevel();
        EvaluateThresholds(arrivedAlarm);
    }

    private void CreateRuntimeUiIfNeeded()
    {
        if (fillImage != null)
        {
            CreateThresholdTicks(tickContainer != null ? tickContainer : fillImage.transform.parent);
            return;
        }
        if (!createRuntimeUiWhenMissing) return;
        Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new("Alarm bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = uiTopOffset;
        panelRect.sizeDelta = uiSize;
        Image background = panel.GetComponent<Image>();
        background.sprite = uiSprite;
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        background.raycastTarget = false;

        GameObject fill = new("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(panel.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = new Vector2(3f, 3f);
        fillImage = fill.GetComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.color = new Color(0.86f, 0.2f, 0.13f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.raycastTarget = false;

        CreateThresholdTicks(panel.transform);
    }

    [ContextMenu("Refresh Threshold Markers")]
    public void RefreshThresholdMarkers()
    {
        if (tickContainer != null) CreateThresholdTicks(tickContainer);
    }

    private void CreateThresholdTicks(Transform panel)
    {
        _thresholdSkulls.Clear();
        for (int index = panel.childCount - 1; index >= 0; index--)
        {
            Transform child = panel.GetChild(index);
            if (!child.TryGetComponent<AlarmThresholdMarker>(out var mark) || mark.owner != this) continue;
            // Authored markers make the prefab editable; replace them with the current difficulty's thresholds.
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        foreach (AlarmThreshold threshold in ConfiguredThresholds)
        {
            if (threshold == null) continue;

            GameObject marker = new($"Threshold {threshold.alarmValue:0}", typeof(RectTransform));
            marker.AddComponent<AlarmThresholdMarker>().owner = this;
            marker.transform.SetParent(panel, false);
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            float normalizedValue = Mathf.Clamp01(threshold.alarmValue / maximumAlarm);
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(normalizedValue, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = Vector2.zero;

            GameObject tick = new("Tick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tick.transform.SetParent(marker.transform, false);
            RectTransform tickRect = tick.GetComponent<RectTransform>();
            Image tickImage = tick.GetComponent<Image>();
            if (thresholdDividerSprites.Length > 0)
                tickImage.sprite = thresholdDividerSprites[Mathf.Min(_thresholdSkulls.Count, thresholdDividerSprites.Length - 1)];
            tickRect.sizeDelta = tickImage.sprite != null ? tickImage.sprite.rect.size * 2f : new Vector2(3f, uiSize.y + 6f);
            tickRect.anchoredPosition = new Vector2(0f, thresholdDividerYOffset);
            tickImage.color = Color.white;
            tickImage.raycastTarget = false;

            if (thresholdSkullSprite == null) continue;
            GameObject skull = new("Skull", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            skull.transform.SetParent(marker.transform, false);
            RectTransform skullRect = skull.GetComponent<RectTransform>();
            skullRect.anchorMin = skullRect.anchorMax = new Vector2(0.5f, 0.5f);
            skullRect.pivot = new Vector2(0.5f, 0.5f);
            skullRect.anchoredPosition = new Vector2(0f, thresholdSkullYOffset);
            skullRect.sizeDelta = thresholdSkullSprite.rect.size * 2f;
            Image skullImage = skull.GetComponent<Image>();
            skullImage.sprite = thresholdSkullSprite;
            skullImage.preserveAspect = true;
            skullImage.raycastTarget = false;
            _thresholdSkulls.Add(skullImage);
        }
        RefreshThresholdSkulls();
    }

    private void RefreshThresholdSkulls()
    {
        int visualIndex = 0;
        foreach (AlarmThreshold threshold in ConfiguredThresholds)
        {
            if (threshold == null) continue;
            if (visualIndex >= _thresholdSkulls.Count) break;
            bool reached = currentAlarm >= threshold.alarmValue;
            Image skull = _thresholdSkulls[visualIndex];
            skull.sprite = reached ? thresholdSkullSprite : inactiveThresholdSkullSprite;
            skull.color = Color.white;
            skull.enabled = skull.sprite != null;
            if (skull.sprite != null) skull.rectTransform.sizeDelta = skull.sprite.rect.size * 2f;
            visualIndex++;
        }
    }
}
