using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class WorldEscapeController : MonoBehaviour
    {
        [Header("Побег")]
        [SerializeField] private Button escapeButton;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private ResourceType humanResource;
        [SerializeField] private WorldFlashlightAvailability flashlightAvailability;

        [Header("Итоги забега")]
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private Transform statisticsContent;
        [SerializeField] private WorldResourceStatRow rowTemplate;
        [SerializeField] private ScrollRect statisticsScroll;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField] private Button returnButton;
        [SerializeField, Min(0f)] private float rowRevealDelay = 0.22f;
        [SerializeField] private string baseScene = "Base";

        private readonly Dictionary<ResourceType, int> _startingResources = new();
        private int _startingHumans;
        private float _runStartedAt;
        private int _runDurationSeconds;
        private bool _escaping;
        private bool _showingStatistics;
        private bool _loading;

        private void Start()
        {
            DayResourceLedger.BeginRun();
            _runStartedAt = Time.realtimeSinceStartup;
            if (humanResource == null && GlobalResourceManager.Instance != null)
                foreach (ResourceType resource in GlobalResourceManager.Instance.AvailableResources)
                    if (resource != null && resource.isHumanResource)
                    {
                        humanResource = resource;
                        break;
                    }

            foreach (var item in CaptureResources())
                _startingResources[item.Key] = item.Value;
            _startingHumans = GetStoredHumans();

            if (progressPanel != null) progressPanel.SetActive(false);
            if (statisticsPanel != null) statisticsPanel.SetActive(false);
            if (buttonLabel != null) buttonLabel.text = "Сбежать";
            if (escapeButton != null) escapeButton.onClick.AddListener(OnEscapeClicked);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToBase);
        }

        private void OnDestroy()
        {
            if (escapeButton != null) escapeButton.onClick.RemoveListener(OnEscapeClicked);
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToBase);
        }

        private void Update()
        {
            if (!_escaping || _showingStatistics || _loading) return;
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            int stored = GetStoredHumans();
            if (progressFill != null)
                progressFill.fillAmount = _startingHumans == 0 ? 1f : Mathf.Clamp01((float)stored / _startingHumans);
            if (progressLabel != null)
                progressLabel.text = $"{stored}/{_startingHumans}";

            if (stored >= _startingHumans)
                ShowStatistics();
        }

        private int GetStoredHumans()
        {
            return GlobalResourceManager.Instance != null && humanResource != null
                ? GlobalResourceManager.Instance.GetResourceAmount(humanResource)
                : 0;
        }

        private void OnEscapeClicked()
        {
            if (_loading || _showingStatistics) return;
            if (_escaping)
            {
                ShowStatistics();
                return;
            }

            _escaping = true;
            foreach (TimerController timer in UnityEngine.Object.FindObjectsByType<TimerController>(FindObjectsInactive.Include))
                timer.StopForEscape();
            if (flashlightAvailability != null) flashlightAvailability.DisableAllForEscape();
            if (OrderManager.Instance != null) OrderManager.Instance.enabled = false;

            foreach (HumanUnit human in UnityEngine.Object.FindObjectsByType<HumanUnit>())
                human.ResetTask();
            foreach (Porter porter in UnityEngine.Object.FindObjectsByType<Porter>())
                porter.ResetTask();
            foreach (ResourceRequester requester in UnityEngine.Object.FindObjectsByType<ResourceRequester>())
                requester.SendHumansHome(humanResource, Warehouse.Instance);

            if (buttonLabel != null) buttonLabel.text = "Сбежать немедленно";
            if (progressPanel != null) progressPanel.SetActive(true);
            RefreshProgress();
        }

        private Dictionary<ResourceType, int> CaptureResources()
        {
            var snapshot = GlobalResourceManager.Instance != null
                ? GlobalResourceManager.Instance.GetAllResourcesData()
                : new Dictionary<ResourceType, int>();
            if (GlobalResourceManager.Instance?.AvailableResources != null)
                foreach (ResourceType resource in GlobalResourceManager.Instance.AvailableResources)
                    if (resource != null && !snapshot.ContainsKey(resource))
                        snapshot.Add(resource, GlobalResourceManager.Instance.GetResourceAmount(resource));
            return snapshot;
        }

        private void ShowStatistics()
        {
            if (_showingStatistics) return;
            _showingStatistics = true;
            if (escapeButton != null) escapeButton.gameObject.SetActive(false);
            if (progressPanel != null) progressPanel.SetActive(false);
            if (statisticsPanel != null) statisticsPanel.SetActive(true);
            if (returnButton != null) returnButton.gameObject.SetActive(false);

            int seconds = Mathf.Max(0, Mathf.FloorToInt(Time.realtimeSinceStartup - _runStartedAt));
            _runDurationSeconds = seconds;
            if (durationLabel != null)
                durationLabel.text = $"Длительность забега: {seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";

            StartCoroutine(RevealStatistics(CaptureResources()));
        }

        private IEnumerator RevealStatistics(Dictionary<ResourceType, int> endingResources)
        {
            var resourceSet = new HashSet<ResourceType>(_startingResources.Keys);
            foreach (ResourceType resource in endingResources.Keys) resourceSet.Add(resource);
            var resources = new List<ResourceType>(resourceSet);
            resources.RemoveAll(resource => resource == null);
            resources.Sort((a, b) => string.Compare(a.resourceName, b.resourceName, StringComparison.CurrentCultureIgnoreCase));

            foreach (ResourceType resource in resources)
            {
                _startingResources.TryGetValue(resource, out int before);
                endingResources.TryGetValue(resource, out int after);
                RevealRow(statisticsContent, statisticsScroll, resource, before, after);
                yield return new WaitForSecondsRealtime(rowRevealDelay);
            }

            if (returnButton != null) returnButton.gameObject.SetActive(true);
        }

        private void RevealRow(Transform content, ScrollRect scroll, ResourceType resource, int before, int after)
        {
            if (content == null || rowTemplate == null) return;
            WorldResourceStatRow row = Instantiate(rowTemplate, content);
            row.gameObject.SetActive(true);
            row.SetData(resource, before, after);
            var group = row.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.DOFade(1f, 0.28f).SetUpdate(true);
            }
            row.transform.localScale = Vector3.one * 0.94f;
            row.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
            Canvas.ForceUpdateCanvases();
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
        }

        private void ReturnToBase()
        {
            if (_loading) return;
            _loading = true;
            DayResourceLedger.EndRun(_runDurationSeconds);
            SceneManager.LoadScene(baseScene);
        }
    }
}

