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

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (cautiousWarriorsButton != null) cautiousWarriorsButton.onClick.AddListener(ToggleCautiousWarriors);
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
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        public void ToggleCautiousWarriors() => RoyalDecreeService.Toggle(RoyalDecreeService.CautiousWarriors);

        private void OnDecreeChanged(string decreeId, bool _) { if (decreeId == RoyalDecreeService.CautiousWarriors) Refresh(); }

        private void Refresh()
        {
            bool enabled = RoyalDecreeService.IsEnabled(RoyalDecreeService.CautiousWarriors);
            if (cautiousWarriorsState != null)
            {
                cautiousWarriorsState.text = Tr(enabled ? "base.castle.decree.enabled" : "base.castle.decree.disabled",
                    enabled ? "Указ включён" : "Указ выключен");
                cautiousWarriorsState.color = enabled ? new Color(0.4f, 0.95f, 0.45f) : new Color(0.95f, 0.35f, 0.32f);
            }
            if (cautiousWarriorsButtonLabel != null)
                cautiousWarriorsButtonLabel.text = Tr(enabled ? "base.castle.decree.turn_off" : "base.castle.decree.turn_on",
                    enabled ? "Выключить" : "Включить");
        }

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
