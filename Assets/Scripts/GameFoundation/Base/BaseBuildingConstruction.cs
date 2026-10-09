using GameFoundation.Localization;
using GameFoundation.Saves;
using GameFoundation.Quests;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameFoundation.Audio;

namespace GameFoundation.Base
{
    public sealed class BaseBuildingConstruction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string buildingId;
        [SerializeField] private ContentUnlockDefinition requiredUnlock;
        public bool ConstructionUnlocked => ContentUnlocks.IsUnlocked(requiredUnlock);
        public ContentUnlockDefinition RequiredUnlock => requiredUnlock;
        public Button ConstructionButton => buildButton;
        [SerializeField] private string nameKey;
        [SerializeField, TextArea] private string fallbackName;
        [SerializeField] private string descriptionKey;
        [SerializeField, TextArea] private string fallbackDescription;
        [SerializeField] private Button buildingButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Text buildButtonLabel;
        // Keep the serialized names for existing prefab and balance-sheet references.
        [SerializeField, InspectorName("Ресурс 1")] private ResourceType wood;
        [SerializeField, Min(0), InspectorName("Количество ресурса 1")] private int woodCost = 1;
        [SerializeField, InspectorName("Ресурс 2")] private ResourceType stone;
        [SerializeField, Min(0), InspectorName("Количество ресурса 2")] private int stoneCost = 1;
        [SerializeField] private BuildingConstructionConfirmation confirmation;
        [SerializeField] private BuildingConstructionEffect constructionEffect;

        private bool built;
        private bool hovering;
        private bool languageSubscribed;
        private CanvasGroup availabilityGroup;
        private string SaveKey => "foundation.building." + buildingId + ".built";
        public bool IsBuilt => built;
        public string BuildingId => buildingId;
        public string DisplayName => Tr(nameKey, fallbackName);
        public ResourceType Wood => wood;
        public ResourceType Stone => stone;
        public int WoodCost => woodCost;
        public int StoneCost => stoneCost;
        public bool CanAffordConstruction => !built && ConstructionUnlocked && CanAfford();

        private void Awake()
        {
            availabilityGroup = GetComponent<CanvasGroup>();
            if (availabilityGroup == null) availabilityGroup = gameObject.AddComponent<CanvasGroup>();
            if (buildingButton == null) buildingButton = GetComponent<Button>();
            built = SaveSlotPrefs.GetInt(SaveKey, 0) != 0;
            if (buildButton != null) buildButton.onClick.AddListener(RequestBuild);
            Refresh();
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            ContentUnlocks.Changed += Refresh;
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
            ContentUnlocks.Changed -= Refresh;
            if (languageSubscribed && LocalizationService.Instance != null)
                LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
            languageSubscribed = false;
            if (hovering) BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
        }

        private void OnDestroy()
        {
            if (buildButton != null) buildButton.onClick.RemoveListener(RequestBuild);
        }

        private void RequestBuild()
        {
            if (built || !ConstructionUnlocked || confirmation == null) return;
            BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
            confirmation.Open(this);
        }

        public void ConfirmBuild()
        {
            using var notification = GameFoundation.UI.GameNotifications.BeginAction();
            if (built || !ConstructionUnlocked || !CanAfford()) return;
            GlobalResourceManager resources = GlobalResourceManager.Instance;
            if (woodCost > 0 && !resources.TrySpendResource(wood, woodCost)) return;
            if (stoneCost > 0 && !resources.TrySpendResource(stone, stoneCost))
            {
                if (woodCost > 0) resources.AddResource(wood, woodCost);
                return;
            }
            built = true;
            GameFoundation.UI.GameNotifications.Post("Построено: " + Tr(nameKey, fallbackName), GameFoundation.UI.NotificationKind.Positive);
            GameAudioController.PlayUI(GameAudioCue.BuildingComplete, 0.9f, 0.98f, 1.02f, 0.1f);
            SaveSlotPrefs.SetInt(SaveKey, 1);
            if (buildingId == "refugees") GameFoundation.MetaProgression.DayCycleService.Instance?.EnsureFirstRefugees();
            SaveSlotPrefs.Save();
            BuildingUpgradeService.NotifyChanged();
            BuildingConstructionTooltip.Instance?.Hide();
            hovering = false;
            Refresh();
            constructionEffect?.Play();
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
            bool visible = built || ConstructionUnlocked;
            if (availabilityGroup != null)
            {
                availabilityGroup.alpha = visible ? 1f : 0f;
                availabilityGroup.blocksRaycasts = visible;
            }
            if (buildingButton != null) buildingButton.interactable = built;
            if (buildButton != null)
            {
                buildButton.gameObject.SetActive(!built && ConstructionUnlocked);
                buildButton.interactable = !built && ConstructionUnlocked && CanAfford();
            }
            if (buildButtonLabel != null)
                buildButtonLabel.text = Tr(nameKey, fallbackName);
            if (hovering) ShowTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (built || !ConstructionUnlocked) return;
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
            if (!ConstructionUnlocked) { BuildingConstructionTooltip.Instance?.Hide(); return; }
            string description = Tr(descriptionKey, fallbackDescription);
            if (buildingId == "housing")
                description += "\n" + string.Format(Tr("base.building.housing.extra_places", "Увеличивает количество жилых мест для людей на {0}."),
                    BuildingUpgradeService.Catalog?.Find("housing")?.constructionCapacity ?? 0);
            BuildingConstructionTooltip.Instance?.Show(
                Tr(nameKey, fallbackName), description,
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
