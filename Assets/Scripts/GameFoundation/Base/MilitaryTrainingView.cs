using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Image humanIcon;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image warriorIcon;
        [SerializeField] private Text humanAmount;
        [SerializeField] private Text weaponAmount;
        [SerializeField] private Text warriorAmount;

        private void Awake()
        {
            if (armButton != null) armButton.onClick.AddListener(Arm);
            if (disarmButton != null) disarmButton.onClick.AddListener(Disarm);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        public void Arm()
        {
            GlobalResourceManager.Instance?.TryExchangeResources(human, 1, weapon, 1, warrior, 1);
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
            }
            Refresh();
        }

        private void Refresh()
        {
            var manager = GlobalResourceManager.Instance;
            int humans = manager != null && human != null ? manager.GetResourceAmount(human) : 0;
            int weapons = manager != null && weapon != null ? manager.GetResourceAmount(weapon) : 0;
            int warriors = manager != null && warrior != null ? manager.GetResourceAmount(warrior) : 0;
            if (armButton != null) armButton.interactable = manager != null && humans > 0 && weapons > 0;
            if (disarmButton != null) disarmButton.interactable = manager != null && warriors > 0;
            if (humanAmount != null) humanAmount.text = humans.ToString();
            if (weaponAmount != null) weaponAmount.text = weapons.ToString();
            if (warriorAmount != null) warriorAmount.text = warriors.ToString();
            ApplyIcon(humanIcon, human);
            ApplyIcon(weaponIcon, weapon);
            ApplyIcon(warriorIcon, warrior);
        }

        private static void ApplyIcon(Image image, ResourceType resource)
        {
            if (image == null || resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
        }
    }
}
