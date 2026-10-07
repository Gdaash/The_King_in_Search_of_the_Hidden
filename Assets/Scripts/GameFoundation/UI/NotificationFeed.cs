using System.Collections.Generic;
using GameFoundation.Bestiary;
using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.UI
{
    public sealed class NotificationFeed : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumRows = 8;
        [SerializeField, Min(.1f)] private float visibleSeconds = 7f;
        [SerializeField, Min(.01f)] private float fadeSeconds = .35f;
        [SerializeField, Min(.01f)] private float moveSeconds = .25f;
        [SerializeField, Min(0)] private float rowSpacing = 6f;
        [Tooltip("Новые сообщения появляются снизу, предыдущие поднимаются вверх.")]
        [SerializeField] private bool growUpwards = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color positiveColor = new(.43f, .86f, .46f);
        [SerializeField] private Color negativeColor = new(1f, .36f, .38f);
        public Color PositiveColor => positiveColor;
        public Color NegativeColor => negativeColor;
        [Tooltip("Образец строки внутри префаба. Настройте его Text: шрифт, размер, выравнивание и интервалы. В игре образец скрыт.")]
        [SerializeField] private NotificationRow rowPrefab;
        [SerializeField] private UnitDescriptionCatalog unitCatalog;
        [SerializeField] private UnitDescriptionCatalog enemyCatalog;
        private readonly List<NotificationRow> rows = new();
        private sealed class MergedEvent
        {
            public NotificationRow row;
            public float lastPosted;
            public readonly List<NotificationPart> parts = new();
        }
        private readonly Dictionary<string, MergedEvent> mergedEvents = new();

        private void OnEnable()
        {
            if (rowPrefab != null && rowPrefab.transform.IsChildOf(transform)) rowPrefab.gameObject.SetActive(false);
            GameNotifications.Posted += AddEvent;
            GameNotifications.CoalescedPosted += AddCoalescedEvent;
            GameNotifications.UnitDied += Death;
        }

        private void OnDisable()
        {
            GameNotifications.Posted -= AddEvent;
            GameNotifications.CoalescedPosted -= AddCoalescedEvent;
            GameNotifications.UnitDied -= Death;
            foreach (var row in rows) if (row != null) RemoveRow(row);
            rows.Clear();
            mergedEvents.Clear();
        }

        private void Death(Health health)
        {
            var enemy = health.GetComponent<EnemyIdentity>();
            var soldier = health.GetComponent<MilitaryExperience>();
            if (enemy == null && soldier == null) return;
            var definition = enemy != null ? enemyCatalog?.FindEnemyId(enemy.Id) : unitCatalog?.Find(soldier.Profile?.type);
            string title = definition != null ? definition.Title : enemy != null ? "Враг" : "Воин";
            GameNotifications.Post((enemy != null ? "Убит враг: " : "Погиб воин: ") + title,
                enemy != null ? NotificationKind.Positive : NotificationKind.Negative, definition?.Portrait);
        }

        public void Add(string text, NotificationKind kind, Sprite icon)
            => AddEvent(new[] { new NotificationPart(text, kind, icon) });

        public void AddEvent(IReadOnlyList<NotificationPart> parts)
            => CreateRow(parts);

        public void AddCoalescedEvent(string key, float window, IReadOnlyList<NotificationPart> parts)
        {
            if (!mergedEvents.TryGetValue(key, out var entry) || entry.row == null ||
                !rows.Contains(entry.row) || Time.unscaledTime - entry.lastPosted > window)
            {
                entry = new MergedEvent();
                mergedEvents[key] = entry;
            }
            foreach (var part in parts)
            {
                // Keep gains and spending visible instead of cancelling each other to zero.
                int index = part.Resource == null ? -1 : entry.parts.FindIndex(p =>
                    p.Resource == part.Resource && (p.Delta > 0) == (part.Delta > 0));
                if (index < 0) entry.parts.Add(part);
                else entry.parts[index] = new NotificationPart(part.Resource, entry.parts[index].Delta + part.Delta);
            }
            entry.lastPosted = Time.unscaledTime;
            if (entry.row == null) entry.row = CreateRow(entry.parts);
            else entry.row.Configure(entry.parts, normalColor, positiveColor, negativeColor, ((RectTransform)transform).rect.width);
        }

        private NotificationRow CreateRow(IReadOnlyList<NotificationPart> parts)
        {
            if (rowPrefab == null) return null;
            while (rows.Count >= Mathf.Max(1, maximumRows)) RemoveFirst();
            var row = Instantiate(rowPrefab, transform, false);
            row.gameObject.SetActive(true);
            row.Configure(parts, normalColor, positiveColor, negativeColor, ((RectTransform)transform).rect.width);
            row.Rect.anchorMin = row.Rect.anchorMax = row.Rect.pivot = new Vector2(1, growUpwards ? 0 : 1);
            float y = growUpwards || rows.Count == 0 ? 0 : rows[^1].Rect.anchoredPosition.y - rows[^1].Height - rowSpacing;
            row.Rect.anchoredPosition = new Vector2(0, y);
            rows.Add(row);
            return row;
        }

        private void RemoveFirst()
        {
            if (rows.Count == 0) return;
            RemoveRow(rows[0]);
            rows.RemoveAt(0);
        }

        private static void RemoveRow(NotificationRow row)
        {
            row.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(row.gameObject);
            else DestroyImmediate(row.gameObject);
        }

        private void Update()
        {
            while (rows.Count > Mathf.Max(1, maximumRows)) RemoveFirst();
            for (int i = rows.Count - 1; i >= 0; i--)
                if (Time.unscaledTime - rows[i].CreatedAt >= visibleSeconds + fadeSeconds)
                { RemoveRow(rows[i]); rows.RemoveAt(i); }
            float y = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[growUpwards ? rows.Count - 1 - index : index];
                row.Animate(y, visibleSeconds, fadeSeconds, moveSeconds);
                y += (row.Height + rowSpacing) * (growUpwards ? 1 : -1);
            }
        }
    }
}
