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
            foreach (Transform child in content) Destroy(child.gameObject);
            if (title != null) title.text = $"Итоги дня {report.day}";
            if (runDuration != null)
            {
                int seconds = report.runDurationSeconds;
                runDuration.text = $"Время в забеге: {seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            }
            if (starvation != null)
            {
                starvation.gameObject.SetActive(report.starved > 0);
                starvation.text = $"От голода умерло людей: {report.starved}";
            }

            var types = new Dictionary<string, ResourceType>();
            if (GlobalResourceManager.Instance?.AvailableResources != null)
                foreach (ResourceType type in GlobalResourceManager.Instance.AvailableResources)
                    if (type != null) types[type.name] = type;

            report.entries.Sort((a, b) => string.Compare(a.resource, b.resource, StringComparison.CurrentCultureIgnoreCase));
            foreach (DayResourceLedger.Entry entry in report.entries)
            {
                types.TryGetValue(entry.resource, out ResourceType type);
                DayReportRow row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.SetData(type, entry);
            }
            panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }

}
