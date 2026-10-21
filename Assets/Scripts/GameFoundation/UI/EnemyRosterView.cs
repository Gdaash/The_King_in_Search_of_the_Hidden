using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Scene-local HUD built from the active alarm's spawn configuration.</summary>
    public sealed class EnemyRosterView : MonoBehaviour
    {
        [Header("Источник и описания")]
        [SerializeField] private AlarmSystem alarm;
        [SerializeField] private UnitDescriptionCatalog descriptions;
        [SerializeField] private Sprite fallbackPortrait;
        [Header("Префабы и вёрстка")]
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform rows;
        [SerializeField] private EnemyRosterItemView itemTemplate;
        [SerializeField] private UnitDescriptionTooltip detailsTooltip;
        [SerializeField, Min(1)] private int maximumVisibleRows = 5;
        [SerializeField, Min(32f)] private float rowHeight = 76f;
        [SerializeField, Min(0f)] private float rowSpacing = 4f;
        [SerializeField, Min(0f)] private float headerAndPadding = 84f;
        [SerializeField] private GameFoundation.MetaProgression.MilitaryStarsView levelStars;
        [Header("Отсчёт волны")]
        [SerializeField, Min(18)] private int waveTimerFontSize = 27;
        [SerializeField] private Color waveAttackColor = new(.82f, .43f, .49f, 1f);
        [SerializeField] private WaveProvokeButton provokeButton;
        private bool ProvokeVisible => provokeButton != null && provokeButton.gameObject.activeSelf;
        private bool lastProvokeVisible;
        private float WaveHeaderHeight => Mathf.Max(headerAndPadding, ProvokeVisible ? 228f : 144f);
        private readonly Dictionary<GameObject, EnemyRosterItemView> entries = new();
        private readonly List<UnitDescriptionDefinition> generatedDescriptions = new();
        private AlarmSystem boundAlarm;
        private bool subscribed;
        private bool started;
        private UnityEngine.UI.Text waveTimer;
        private string timerLanguage;
        private int displayedSeconds = -1;
        private int displayedDigitSize = -1;
        private float compactAmount;
        public void SetCompact(bool compact)
            => SetCompactAmount(compact ? 1 : 0);
        public void SetCompactAmount(float amount)
        {
            amount=Mathf.Clamp01(amount);
            compactAmount = amount;
            if(rows)
            {
                var scroll=rows.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
                var content=scroll ? scroll.gameObject : rows.gameObject;
                content.SetActive(amount<.9999f);
                var group=content.GetComponent<CanvasGroup>();
                if(!group)group=content.AddComponent<CanvasGroup>();
                group.alpha=1-amount;group.blocksRaycasts=amount<.01f;
            }
            int visibleRows = 0;
            foreach (var entry in entries.Values) if (entry.gameObject.activeSelf) visibleRows++;
            float listHeight=Mathf.Min(maximumVisibleRows,visibleRows)*(rowHeight+rowSpacing)-(visibleRows>0?rowSpacing:0);
            if(panel) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,WaveHeaderHeight+listHeight*(1-amount));
        }

        private void Awake() => SetVisible(false);
        private void OnEnable() { if (started) Bind(); }
        // All AlarmSystem.Awake calls have selected the current location by Start.
        private void Start() { started = true; Bind(); }
        private void OnDisable()
        {
            if (boundAlarm != null && subscribed) { boundAlarm.EnemyLevelChanged -= RefreshLevel; boundAlarm.WaveChanged -= RefreshWave; }
            subscribed = false;
            if (detailsTooltip != null) detailsTooltip.Hide(null);
        }
        private void OnDestroy()
        {
            foreach (var data in generatedDescriptions) if (data != null) Destroy(data);
        }

        private void Bind()
        {
            AlarmSystem source = alarm != null ? alarm : AlarmSystem.Instance;
            if (source == null || itemTemplate == null || rows == null) return;
            if (boundAlarm != null && subscribed) { boundAlarm.EnemyLevelChanged -= RefreshLevel; boundAlarm.WaveChanged -= RefreshWave; }
            boundAlarm = source;
            itemTemplate.gameObject.SetActive(false);
            source.WaveChanged += RefreshWave;
            source.EnemyLevelChanged += RefreshLevel;
            RefreshLevel(source.EnemyLevel);
            subscribed = true;
            EnsureWaveTimer();
            if (provokeButton != null) provokeButton.Bind(source);
            ApplyWaveHeaderLayout();
            RefreshWave();
        }

        private void RefreshLevel(int level) { if (levelStars != null) levelStars.SetStars(level, AlarmSystem.MaximumEnemyLevel); }

        private void EnsureWaveTimer()
        {
            if (waveTimer != null || panel == null) return;
            foreach (var text in panel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.name == "Subtitle") { waveTimer = text; break; }
            if (waveTimer != null)
            {
                var binding = waveTimer.GetComponent<GameFoundation.Localization.LocalizedText>();
                if (binding != null) binding.enabled = false;
                ConfigureWaveTimer();
                return;
            }
            var go = new GameObject("Wave countdown", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            waveTimer = go.AddComponent<UnityEngine.UI.Text>();
            var source = GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (source != null && source != waveTimer) waveTimer.font = source.font;
            ConfigureWaveTimer();
        }
        private void ConfigureWaveTimer()
        {
            int titleSize = 20;
            foreach (var text in panel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.name == "Title") titleSize = text.fontSize;
            waveTimer.fontSize = Mathf.Max(waveTimerFontSize, Mathf.CeilToInt(titleSize * 1.15f));
            waveTimer.supportRichText = true;
            waveTimer.resizeTextForBestFit = false;
            waveTimer.alignment = TextAnchor.MiddleCenter;
            waveTimer.horizontalOverflow = HorizontalWrapMode.Wrap;
            waveTimer.verticalOverflow = VerticalWrapMode.Truncate;
            waveTimer.raycastTarget = false;
            var rect = waveTimer.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -44);
            rect.sizeDelta = new Vector2(-24, 48);
            ApplyWaveHeaderLayout();
        }
        private void ApplyWaveHeaderLayout()
        {
            lastProvokeVisible = ProvokeVisible;
            if (levelStars != null && levelStars.transform is RectTransform stars)
                stars.anchoredPosition = new Vector2(stars.anchoredPosition.x, ProvokeVisible ? -194 : -108);
            if (rows != null)
            {
                var scroll = rows.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
                if (scroll != null && scroll.transform is RectTransform list)
                {
                    list.offsetMin = new Vector2(12, 12);
                    list.offsetMax = new Vector2(-12, -(WaveHeaderHeight - 12));
                }
            }
        }

        private void Update()
        {
            if (provokeButton != null) provokeButton.Refresh();
            if (lastProvokeVisible != ProvokeVisible)
            {
                ApplyWaveHeaderLayout();
                SetCompactAmount(compactAmount);
            }
            if (boundAlarm == null || waveTimer == null) return;
            float progress = 1 - Mathf.Clamp01(boundAlarm.WaveSecondsRemaining / Mathf.Max(.1f, boundAlarm.WavePreparationDuration));
            waveTimer.color = boundAlarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown
                ? Color.Lerp(Color.white, waveAttackColor, Mathf.SmoothStep(0, 1, progress)) : waveAttackColor;
            int seconds = Mathf.CeilToInt(boundAlarm.WaveSecondsRemaining);
            bool pulse = boundAlarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown &&
                boundAlarm.WaveSecondsRemaining > 0 && boundAlarm.WaveSecondsRemaining <= 5;
            float phase = (5 - boundAlarm.WaveSecondsRemaining) * Mathf.PI * 3;
            int digitSize = pulse ? Mathf.RoundToInt(waveTimer.fontSize * (1 + .14f * (.5f - .5f * Mathf.Cos(phase)))) : 0;
            string language = GameFoundation.Localization.LocalizationService.Instance?.Language;
            if (seconds == displayedSeconds && timerLanguage == language && digitSize == displayedDigitSize) return;
            displayedDigitSize = digitSize;
            timerLanguage = language;
            displayedSeconds = seconds;
            waveTimer.text = boundAlarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown
                ? GameFoundation.Localization.GameText.Get("До волны: ") + (pulse ? "<size=" + digitSize + ">" + seconds + "</size>" : seconds.ToString())
                : GameFoundation.Localization.GameText.Get("Атака");
        }
        private void RefreshWave()
        {
            if (boundAlarm == null) return;
            EnsureWaveTimer(); displayedSeconds = -1;
            foreach (var entry in entries) entry.Value.gameObject.SetActive(false);
            int visible = 0;
            foreach (var prefab in boundAlarm.CurrentWaveEnemyTypes)
            {
                int count = boundAlarm.GetWaveEnemyCount(prefab);
                if (count <= 0) continue;
                var item = EnsureEntry(prefab);
                if (item == null) continue;
                item.gameObject.SetActive(true); item.SetCount(count); visible++;
            }
            if (panel != null) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                WaveHeaderHeight + Mathf.Min(maximumVisibleRows, visible) * (rowHeight + rowSpacing));
            SetVisible(visible > 0 && (boundAlarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown || boundAlarm.CurrentWaveStage == AlarmSystem.WaveStage.Attacking));
            SetCompactAmount(compactAmount);
        }
        private EnemyRosterItemView EnsureEntry(GameObject prefab)
        {
            if (prefab == null) return null;
            if (entries.TryGetValue(prefab, out var existing)) return existing;
            UnitDescriptionDefinition data = descriptions != null ? descriptions.Find(prefab) : null;
            if (data == null)
            {
                // New spawn-list entries work immediately; a catalog asset can refine their text/icon later.
                data = ScriptableObject.CreateInstance<UnitDescriptionDefinition>();
                data.isEnemy = true; data.unitPrefab = prefab; data.fallbackTitle = prefab.name;
                data.roleKey = "unit.enemy.role"; data.fallbackRole = GameFoundation.Localization.GameText.Get("ВРАГ");
                var sprite = prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
                data.portraitIcon = sprite != null && sprite.rect.width <= 32 && sprite.rect.height <= 32 ? sprite : fallbackPortrait;
                generatedDescriptions.Add(data);
            }
            var item = Instantiate(itemTemplate, rows);
            item.name = "Enemy " + prefab.name;
            item.gameObject.SetActive(true);
            var layout = item.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layout != null) layout.preferredHeight = rowHeight;
            item.Bind(data, detailsTooltip, boundAlarm.GetAliveEnemyCount(prefab));
            entries.Add(prefab, item);
            if (rows.TryGetComponent<UnityEngine.UI.VerticalLayoutGroup>(out var group)) group.spacing = rowSpacing;
            if (panel != null) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                headerAndPadding + Mathf.Min(maximumVisibleRows, entries.Count) * (rowHeight + rowSpacing) - rowSpacing);
            return item;
        }
        private void SetVisible(bool value)
        {
            if (visibility == null) return;
            visibility.alpha = value ? 1f : 0f;
            visibility.blocksRaycasts = value;
            visibility.interactable = value;
        }
    }
}
