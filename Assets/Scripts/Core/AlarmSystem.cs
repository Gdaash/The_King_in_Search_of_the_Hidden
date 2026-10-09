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
public class AlarmSystem : MonoBehaviour
{
    public static AlarmSystem Instance { get; private set; }

    [Header("Шкала тревоги")]
    [SerializeField, Min(1f)] private float maximumAlarm = 100f;
    [SerializeField, Min(0f)] private float currentAlarm;
    [SerializeField, Min(0f)] private float fillSpeed = 3f;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image previewImage;
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
    [SerializeField] private List<AlarmThreshold> thresholds = new();
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
    private DangerLevelNotificationView _activeDangerNotification;
    private readonly List<Image> _thresholdSkulls = new();
    private float _displayedFill;
    private float _pendingAlarm;
    private int alarmRevision;
    private int reportedEnemyLevel = 1;
    public const int MaximumEnemyLevel = 50;
    public const float AlarmPerEnemyLevel = 10f;
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
            string.Format(GameFoundation.UI.UnitDescriptionText.Get("enemy.level.notification", "Враги достигли уровня {0}"), level),
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

    public void RegisterSpawnedEnemy(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null || !instance.CompareTag("Enemy1") || instance.GetComponent<Health>() == null) return;
        var rank = instance.GetComponent<GameFoundation.MetaProgression.EnemyLevel>();
        if (rank == null) rank = instance.AddComponent<GameFoundation.MetaProgression.EnemyLevel>();
        rank.Initialize(EnemyLevel);
        BestiaryService.RegisterSpawn(prefab, instance);
        var member = instance.GetComponent<AlarmSpawnedEnemy>();
        if (member == null) member = instance.AddComponent<AlarmSpawnedEnemy>();
        member.Initialize(this, prefab);
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
        _canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindFirstObjectByType<Canvas>();
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
    }

    public IReadOnlyList<AlarmThreshold> ConfiguredThresholds => thresholds;
    public AlarmDifficultyTable DifficultyTable => difficultyTable;
    public Sprite OrbSprite => orbSettings != null && orbSettings.Image != null ? orbSettings.Image.sprite : alarmOrbSprite != null ? alarmOrbSprite : uiSprite;
    public Color OrbColor => orbSettings != null && orbSettings.Image != null ? orbSettings.Image.color : Color.white;
    public Sprite SkullSprite => thresholdSkullSprite;
    public Sprite InactiveSkullSprite => inactiveThresholdSkullSprite;

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
        float after = start + step * (index + 1);
        foreach (var threshold in thresholds)
            if (threshold != null && threshold.alarmValue > before && threshold.alarmValue <= after &&
                !_activatedThresholds.Contains(threshold)) return true;
        return LevelForAlarm(after, maximumAlarm) > LevelForAlarm(before, maximumAlarm);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        CreateRuntimeUiIfNeeded();
        _displayedFill = Mathf.Clamp01(currentAlarm / maximumAlarm);
        ApplyFill(_displayedFill);
        ApplyPreview(_displayedFill);
        EvaluateThresholds();
    }

    private void Update()
    {
        float desiredFill = Mathf.Clamp01(currentAlarm / maximumAlarm);
        _displayedFill = Mathf.MoveTowards(_displayedFill, desiredFill, fillSpeed * Time.deltaTime);
        ApplyFill(_displayedFill);
    }

    private void OnDisable()
    {
        alarmRevision++;
        _pendingAlarm = 0f;
        _actionAlarmReservations.Clear();
        _pausedActionAlarmSources.Clear();
        foreach (Coroutine routine in _waveRoutines)
            if (routine != null) StopCoroutine(routine);
        _waveRoutines.Clear();
        foreach (GameObject warning in _activeSpawnWarnings)
            if (warning != null) Destroy(warning);
        _activeSpawnWarnings.Clear();
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
        _pendingAlarm += acceptedAmount;
        ApplyPreview(Mathf.Clamp01((currentAlarm + _pendingAlarm) / maximumAlarm));

        float amountPerOrb = acceptedAmount / orbCount;
        float targetFill = Mathf.Clamp01((currentAlarm + _pendingAlarm * 0.5f) / maximumAlarm);
        for (int i = 0; i < orbCount; i++)
            StartCoroutine(FlyOrb(worldPosition, amountPerOrb, targetFill, alarmRevision));
    }

    /// <summary>Устанавливает абсолютное значение тревоги.</summary>
    public void SetAlarm(float value)
    {
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

    private void EvaluateThresholds()
    {
        foreach (AlarmThreshold threshold in thresholds)
        {
            if (threshold == null || _activatedThresholds.Contains(threshold) || currentAlarm < threshold.alarmValue)
                continue;

            _activatedThresholds.Add(threshold);
            OnThresholdReached?.Invoke(threshold);
            EnqueueDangerLevelNotification(thresholds.IndexOf(threshold) + 1);
            _waveRoutines.Add(StartCoroutine(SpawnWavesForever(threshold)));
        }
    }

    private void EnqueueDangerLevelNotification(int level)
    {
        GameFoundation.UI.GameNotifications.Post("Уровень тревоги: " + level, GameFoundation.UI.NotificationKind.Negative);
        if (dangerLevelNotificationPrefab == null) return;
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
            yield return _activeDangerNotification.Play(level, thresholds.Count);
            if (_activeDangerNotification != null) Destroy(_activeDangerNotification.gameObject);
            _activeDangerNotification = null;
        }
        _dangerNotificationRoutine = null;
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

        List<HexBlocker> candidates = UnityEngine.Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None)
            .Where(hex => hex.IsBlocked)
            .OrderBy(hex => ((Vector2)hex.transform.position - (Vector2)GetMapCenter()).sqrMagnitude)
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

        foreach (SpawnEntry entry in spawnEntries)
        {
            MonsterSpawnWarningView warning = warnings[entry.Hex];
            yield return PrepareMonsterSpawn(warning);
            GameObject enemy = Instantiate(entry.Prefab, entry.Position, Quaternion.identity);
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
        MonsterSpawnWarningView warning = Instantiate(monsterSpawnWarningPrefab, position, Quaternion.identity);
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
            Destroy(warning.gameObject);
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

    private Vector3 GetMapCenter()
    {
        if (mapCenter != null) return mapCenter.position;
        HexMapGenerator generator = UnityEngine.Object.FindFirstObjectByType<HexMapGenerator>();
        return generator != null ? generator.centerPosition : Vector3.zero;
    }

    private void ApplyFill(float value)
    {
        if (fillImage != null) fillImage.fillAmount = value;
        RefreshThresholdSkulls();
    }

    private void ApplyPreview(float value)
    {
        if (previewImage != null) previewImage.fillAmount = value;
    }

    private IEnumerator FlyOrb(Vector3 worldPosition, float amount, float targetFill, int revision)
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
        RectTransform orbRect = orb.GetComponent<RectTransform>();
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
            orbRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, easedT);
            yield return null;
        }

        Destroy(orb);
        if (revision == alarmRevision) CommitIncomingAlarm(amount);
    }

    private void CommitIncomingAlarm(float amount)
    {
        _pendingAlarm = Mathf.Max(0f, _pendingAlarm - amount);
        currentAlarm = Mathf.Clamp(currentAlarm + amount, 0f, MaximumTotalAlarm);
        RefreshEnemyLevel();
        float value = Mathf.Clamp01(currentAlarm / maximumAlarm);
        ApplyFill(value);
        ApplyPreview(Mathf.Clamp01((currentAlarm + _pendingAlarm) / maximumAlarm));
        OnAlarmChanged?.Invoke(currentAlarm);
        EvaluateThresholds();
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

        foreach (AlarmThreshold threshold in thresholds)
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
        foreach (AlarmThreshold threshold in thresholds)
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
