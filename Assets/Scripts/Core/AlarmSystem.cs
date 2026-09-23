using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFoundation.Audio;
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
    [SerializeField] private Color lockedSkullColor = Color.black;
    [SerializeField] private Color unlockedSkullColor = Color.white;
    [SerializeField] private Vector2 thresholdSkullSize = new(32f, 32f);
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
    private Canvas _canvas;
    private RectTransform _canvasRect;

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

        float available = maximumAlarm - currentAlarm - _pendingAlarm;
        float acceptedAmount = Mathf.Min(amount, Mathf.Max(0f, available));
        if (acceptedAmount <= 0f) return;

        _pendingAlarm += acceptedAmount;
        ApplyPreview(Mathf.Clamp01((currentAlarm + _pendingAlarm) / maximumAlarm));

        float alarmUnitsPerOrb = orbSettings != null ? orbSettings.AlarmUnitsPerOrb : 1f;
        int maximumOrbs = orbSettings != null ? orbSettings.MaximumOrbsPerAddition : maximumOrbsPerAddition;
        int orbCount = Mathf.Clamp(Mathf.CeilToInt(acceptedAmount / alarmUnitsPerOrb), 1, maximumOrbs);
        float amountPerOrb = acceptedAmount / orbCount;
        float targetFill = Mathf.Clamp01((currentAlarm + _pendingAlarm * 0.5f) / maximumAlarm);
        for (int i = 0; i < orbCount; i++)
            StartCoroutine(FlyOrb(worldPosition, amountPerOrb, targetFill));
    }

    /// <summary>Устанавливает абсолютное значение тревоги.</summary>
    public void SetAlarm(float value)
    {
        currentAlarm = Mathf.Clamp(value, 0f, maximumAlarm);
        _pendingAlarm = 0f;
        ApplyFill(Mathf.Clamp01(currentAlarm / maximumAlarm));
        ApplyPreview(Mathf.Clamp01(currentAlarm / maximumAlarm));
        OnAlarmChanged?.Invoke(currentAlarm);
        EvaluateThresholds();
    }

    public void ResetAlarm()
    {
        currentAlarm = 0f;
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
            UnityEngine.Object.Instantiate((UnityEngine.Object)entry.Prefab, entry.Position, Quaternion.identity);
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

    private IEnumerator FlyOrb(Vector3 worldPosition, float amount, float targetFill)
    {
        if (_canvasRect == null || fillImage == null)
        {
            CommitIncomingAlarm(amount);
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
            orbImage.color = new Color(1f, 0.22f, 0.16f, 1f);
        }
        orbImage.raycastTarget = false;
        RectTransform orbRect = orb.GetComponent<RectTransform>();
        float configuredSize = spawnedSettings != null ? spawnedSettings.Size : orbSize;
        float configuredDuration = spawnedSettings != null ? spawnedSettings.TotalDuration : orbFlightDuration;
        float configuredSpreadDuration = spawnedSettings != null ? spawnedSettings.SpreadDuration : orbSpreadDuration;
        float configuredLaunchDelay = spawnedSettings != null ? spawnedSettings.LaunchDelayMax : orbLaunchDelayMax;
        float configuredClusterRadius = spawnedSettings != null ? spawnedSettings.ClusterRadius : orbClusterRadius;
        float configuredArcHeight = spawnedSettings != null ? spawnedSettings.ArcHeight : orbArcHeight;
        orbRect.sizeDelta = Vector2.one * configuredSize;

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
        CommitIncomingAlarm(amount);
    }

    private void CommitIncomingAlarm(float amount)
    {
        _pendingAlarm = Mathf.Max(0f, _pendingAlarm - amount);
        currentAlarm = Mathf.Clamp(currentAlarm + amount, 0f, maximumAlarm);
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

    private void CreateThresholdTicks(Transform panel)
    {
        _thresholdSkulls.Clear();
        for (int index = panel.childCount - 1; index >= 0; index--)
        {
            Transform child = panel.GetChild(index);
            if (child.name.StartsWith("Threshold ")) Destroy(child.gameObject);
        }

        foreach (AlarmThreshold threshold in thresholds)
        {
            if (threshold == null) continue;

            GameObject marker = new($"Threshold {threshold.alarmValue:0}", typeof(RectTransform));
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
            tickRect.sizeDelta = new Vector2(3f, uiSize.y + 6f);

            Image tickImage = tick.GetComponent<Image>();
            tickImage.color = new Color(1f, 0.83f, 0.3f, 1f);
            tickImage.raycastTarget = false;

            if (thresholdSkullSprite == null) continue;
            GameObject skull = new("Skull", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            skull.transform.SetParent(marker.transform, false);
            RectTransform skullRect = skull.GetComponent<RectTransform>();
            skullRect.anchorMin = skullRect.anchorMax = new Vector2(0.5f, 0.5f);
            skullRect.pivot = new Vector2(0.5f, 0.5f);
            skullRect.anchoredPosition = new Vector2(0f, thresholdSkullYOffset);
            skullRect.sizeDelta = thresholdSkullSize;
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
            _thresholdSkulls[visualIndex].color = reached ? unlockedSkullColor : lockedSkullColor;
            visualIndex++;
        }
    }
}
