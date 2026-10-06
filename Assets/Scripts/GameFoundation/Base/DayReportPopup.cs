using System;
using System.Collections.Generic;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class DayReportPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text runDuration;
        [SerializeField] private TMP_Text starvation;
        [SerializeField] private Transform content;
        [SerializeField] private DayReportRow rowTemplate;
        [SerializeField] private Button closeButton;
        [Tooltip("Как в итогах побега: показывать имеющиеся войска, даже если их количество не изменилось.")]
        [SerializeField] private ResourceType[] militaryResources;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open(DayResourceLedger.Report report)
        {
            if (report == null || panel == null || content == null || rowTemplate == null) return;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (title != null) title.text = $"Итоги дня {report.day}";
            if (runDuration != null)
            {
                int seconds = report.runDurationSeconds;
                runDuration.text = $"Время в забеге: {seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            }
            if (starvation != null)
            {
                starvation.gameObject.SetActive(report.starved > 0);
                starvation.text = $"От голода умерло жителей и воинов: {report.starved}";
            }

            var types = new Dictionary<string, ResourceType>();
            if (GlobalResourceManager.Instance?.AvailableResources != null)
                foreach (ResourceType type in GlobalResourceManager.Instance.AvailableResources)
                    if (type != null) types[type.Id] = type;

            report.entries.Sort((a, b) => string.Compare(a.resource, b.resource, StringComparison.CurrentCultureIgnoreCase));
            foreach (DayResourceLedger.Entry entry in report.entries)
            {
                types.TryGetValue(entry.resource, out ResourceType type);
                if (type == null) type = ResourceCatalog.Find(entry.resource);
                if (!ShouldDisplay(entry, type)) continue;
                DayReportRow row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.SetData(type, entry);
            }
            panel.SetActive(true);
            var scroll = content.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        public bool ShouldDisplay(DayResourceLedger.Entry entry, ResourceType type)
        {
            if (entry == null) return false;
            if (type != null && type.isHumanResource) return true;
            if (entry.HasChanges) return true;
            if (type != null && (entry.start > 0 || entry.end > 0) && militaryResources != null)
                foreach (var military in militaryResources) if (type == military) return true;
            return false;
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }

}
