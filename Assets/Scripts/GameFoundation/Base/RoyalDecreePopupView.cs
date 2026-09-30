using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>One decree entry for the initial Castle popup. More entries can be added as children later.</summary>
    public sealed class RoyalDecreePopupView : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button cautiousWarriorsButton;
        [SerializeField] private Text cautiousWarriorsButtonLabel;
        [SerializeField] private Text cautiousWarriorsState;
        [SerializeField] private Button finishOffEnemiesButton;
        [SerializeField] private Text finishOffEnemiesButtonLabel;
        [SerializeField] private Text finishOffEnemiesState;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (cautiousWarriorsButton != null) cautiousWarriorsButton.onClick.AddListener(ToggleCautiousWarriors);
            if (finishOffEnemiesButton != null) finishOffEnemiesButton.onClick.AddListener(ToggleFinishOffEnemies);
        }

        private void OnEnable()
        {
            RoyalDecreeService.Changed += OnDecreeChanged;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            RoyalDecreeService.Changed -= OnDecreeChanged;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (cautiousWarriorsButton != null) cautiousWarriorsButton.onClick.RemoveListener(ToggleCautiousWarriors);
            if (finishOffEnemiesButton != null) finishOffEnemiesButton.onClick.RemoveListener(ToggleFinishOffEnemies);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        public void ToggleCautiousWarriors() => RoyalDecreeService.Toggle(RoyalDecreeService.CautiousWarriors);
        public void ToggleFinishOffEnemies() => RoyalDecreeService.Toggle(RoyalDecreeService.FinishOffEnemies);

        private void OnDecreeChanged(string decreeId, bool _)
        {
            if (decreeId == RoyalDecreeService.CautiousWarriors || decreeId == RoyalDecreeService.FinishOffEnemies)
                Refresh();
        }

        private void Refresh()
        {
            RefreshDecree(RoyalDecreeService.CautiousWarriors, cautiousWarriorsState, cautiousWarriorsButtonLabel);
            RefreshDecree(RoyalDecreeService.FinishOffEnemies, finishOffEnemiesState, finishOffEnemiesButtonLabel);
        }

        private static void RefreshDecree(string decreeId, Text state, Text buttonLabel)
        {
            bool enabled = RoyalDecreeService.IsEnabled(decreeId);
            if (state != null)
            {
                state.text = Tr(enabled ? "base.castle.decree.enabled" : "base.castle.decree.disabled",
                    enabled ? "Указ включён" : "Указ выключен");
                state.color = enabled ? new Color(0.4f, 0.95f, 0.45f) : new Color(0.95f, 0.35f, 0.32f);
            }
            if (buttonLabel != null)
                buttonLabel.text = Tr(enabled ? "base.castle.decree.turn_off" : "base.castle.decree.turn_on",
                    enabled ? "Выключить" : "Включить");
        }

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
