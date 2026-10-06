using System.Collections.Generic;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEngine;

namespace GameFoundation.Bestiary
{
    /// <summary>Base popup for the player's persistent monster collection.</summary>
    public sealed class MagicLibraryView : MonoBehaviour
    {
        [SerializeField] private UnitDescriptionCatalog enemyDescriptions;
        [SerializeField] private BestiaryEntryView entryTemplate;
        [SerializeField] private RectTransform entriesRoot;
        [SerializeField] private UnitDescriptionTooltip detailsTooltip;
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private UnityEngine.UI.Text subtitle;
        [SerializeField] private UnityEngine.UI.Text discoveredCount;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private string titleKey = "base.magic_library.title";
        [SerializeField] private string subtitleKey = "base.magic_library.subtitle";
        [SerializeField] private string countKey = "base.magic_library.count";
        [SerializeField] private string emptyKey = "base.magic_library.empty";
        private readonly List<BestiaryEntryView> entries = new();
        private bool subscribed;

        private void Awake()
        {
            if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            BestiaryService.Encountered += OnEncountered;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged += Refresh;
            subscribed = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            BestiaryService.Encountered -= OnEncountered;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
            subscribed = false;
            if (detailsTooltip != null) detailsTooltip.Hide(null);
        }

        private void OnDestroy()
        {
            foreach (BestiaryEntryView entry in entries)
                if (entry != null)
                {
                    // Destroy is deferred until the frame end. Disable immediately so reopening
                    // the popup cannot render an old row together with its replacement.
                    entry.gameObject.SetActive(false);
                    Destroy(entry.gameObject);
                }
        }

        private void OnEncountered(string _) => Refresh();

        public void Refresh()
        {
            if (title != null) title.text = Tr(titleKey, "МАГИЧЕСКАЯ БИБЛИОТЕКА");
            if (subtitle != null) subtitle.text = Tr(subtitleKey, "Бестиарий встреченных существ");

            foreach (BestiaryEntryView entry in entries)
                if (entry != null)
                {
                    // Destroy is deferred until the frame end. Disable immediately so reopening
                    // the popup cannot render an old row together with its replacement.
                    entry.gameObject.SetActive(false);
                    Destroy(entry.gameObject);
                }
            entries.Clear();

            int discovered = 0;
            foreach (string id in BestiaryService.EncounteredIds)
            {
                UnitDescriptionDefinition definition = enemyDescriptions != null ? enemyDescriptions.FindEnemyId(id) : null;
                if (definition == null || entryTemplate == null || entriesRoot == null) continue;
                BestiaryEntryView entry = Instantiate(entryTemplate, entriesRoot);
                entry.name = "Discovered " + definition.Title;
                entry.gameObject.SetActive(true);
                entry.Bind(definition, detailsTooltip);
                entries.Add(entry);
                discovered++;
            }

            if (discoveredCount != null)
                discoveredCount.text = string.Format(Tr(countKey, "Открыто: {0}"), discovered);
            if (emptyState != null)
            {
                emptyState.SetActive(discovered == 0);
                var text = emptyState.GetComponent<UnityEngine.UI.Text>();
                if (text != null) text.text = Tr(emptyKey, "В бестиарии пока нет записей.\nВстретьте монстра в бою, чтобы добавить его сюда.");
            }
        }

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
