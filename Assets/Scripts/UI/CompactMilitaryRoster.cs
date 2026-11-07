using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>One icon per warrior, up to two rows; crowding changes spacing, never sprite scale.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class CompactMilitaryRoster : MonoBehaviour
    {
        [SerializeField, Min(0)] private float iconGap = 6;
        private readonly List<ResourceType> types = new();
        private readonly List<(ResourceType type, int count)> snapshot = new();
        private readonly List<UnityEngine.UI.Image> icons = new();
        private Vector2 previousSize;
        public int TotalCount { get; private set; }
        public int RowCount { get; private set; }

        public void Refresh()
        {
            var resources = GlobalResourceManager.Instance;
            types.Clear();
            foreach (var type in ResourceCatalog.All)
                if (type != null && type.isMilitaryUnit && resources != null && resources.GetResourceAmount(type) > 0)
                    types.Add(type);
            types.Sort(CompareTypes);
            var rect = (RectTransform)transform;
            bool changed = previousSize != rect.rect.size || types.Count != snapshot.Count;
            for (int i = 0; !changed && i < types.Count; i++)
                changed = snapshot[i].type != types[i] || snapshot[i].count != resources.GetResourceAmount(types[i]);
            if (!changed) return;
            previousSize = rect.rect.size;
            snapshot.Clear();
            int active = 0;
            foreach (var type in types)
            {
                int count = resources.GetResourceAmount(type);
                snapshot.Add((type, count));
                for (int i = 0; i < count; i++, active++)
                {
                    if (active == icons.Count)
                    {
                        var icon = new GameObject("Warrior", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
                        icon.transform.SetParent(transform, false);
                        icon.raycastTarget = false;
                        icons.Add(icon);
                    }
                    var image = icons[active];
                    image.gameObject.SetActive(true);
                    image.name = type.Id + " " + (i + 1);
                    ResourceIconSizing.Apply(image, type.resourceIcon);
                    image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.one * .5f;
                    // Later icons cover earlier icons, like a hand of cards. No separate canvases or physical offset.
                    image.transform.SetSiblingIndex(active);
                }
            }
            for (int i = active; i < icons.Count; i++) icons[i].gameObject.SetActive(false);
            TotalCount = active;
            if (active == 0) { RowCount = 0; return; }

            float width = rect.rect.width;
            if (RowWidth(0, active) <= width)
            {
                RowCount = 1;
                LayoutRow(0, active, 0, width);
                return;
            }
            RowCount = 2;
            // Fill the first row normally. Only balance the rows once both need to become denser.
            int split = 1;
            while (split < active && RowWidth(0, split + 1) <= width) split++;
            if (RowWidth(split, active - split) > width) split = (active + 1) / 2;
            LayoutRow(0, split, rect.rect.height * .25f, width);
            LayoutRow(split, active - split, -rect.rect.height * .25f, width);
        }

        private static int CompareTypes(ResourceType a, ResourceType b)
        {
            int order = a.militaryDisplayOrder.CompareTo(b.militaryDisplayOrder);
            return order != 0 ? order : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }

        private float RowWidth(int first, int count)
        {
            float width = Mathf.Max(0, count - 1) * iconGap;
            for (int i = first; i < first + count; i++) width += icons[i].rectTransform.rect.width;
            return width;
        }

        private void LayoutRow(int first, int count, float y, float availableWidth)
        {
            if (count == 0) return;
            float width = RowWidth(first, count);
            float edgeHalves = (icons[first].rectTransform.rect.width + icons[first + count - 1].rectTransform.rect.width) * .5f;
            float compression = width > availableWidth && count > 1
                ? Mathf.Clamp01((availableWidth - edgeHalves) / (width - edgeHalves)) : 1;
            float displayedWidth = edgeHalves + (width - edgeHalves) * compression;
            float x = -displayedWidth * .5f + icons[first].rectTransform.rect.width * .5f;
            for (int i = first; i < first + count; i++)
            {
                var icon = icons[i].rectTransform;
                icon.anchoredPosition = new Vector2(Mathf.Round(x), y);
                if (i + 1 < first + count)
                    x += (icon.rect.width * .5f + iconGap + icons[i + 1].rectTransform.rect.width * .5f) * compression;
            }
        }
    }
}
