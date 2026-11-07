using System;
using System.Collections.Generic;
using GameFoundation.Localization;
using GameFoundation.UI;
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

        private Dropdown resolutionDropdown;
        private Vector2Int[] resolutions;
        private int currentResolutionOffset;
        private Text resolutionLabel;
        private LocalizationService localization;
        private RectTransform resolutionRow;

        public const float ResolutionRowHeight = 64f;

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
            CreateResolutionRow();
        }

        private void OnEnable()
        {
            if (GetComponent<LanguageSelectionList>() != null)
            {
                // Legacy scene overrides may still enable the two original language buttons.
                if (russianButton) russianButton.gameObject.SetActive(false);
                if (englishButton) englishButton.gameObject.SetActive(false);
            }
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += RefreshResolutionLabel;
            RefreshResolutionLabel();
            RefreshResolutions();
            var service = GameSettingsService.Instance;
            if (service == null) return;
            if (masterSlider) masterSlider.SetValueWithoutNotify(service.Master);
            if (musicSlider) musicSlider.SetValueWithoutNotify(service.Music);
            if (effectsSlider) effectsSlider.SetValueWithoutNotify(service.Effects);
            if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(service.Fullscreen);
            if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(service.VSync);
        }

        private void OnDisable()
        {
            if (localization != null) localization.LanguageChanged -= RefreshResolutionLabel;
            if (resolutionDropdown != null) resolutionDropdown.Hide();
        }

        private void CreateResolutionRow()
        {
            if (!fullscreenToggle) return;
            resolutionRow = new GameObject("Resolution Row", typeof(RectTransform)).GetComponent<RectTransform>();
            resolutionRow.SetParent(transform, false);
            resolutionRow.sizeDelta = new Vector2(550, 48);

            var theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
            var row = DefaultControls.CreateDropdown(new DefaultControls.Resources {
                standard = theme != null ? theme.textButtonSprite : null,
                background = theme != null ? theme.popupPanelSprite : null
            });
            row.name = "Resolution";
            row.transform.SetParent(resolutionRow, false);
            resolutionDropdown = row.GetComponent<Dropdown>();
            var rect = (RectTransform)row.transform;
            rect.sizeDelta = new Vector2(224, 46);
            rect.anchoredPosition = new Vector2(128, 0);
            resolutionDropdown.template.sizeDelta = new Vector2(0, 250);
            var listBackground = resolutionDropdown.template.GetComponent<Image>();
            if (theme != null) listBackground.sprite = theme.popupPanelSprite;
            var scroll = resolutionDropdown.template.GetComponent<ScrollRect>();
            var item = resolutionDropdown.itemText.GetComponentInParent<Toggle>(true);
            ((RectTransform)item.transform).sizeDelta = new Vector2(0, 38);
            var colors = item.colors;
            colors.highlightedColor = new Color(.73f, .65f, .82f);
            item.colors = colors;
            item.targetGraphic.color = new Color(.28f, .24f, .33f);
            item.graphic.rectTransform.sizeDelta = new Vector2(8, 8);
            item.graphic.color = theme != null ? theme.positiveLabelColor : Color.green;
            scroll.verticalScrollbar.GetComponent<Image>().color = new Color(.14f, .11f, .17f);
            scroll.verticalScrollbar.targetGraphic.color = new Color(.50f, .43f, .56f);
            var arrow = row.transform.Find("Arrow");
            if (arrow != null)
            {
                var image = arrow.GetComponent<Image>();
                image.enabled = false;
                var indicator = new GameObject("Indicator", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                indicator.transform.SetParent(arrow, false);
                indicator.rectTransform.sizeDelta = new Vector2(26, 34);
                indicator.font = resolutionDropdown.captionText.font;
                indicator.fontSize = 20;
                indicator.text = "v";
                indicator.color = theme != null ? theme.neutralLabelColor : Color.white;
                indicator.alignment = TextAnchor.MiddleCenter;
                indicator.raycastTarget = false;
            }
            var font = fullscreenToggle.GetComponentInChildren<Text>(true)?.font;
            foreach (var text in new[] { resolutionDropdown.captionText, resolutionDropdown.itemText })
            {
                if (font != null) text.font = font;
                text.fontSize = 24;
                text.color = theme != null ? theme.neutralLabelColor : Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }
            resolutionDropdown.captionText.rectTransform.offsetMax = new Vector2(-25, -7);

            resolutionLabel = new GameObject("Resolution Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            resolutionLabel.transform.SetParent(resolutionRow, false);
            resolutionLabel.rectTransform.sizeDelta = new Vector2(254, 46);
            resolutionLabel.rectTransform.anchoredPosition = new Vector2(-146, 0);
            if (font != null) resolutionLabel.font = font;
            resolutionLabel.fontSize = 22;
            resolutionLabel.alignment = TextAnchor.MiddleCenter;
            resolutionLabel.color = resolutionDropdown.captionText.color;
            resolutionLabel.raycastTarget = false;
            resolutionDropdown.onValueChanged.AddListener(SelectResolution);
            PositionResolutionRow(((RectTransform)fullscreenToggle.transform).anchoredPosition.y - 60);
        }

        public void PositionResolutionRow(float y)
        {
            if (resolutionRow != null) resolutionRow.anchoredPosition = new Vector2(0, y);
        }

        private void RefreshResolutionLabel()
        {
            if (resolutionLabel == null) return;
            const string key = "base.settings_popup.resolution";
            string translated = localization?.Get(key);
            resolutionLabel.text = string.IsNullOrEmpty(translated) || translated == key ? "Разрешение экрана" : translated;
        }

        private void RefreshResolutions()
        {
            if (resolutionDropdown == null) return;
            resolutions = GameSettingsService.AvailableResolutions();
            var current = new Vector2Int(Screen.width, Screen.height);
            int selected = Array.IndexOf(resolutions, current);
            currentResolutionOffset = selected < 0 ? 1 : 0;
            var options = new List<string>();
            if (currentResolutionOffset == 1) options.Add(ResolutionText(current));
            foreach (var size in resolutions) options.Add(ResolutionText(size));
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, selected));
            if (selected < 0) resolutionDropdown.captionText.text = ResolutionText(current);
            resolutionDropdown.interactable = resolutions.Length > 0 && (resolutions.Length > 1 || selected < 0);
        }

        private void SelectResolution(int value)
        {
            int index = value - currentResolutionOffset;
            if (index < 0 || index >= resolutions.Length) { RefreshResolutions(); return; }
            if (GameSettingsService.Instance?.SetResolution(resolutions[index]) != true)
                RefreshResolutions();
        }

        private static string ResolutionText(Vector2Int size) => $"{size.x} × {size.y}";

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
