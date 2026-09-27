using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>World HUD roster driven by a prefab-authored title, content area and item template.</summary>
    public sealed class WorldMilitaryRosterView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WorldMilitaryDeploymentController deployment;

        [Header("Prefab hierarchy")]
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform content;
        [SerializeField] private Text emptyLabel;
        [SerializeField] private WorldMilitaryRosterItemView itemTemplate;

        [Header("Layout")]
        [SerializeField, Min(48f)] private float itemSpacing = 64f;
        [SerializeField, Min(0f)] private float horizontalPadding = 12f;
        [SerializeField, Min(1f)] private float minimumPanelWidth = 216f;
        [SerializeField, Min(1f)] private float panelHeight = 118f;

        private readonly Dictionary<GameObject, WorldMilitaryRosterItemView> entries = new();
        private RectTransform panel;

        private void Awake()
        {
            if (deployment == null) deployment = GetComponent<WorldMilitaryDeploymentController>();
            panel = transform as RectTransform;
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (deployment == null || content == null || itemTemplate == null) return;
            Sync();
        }

        private void Sync()
        {
            IReadOnlyList<GameObject> units = deployment.ActiveUnits;
            RemoveStaleEntries(units);
            foreach (GameObject unit in units)
            {
                if (unit == null || entries.ContainsKey(unit)) continue;
                WorldMilitaryRosterItemView entry = Instantiate(itemTemplate, content);
                entry.name = unit.name + " Health";
                entry.gameObject.SetActive(true);
                entry.SetUnit(unit);
                entries.Add(unit, entry);
            }

            int count = 0;
            foreach (GameObject unit in units)
                if (unit != null && entries.TryGetValue(unit, out WorldMilitaryRosterItemView entry) && entry != null) count++;

            float contentWidth = Mathf.Max(itemSpacing, count * itemSpacing);
            ApplyLayout(contentWidth, units);
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(count == 0);
        }

        private void ApplyLayout(float contentWidth, IReadOnlyList<GameObject> units)
        {
            if (content != null) content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
            if (panel != null) panel.sizeDelta = new Vector2(Mathf.Max(minimumPanelWidth, contentWidth + horizontalPadding * 2f), panelHeight);
            if (title != null) title.sizeDelta = new Vector2(Mathf.Max(minimumPanelWidth - horizontalPadding * 2f, contentWidth), title.sizeDelta.y);
            if (emptyLabel != null) emptyLabel.rectTransform.sizeDelta = new Vector2(contentWidth, emptyLabel.rectTransform.sizeDelta.y);

            int index = 0;
            foreach (GameObject unit in units)
            {
                if (unit == null || !entries.TryGetValue(unit, out WorldMilitaryRosterItemView entry) || entry == null) continue;
                RectTransform rect = entry.transform as RectTransform;
                if (rect == null) continue;
                rect.anchoredPosition = new Vector2(-contentWidth * .5f + itemSpacing * .5f + index * itemSpacing, 0f);
                entry.Refresh();
                index++;
            }
        }

        private void RemoveStaleEntries(IReadOnlyList<GameObject> units)
        {
            while (true)
            {
                bool found = false;
                GameObject staleKey = null;
                WorldMilitaryRosterItemView staleEntry = null;
                foreach (var pair in entries)
                {
                    if (pair.Key != null && ContainsUnit(units, pair.Key)) continue;
                    // A destroyed Unity object compares equal to null, but remains a valid
                    // Dictionary key until it is explicitly removed.
                    staleKey = pair.Key;
                    staleEntry = pair.Value;
                    found = true;
                    break;
                }
                if (!found) return;
                if (staleEntry != null) Destroy(staleEntry.gameObject);
                entries.Remove(staleKey);
            }
        }

        private static bool ContainsUnit(IReadOnlyList<GameObject> units, GameObject candidate)
        {
            for (int i = 0; i < units.Count; i++) if (units[i] == candidate) return true;
            return false;
        }
    }
}
