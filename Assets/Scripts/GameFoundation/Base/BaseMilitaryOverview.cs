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

        private readonly List<WorldMilitaryRosterItemView> items = new();
        private float nextHealthRefresh;

        private void Awake()
        {
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void OnResourceChanged(ResourceType _, int __) => Refresh();
        private void LateUpdate()
        {
            if (Time.unscaledTime < nextHealthRefresh) return;
            nextHealthRefresh = Time.unscaledTime + .2f;
            foreach (var item in items) if (item != null) item.Refresh();
        }

        private void Refresh()
        {
            if (content == null || itemTemplate == null) return;
            foreach (WorldMilitaryRosterItemView item in items)
                if (item != null) Destroy(item.gameObject);
            items.Clear();
            AddWarriors(swordsmen);
            AddWarriors(archers);

            if (emptyLabel != null) emptyLabel.gameObject.SetActive(items.Count == 0);
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
                item.SetProfile(type.resource, profiles[i]);
                items.Add(item);
            }
        }
    }
}
