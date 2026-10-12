using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameFoundation.Settings
{
    public sealed class GameMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [SerializeField] private Text title, description, settingsLabel, exitLabel, cancelLabel;
        [SerializeField] private Button settingsButton, exitButton, cancelButton;
        private SettingsPopupController settings;
        private Transform settingsParent;
        private int settingsSibling;
        private bool confirming, showingSettings, opened, leaving;
        private float previousSpeed;
        public bool IsConfirming => confirming;
        private void Awake()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(RequestExit);
            cancelButton.onClick.AddListener(Cancel);
        }
        public void Open(SettingsPopupController popup)
        {
            if (opened) return;
            settings = popup; previousSpeed = GameSpeedControls.SimulationSpeed;
            opened = true; confirming = showingSettings = leaving = false;
            gameObject.SetActive(true); window.SetActive(true); transform.SetAsLastSibling();
            GameSpeedControls.SetSimulationSpeed(0);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            if (LocalizationService.Instance) LocalizationService.Instance.LanguageChanged += Refresh;
            Refresh();
        }
        private void Update()
        {
            if (showingSettings && (!settings || !settings.gameObject.activeSelf))
            { showingSettings = false; RestoreSettingsParent(); window.SetActive(true); Refresh(); }
            if (Input.GetKeyDown(KeyCode.Escape)) { if (showingSettings) settings.Close(); else Cancel(); }
        }
        public void OpenSettings()
        {
            if (!settings) return;
            showingSettings = true; window.SetActive(false);
            settingsParent = settings.transform.parent; settingsSibling = settings.transform.GetSiblingIndex();
            settings.transform.SetParent(transform, false);
            settings.transform.SetAsLastSibling(); settings.Open();
        }
        private void RestoreSettingsParent()
        {
            if (settings && settingsParent) { settings.transform.SetParent(settingsParent, false); settings.transform.SetSiblingIndex(settingsSibling); }
            settingsParent = null;
        }
        public void RequestExit()
        {
            if (leaving) return;
            var battle = FindAnyObjectByType<WorldEscapeController>();
            if (battle && !confirming) { confirming = true; Refresh(); return; }
            leaving = true;
            if (battle) battle.ExitToMainMenu();
            else { SaveSlotPrefs.Save(); SceneManager.LoadScene("MainMenu"); GameSpeedControls.SetSimulationSpeed(1); }
        }
        public void Cancel()
        {
            if (confirming) { confirming = false; Refresh(); return; }
            gameObject.SetActive(false);
        }
        private void Refresh()
        {
            title.text = GameText.Get(confirming ? "Выйти в главное меню?" : "Меню");
            description.gameObject.SetActive(confirming);
            description.text = GameText.Get("Все люди, оставшиеся в портале, погибнут. Продолжить?");
            settingsButton.gameObject.SetActive(!confirming);
            settingsLabel.text = GameText.Get("Настройки");
            exitLabel.text = GameText.Get(confirming ? "Выйти" : "В главное меню");
            cancelLabel.text = GameText.Get(confirming ? "Отмена" : "Продолжить игру");
        }
        private void OnDisable()
        {
            if (LocalizationService.Instance) LocalizationService.Instance.LanguageChanged -= Refresh;
            RestoreSettingsParent();
            if (opened && !leaving) GameSpeedControls.SetSimulationSpeed(previousSpeed);
            opened = false;
        }
    }
}
