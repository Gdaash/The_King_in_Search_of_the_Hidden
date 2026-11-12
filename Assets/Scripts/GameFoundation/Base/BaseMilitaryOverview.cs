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
        [SerializeField] private ResourceType[] additionalWarriors = Array.Empty<ResourceType>();

        [Header("Prefab hierarchy")]
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform content;
        [SerializeField] private Text emptyLabel;
        [SerializeField] private WorldMilitaryRosterItemView itemTemplate;

        private readonly Dictionary<string, WorldMilitaryRosterItemView> items = new();
        private readonly HashSet<string> visible = new();
        private readonly List<string> stale = new();
        private bool needsRefresh;
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
        private void OnResourceChanged(ResourceType resource, int _)
        {
            if (resource == null) return;
            if (resource == swordsmen?.resource || resource == archers?.resource ||
                Array.IndexOf(additionalWarriors, resource) >= 0 || resource.isMilitaryUnit)
                needsRefresh = true;
        }
        private void LateUpdate()
        {
            if (needsRefresh) Refresh();
            if (Time.unscaledTime < nextHealthRefresh) return;
            nextHealthRefresh = Time.unscaledTime + .2f;
            foreach (var item in items.Values) if (item != null) item.Refresh();
        }

        public void RebuildRoster() => Refresh();

        private void Refresh()
        {
            if (content == null || itemTemplate == null) return;
            needsRefresh = false;
            visible.Clear();
            AddWarriors(swordsmen);
            AddWarriors(archers);
            foreach(var resource in additionalWarriors) AddWarriors(new MilitaryCount{resource=resource});

            foreach (var resource in ResourceCatalog.All)
                if (resource.isMilitaryUnit && resource != swordsmen?.resource && resource != archers?.resource &&
                    Array.IndexOf(additionalWarriors, resource) < 0)
                    AddWarriors(new MilitaryCount { resource = resource });

            stale.Clear();
            foreach (var entry in items)
                if (!visible.Contains(entry.Key)) stale.Add(entry.Key);
            foreach (var key in stale)
            {
                var item = items[key];
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                    Destroy(item.gameObject);
                }
                items.Remove(key);
            }
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(items.Count == 0);
        }

        private void AddWarriors(MilitaryCount type)
        {
            if (type?.resource == null || GlobalResourceManager.Instance == null) return;
            int count = GlobalResourceManager.Instance.GetResourceAmount(type.resource);
            IReadOnlyList<MilitaryProfile> profiles = MilitaryExperienceService.GetStored(type.resource, count);
            for (int i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (!visible.Add(profile.id)) continue;
                if (!items.TryGetValue(profile.id, out var item) || item == null)
                {
                    item = Instantiate(itemTemplate, content);
                    item.name = type.resource.resourceName + " " + (i + 1);
                    item.gameObject.SetActive(true);
                    items[profile.id] = item;
                }
                item.SetProfile(type.resource, profile);
                int sibling = itemTemplate.transform.GetSiblingIndex() + visible.Count;
                if (item.transform.GetSiblingIndex() != sibling) item.transform.SetSiblingIndex(sibling);
            }
        }
    }
}
