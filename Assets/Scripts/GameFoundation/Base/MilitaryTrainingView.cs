using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;
using GameFoundation.Localization;
using GameFoundation.UI;

namespace GameFoundation.Base
{
    public sealed class MilitaryTrainingView : MonoBehaviour
    {
        [Header("Ресурсы")]
        [SerializeField] private ResourceType human;
        [SerializeField] private ResourceType weapon;
        [SerializeField] private ResourceType warrior;

        [Header("Интерфейс")]
        [SerializeField] private Button armButton;
        [SerializeField] private Button disarmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image humanIcon;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image warriorIcon;
        [SerializeField] private Text humanAmount;
        [SerializeField] private Text weaponAmount;
        [SerializeField] private Text warriorAmount;
        [SerializeField] private Text humanCostAmount;
        [SerializeField] private Text weaponCostAmount;
        [Header("Пища отряда")]
        [SerializeField] private UnitDescriptionDefinition unitDescription;
        [SerializeField] private Image foodPerUnitIcon;
        [SerializeField] private Image totalFoodIcon;
        [SerializeField] private Text foodPerUnitAmount;
        [SerializeField] private Text totalFoodAmount;
        [SerializeField] private Color affordableColor = new(.55f, .85f, .54f);
        [SerializeField] private Color unaffordableColor = new(1f, .38f, .37f);
        private LocalizationService localization;

        public bool CanArmWarrior
        {
            get
            {
                GlobalResourceManager manager = GlobalResourceManager.Instance;
                return manager != null && human != null && weapon != null && warrior != null &&
                    manager.GetResourceAmount(human) > 0 && manager.GetResourceAmount(weapon) > 0;
            }
        }

        private void Awake()
        {
            if (armButton != null) armButton.onClick.AddListener(Arm);
            if (disarmButton != null) disarmButton.onClick.AddListener(Disarm);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Close() => gameObject.SetActive(false);

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (localization != null) localization.LanguageChanged -= Refresh;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        public void Arm()
        {
            if (GlobalResourceManager.Instance?.TryExchangeResources(human, 1, weapon, 1, warrior, 1) == true)
                MilitaryExperienceService.GetStored(warrior, GlobalResourceManager.Instance.GetResourceAmount(warrior));
            Refresh();
        }

        public void Disarm()
        {
            var manager = GlobalResourceManager.Instance;
            if (manager != null && warrior != null && human != null && weapon != null &&
                manager.TrySpendResource(warrior, 1))
            {
                manager.AddResource(human, 1);
                manager.AddResource(weapon, 1);
                MilitaryExperienceService.RemoveStored(warrior);
            }
            Refresh();
        }

        private void Refresh()
        {
            var manager = GlobalResourceManager.Instance;
            int humans = manager != null && human != null ? manager.GetResourceAmount(human) : 0;
            int weapons = manager != null && weapon != null ? manager.GetResourceAmount(weapon) : 0;
            int warriors = manager != null && warrior != null ? manager.GetResourceAmount(warrior) : 0;
            if (armButton != null) armButton.interactable = CanArmWarrior;
            if (disarmButton != null) disarmButton.interactable = manager != null && warriors > 0;
            if (humanAmount != null) humanAmount.text = humans.ToString();
            if (weaponAmount != null) weaponAmount.text = weapons.ToString();
            if (warriorAmount != null) warriorAmount.text = warriors.ToString();
            if (humanCostAmount != null) { humanCostAmount.text = "1"; humanCostAmount.color = humans > 0 ? affordableColor : unaffordableColor; }
            if (weaponCostAmount != null) { weaponCostAmount.text = "1"; weaponCostAmount.color = weapons > 0 ? affordableColor : unaffordableColor; }
            ApplyIcon(humanIcon, human);
            ApplyIcon(weaponIcon, weapon);
            ApplyIcon(warriorIcon, warrior);
            // DayCycleService consumes one berry for every resident, including each warrior.
            string daily = UnitDescriptionText.Get("unit.training.food_daily", "{0} / день");
            if (foodPerUnitAmount != null) foodPerUnitAmount.text = string.Format(daily, 1);
            if (totalFoodAmount != null) totalFoodAmount.text = string.Format(daily, Mathf.Max(0, warriors));
            if (unitDescription != null)
            {
                ApplyIcon(foodPerUnitIcon, unitDescription.food);
                ApplyIcon(totalFoodIcon, unitDescription.food);
            }
        }

        private static void ApplyIcon(Image image, ResourceType resource)
        {
            if (image == null || resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
        }
    }
}
