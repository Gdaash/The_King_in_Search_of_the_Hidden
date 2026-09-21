using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Settings
{
    public sealed class SettingsPopupController : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Button russianButton;
        [SerializeField] private Button englishButton;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (masterSlider) masterSlider.onValueChanged.AddListener(SetMaster);
            if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusic);
            if (effectsSlider) effectsSlider.onValueChanged.AddListener(SetEffects);
            if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            if (vsyncToggle) vsyncToggle.onValueChanged.AddListener(SetVSync);
            if (russianButton) russianButton.onClick.AddListener(SetRussian);
            if (englishButton) englishButton.onClick.AddListener(SetEnglish);
        }

        private void OnEnable()
        {
            var service = GameSettingsService.Instance;
            if (service == null) return;
            if (masterSlider) masterSlider.SetValueWithoutNotify(service.Master);
            if (musicSlider) musicSlider.SetValueWithoutNotify(service.Music);
            if (effectsSlider) effectsSlider.SetValueWithoutNotify(service.Effects);
            if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(service.Fullscreen);
            if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(service.VSync);
        }

        private void OnDestroy()
        {
            if (closeButton) closeButton.onClick.RemoveListener(Close);
            if (masterSlider) masterSlider.onValueChanged.RemoveListener(SetMaster);
            if (musicSlider) musicSlider.onValueChanged.RemoveListener(SetMusic);
            if (effectsSlider) effectsSlider.onValueChanged.RemoveListener(SetEffects);
            if (fullscreenToggle) fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            if (vsyncToggle) vsyncToggle.onValueChanged.RemoveListener(SetVSync);
            if (russianButton) russianButton.onClick.RemoveListener(SetRussian);
            if (englishButton) englishButton.onClick.RemoveListener(SetEnglish);
        }

        public void Open() => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);
        private static void SetMaster(float value) => GameSettingsService.Instance?.SetMaster(value);
        private static void SetMusic(float value) => GameSettingsService.Instance?.SetMusic(value);
        private static void SetEffects(float value) => GameSettingsService.Instance?.SetEffects(value);
        private static void SetFullscreen(bool value) => GameSettingsService.Instance?.SetFullscreen(value);
        private static void SetVSync(bool value) => GameSettingsService.Instance?.SetVSync(value);
        private static void SetRussian() => LocalizationService.Instance?.SetLanguage("ru");
        private static void SetEnglish() => LocalizationService.Instance?.SetLanguage("en");
    }
}
