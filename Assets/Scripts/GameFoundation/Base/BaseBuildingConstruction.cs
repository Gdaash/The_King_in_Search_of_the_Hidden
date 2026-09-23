using GameFoundation.Localization;
using GameFoundation.Saves;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameFoundation.Audio;

namespace GameFoundation.Base
{
    public sealed class BaseBuildingConstruction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string buildingId;
        [SerializeField] private string nameKey;
        [SerializeField, TextArea] private string fallbackName;
        [SerializeField] private string descriptionKey;
        [SerializeField, TextArea] private string fallbackDescription;
        [SerializeField] private Button buildingButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Text buildButtonLabel;
        [SerializeField] private ResourceType wood;
        [SerializeField, Min(0)] private int woodCost = 1;
        [SerializeField] private ResourceType stone;
        [SerializeField, Min(0)] private int stoneCost = 1;

        private bool built;
        private bool hovering;
        private bool languageSubscribed;
        private string SaveKey => "foundation.building." + buildingId + ".built";

        private void Awake()
        {
            if (buildingButton == null) buildingButton = GetComponent<Button>();
            built = SaveSlotPrefs.GetInt(SaveKey, 0) != 0;
            if (buildButton != null) buildButton.onClick.AddListener(Build);
            Refresh();
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            SubscribeLanguage();
            Refresh();
        }

        private void Start()
        {
            SubscribeLanguage();
            Refresh();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (languageSubscribed && LocalizationService.Instance != null)
                LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
            languageSubscribed = false;
            if (hovering) BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
        }

        private void OnDestroy()
        {
            if (buildButton != null) buildButton.onClick.RemoveListener(Build);
        }

        private void Build()
        {
            if (built || !CanAfford()) return;
            GlobalResourceManager resources = GlobalResourceManager.Instance;
            if (!resources.TrySpendResource(wood, woodCost)) return;
            if (!resources.TrySpendResource(stone, stoneCost))
            {
                resources.AddResource(wood, woodCost);
                return;
            }
            built = true;
            GameAudioController.PlayUI(GameAudioCue.BuildingComplete, 0.9f, 0.98f, 1.02f, 0.1f);
            SaveSlotPrefs.SetInt(SaveKey, 1);
            SaveSlotPrefs.Save();
            BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
            Refresh();
        }

        private bool CanAfford()
        {
            GlobalResourceManager resources = GlobalResourceManager.Instance;
            return resources != null && wood != null && stone != null &&
                resources.GetResourceAmount(wood) >= woodCost &&
                resources.GetResourceAmount(stone) >= stoneCost;
        }

        private void Refresh()
        {
            if (buildingButton != null) buildingButton.interactable = built;
            if (buildButton != null)
            {
                buildButton.gameObject.SetActive(!built);
                buildButton.interactable = !built && CanAfford();
            }
            if (buildButtonLabel != null)
                buildButtonLabel.text = Tr("base.building.build", "Строить");
            if (hovering) ShowTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (built) return;
            hovering = true;
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            BuildingConstructionTooltip.Instance?.Hide();
        }

        private void ShowTooltip()
        {
            BuildingConstructionTooltip.Instance?.Show(
                Tr(nameKey, fallbackName), Tr(descriptionKey, fallbackDescription),
                Tr("base.building.price", "Цена постройки"), wood, woodCost, stone, stoneCost);
        }

        private void OnResourceChanged(ResourceType _, int __) => Refresh();
        private void OnLanguageChanged() => Refresh();

        private void SubscribeLanguage()
        {
            if (languageSubscribed || LocalizationService.Instance == null) return;
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
            languageSubscribed = true;
        }

        private static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value.TrimEnd(':');
        }
    }
}
