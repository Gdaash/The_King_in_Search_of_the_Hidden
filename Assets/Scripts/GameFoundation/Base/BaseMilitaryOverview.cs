using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;

namespace GameFoundation.Base
{
    /// <summary>Shows every warrior held at the base using the same item template as the World roster.</summary>
    public sealed class BaseMilitaryOverview : MonoBehaviour
    {
        [Serializable]
        private sealed class MilitaryCount
        {
            public ResourceType resource;
        }

        [Header("Resources")]
        [SerializeField] private MilitaryCount swordsmen = new();
        [SerializeField] private MilitaryCount archers = new();

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

        private readonly List<WorldMilitaryRosterItemView> items = new();
        private RectTransform panel;

        private void Awake()
        {
            panel = transform as RectTransform;
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            if (content == null || itemTemplate == null) return;
            foreach (WorldMilitaryRosterItemView item in items)
                if (item != null) Destroy(item.gameObject);
            items.Clear();
            AddWarriors(swordsmen);
            AddWarriors(archers);

            float contentWidth = Mathf.Max(itemSpacing, items.Count * itemSpacing);
            content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
            if (panel != null) panel.sizeDelta = new Vector2(Mathf.Max(minimumPanelWidth, contentWidth + horizontalPadding * 2f), panelHeight);
            if (title != null) title.sizeDelta = new Vector2(Mathf.Max(minimumPanelWidth - horizontalPadding * 2f, contentWidth), title.sizeDelta.y);
            if (emptyLabel != null)
            {
                emptyLabel.rectTransform.sizeDelta = new Vector2(contentWidth, emptyLabel.rectTransform.sizeDelta.y);
                emptyLabel.gameObject.SetActive(items.Count == 0);
            }
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform rect = items[i].transform as RectTransform;
                if (rect != null) rect.anchoredPosition = new Vector2(-contentWidth * .5f + itemSpacing * .5f + i * itemSpacing, 0f);
            }
        }

        private void AddWarriors(MilitaryCount type)
        {
            if (type?.resource == null || GlobalResourceManager.Instance == null) return;
            int count = GlobalResourceManager.Instance.GetResourceAmount(type.resource);
            IReadOnlyList<MilitaryProfile> profiles = MilitaryExperienceService.GetStored(type.resource, count);
            for (int i = 0; i < profiles.Count; i++)
            {
                WorldMilitaryRosterItemView item = Instantiate(itemTemplate, content);
                item.name = type.resource.resourceName + " " + (i + 1);
                item.gameObject.SetActive(true);
                item.SetIcon(type.resource.resourceIcon, MilitaryExperienceService.HealthPercent(profiles[i]),
                    MilitaryExperienceService.Stars(profiles[i]));
                items.Add(item);
            }
        }
    }
}
