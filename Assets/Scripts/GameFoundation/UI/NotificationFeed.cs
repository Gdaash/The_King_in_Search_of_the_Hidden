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
        [Tooltip("Образец строки внутри префаба. Настройте его Text: шрифт, размер, выравнивание и интервалы. В игре образец скрыт.")]
        [SerializeField] private NotificationRow rowPrefab;
        [SerializeField] private UnitDescriptionCatalog unitCatalog;
        [SerializeField] private UnitDescriptionCatalog enemyCatalog;
        private readonly List<NotificationRow> rows = new();

        private void OnEnable()
        {
            if (rowPrefab != null && rowPrefab.transform.IsChildOf(transform)) rowPrefab.gameObject.SetActive(false);
            GameNotifications.Posted += AddEvent;
            GameNotifications.UnitDied += Death;
        }

        private void OnDisable()
        {
            GameNotifications.Posted -= AddEvent;
            GameNotifications.UnitDied -= Death;
            foreach (var row in rows) if (row != null) RemoveRow(row);
            rows.Clear();
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
        {
            if (rowPrefab == null) return;
            while (rows.Count >= Mathf.Max(1, maximumRows)) RemoveFirst();
            var row = Instantiate(rowPrefab, transform, false);
            row.gameObject.SetActive(true);
            row.Configure(parts, normalColor, positiveColor, negativeColor, ((RectTransform)transform).rect.width);
            row.Rect.anchorMin = row.Rect.anchorMax = row.Rect.pivot = new Vector2(1, growUpwards ? 0 : 1);
            float y = growUpwards || rows.Count == 0 ? 0 : rows[^1].Rect.anchoredPosition.y - rows[^1].Height - rowSpacing;
            row.Rect.anchoredPosition = new Vector2(0, y);
            rows.Add(row);
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
            while (rows.Count > Mathf.Max(1, maximumRows) || rows.Count > 0 &&
                Time.unscaledTime - rows[0].CreatedAt >= visibleSeconds + fadeSeconds) RemoveFirst();
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
