using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameFoundation.UI;
using GameFoundation.Audio;

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
        [SerializeField] private WorldMilitaryDeploymentController militaryDeployment;

        [Header("Предупреждение о здоровье портала")]
        [SerializeField] private Health portalTowerHealth;
        [SerializeField, Range(0f, 1f)] private float softWarningThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float criticalWarningThreshold = 0.25f;
        [SerializeField, Min(0f)] private float softPulseAmount = 0.025f;
        [SerializeField, Min(0f)] private float criticalPulseAmount = 0.07f;
        [SerializeField, Min(0.01f)] private float softPulseFrequency = 1.15f;
        [SerializeField, Min(0.01f)] private float criticalPulseFrequency = 2.1f;
        [SerializeField] private Color criticalLabelColor = new(0.95f, 0.16f, 0.13f, 1f);

        [Header("Итоги забега")]
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private Transform statisticsContent;
        [SerializeField] private WorldResourceStatRow rowTemplate;
        [SerializeField] private ScrollRect statisticsScroll;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField] private Button returnButton;
        [SerializeField] private TMP_Text defeatMessage;
        [SerializeField, Min(0f)] private float rowRevealDelay = 0.22f;
        [SerializeField] private string baseScene = "Base";

        private readonly Dictionary<ResourceType, int> _startingResources = new();
        private int _startingHumans;
        private float _runStartedAt;
        private int _runDurationSeconds;
        private bool _escaping;
        private bool _showingStatistics;
        private bool _loading;
        private UnifiedButtonFeedback _buttonFeedback;
        private Color _normalButtonLabelColor = Color.white;
        private float _portalHealthNormalized = 1f;

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
            if (defeatMessage != null) defeatMessage.gameObject.SetActive(false);
            if (buttonLabel != null) buttonLabel.text = "Сбежать";
            if (buttonLabel != null) _normalButtonLabelColor = buttonLabel.color;
            if (escapeButton != null) _buttonFeedback = escapeButton.GetComponent<UnifiedButtonFeedback>();
            ResolvePortalTowerHealth();
            if (escapeButton != null) escapeButton.onClick.AddListener(OnEscapeClicked);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToBase);
        }

        private void OnDestroy()
        {
            if (escapeButton != null) escapeButton.onClick.RemoveListener(OnEscapeClicked);
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToBase);
            if (portalTowerHealth != null) portalTowerHealth.OnHealthChanged.RemoveListener(OnPortalHealthChanged);
            if (_buttonFeedback != null) _buttonFeedback.ExternalScaleMultiplier = 1f;
            if (buttonLabel != null) buttonLabel.color = _normalButtonLabelColor;
        }

        private void Update()
        {
            UpdateEscapeButtonWarning();
            if (!_escaping || _showingStatistics || _loading) return;
            RefreshProgress();
        }

        private void ResolvePortalTowerHealth()
        {
            if (portalTowerHealth == null)
            {
                GameObject tower = GameObject.Find("PortalTower");
                if (tower != null) portalTowerHealth = tower.GetComponent<Health>() ?? tower.GetComponentInChildren<Health>(true);
            }
            if (portalTowerHealth == null) return;
            _portalHealthNormalized = portalTowerHealth.NormalizedHealth;
            portalTowerHealth.OnHealthChanged.RemoveListener(OnPortalHealthChanged);
            portalTowerHealth.OnHealthChanged.AddListener(OnPortalHealthChanged);
        }

        private void OnPortalHealthChanged(float normalizedHealth)
        {
            _portalHealthNormalized = Mathf.Clamp01(normalizedHealth);
        }

        private void UpdateEscapeButtonWarning()
        {
            if (_buttonFeedback == null || buttonLabel == null) return;
            if (_escaping || _showingStatistics || _loading)
            {
                _buttonFeedback.ExternalScaleMultiplier = 1f;
                buttonLabel.color = _normalButtonLabelColor;
                return;
            }

            bool critical = _portalHealthNormalized < criticalWarningThreshold;
            bool warning = _portalHealthNormalized < softWarningThreshold;
            if (!warning)
            {
                _buttonFeedback.ExternalScaleMultiplier = 1f;
                buttonLabel.color = _normalButtonLabelColor;
                return;
            }

            float amount = critical ? criticalPulseAmount : softPulseAmount;
            float frequency = critical ? criticalPulseFrequency : softPulseFrequency;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * frequency);
            _buttonFeedback.ExternalScaleMultiplier = 1f + amount * wave;
            buttonLabel.color = critical ? criticalLabelColor : _normalButtonLabelColor;
        }

        private void RefreshProgress()
        {
            int stored = GetStoredHumans();
            if (progressFill != null)
                progressFill.fillAmount = _startingHumans == 0 ? 1f : Mathf.Clamp01((float)stored / _startingHumans);
            if (progressLabel != null)
                progressLabel.text = $"{stored}/{_startingHumans}";

            bool militaryReturned = militaryDeployment == null || !militaryDeployment.HasReturningUnits;
            if (stored >= _startingHumans && militaryReturned)
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
            GameAudioController.PlayUI(GameAudioCue.Portal, 0.9f, 0.98f, 1.02f, 0.2f);
            StopWorldForEscape();

            foreach (HumanUnit human in UnityEngine.Object.FindObjectsByType<HumanUnit>())
                human.ResetTask();
            foreach (Porter porter in UnityEngine.Object.FindObjectsByType<Porter>())
                porter.ResetTask();
            foreach (ResourceRequester requester in UnityEngine.Object.FindObjectsByType<ResourceRequester>())
                requester.SendHumansHome(humanResource, Warehouse.Instance);
            if (militaryDeployment == null)
                militaryDeployment = UnityEngine.Object.FindAnyObjectByType<WorldMilitaryDeploymentController>();
            if (militaryDeployment != null)
                militaryDeployment.BeginEscapeRecall();

            if (buttonLabel != null) buttonLabel.text = "Сбежать немедленно";
            if (progressPanel != null) progressPanel.SetActive(true);
            RefreshProgress();
        }

        public void ShowPortalDestroyedStatistics()
        {
            if (_loading || _showingStatistics) return;
            _escaping = true;
            StopWorldForEscape();
            if (defeatMessage != null)
            {
                defeatMessage.text = "Вы не успели сбежать, портал был уничтожен.";
                defeatMessage.color = new Color(0.9f, 0.16f, 0.16f, 1f);
                defeatMessage.gameObject.SetActive(true);
            }
            ShowStatistics();
        }

        private void StopWorldForEscape()
        {
            foreach (TimerController timer in UnityEngine.Object.FindObjectsByType<TimerController>(FindObjectsInactive.Include))
                timer.StopForEscape();
            if (flashlightAvailability != null) flashlightAvailability.DisableAllForEscape();
            if (OrderManager.Instance != null) OrderManager.Instance.enabled = false;
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
            GameSpeedControls speedControls = UnityEngine.Object.FindAnyObjectByType<GameSpeedControls>();
            if (speedControls != null) speedControls.SetSpeed(0f);
            else GameSpeedControls.SetSimulationSpeed(0f);
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
            GameSpeedControls.SetSimulationSpeed(1f);
            DayResourceLedger.EndRun(_runDurationSeconds);
            SceneManager.LoadScene(baseScene);
        }
    }
}

