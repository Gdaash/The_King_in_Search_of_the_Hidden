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
        private readonly Dictionary<GameObject, EnemyRosterItemView> entries = new();
        private readonly List<UnitDescriptionDefinition> generatedDescriptions = new();
        private AlarmSystem boundAlarm;
        private bool subscribed;
        private bool started;

        private void Awake() => SetVisible(false);
        private void OnEnable() { if (started) Bind(); }
        // All AlarmSystem.Awake calls have selected the current location by Start.
        private void Start() { started = true; Bind(); }
        private void OnDisable()
        {
            if (boundAlarm != null && subscribed) boundAlarm.EnemyCountChanged -= OnCountChanged;
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
            if (boundAlarm != null && subscribed) boundAlarm.EnemyCountChanged -= OnCountChanged;
            boundAlarm = source;
            itemTemplate.gameObject.SetActive(false);
            foreach (var threshold in source.ConfiguredThresholds)
            {
                if (threshold?.enemies == null) continue;
                foreach (var enemy in threshold.enemies)
                    if (enemy?.prefab != null && enemy.weight > 0) EnsureEntry(enemy.prefab);
            }
            foreach (var prefab in source.SpawnedEnemyTypes) EnsureEntry(prefab);
            foreach (var entry in entries) entry.Value.SetCount(source.GetAliveEnemyCount(entry.Key));
            source.EnemyCountChanged += OnCountChanged;
            subscribed = true;
            SetVisible(source.HasSpawnedEnemies);
        }

        private void OnCountChanged(GameObject prefab, int count)
        {
            EnsureEntry(prefab)?.SetCount(count);
            SetVisible(boundAlarm != null && boundAlarm.HasSpawnedEnemies);
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
                data.roleKey = "unit.enemy.role"; data.fallbackRole = "ВРАГ";
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
