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
                    manager.GetResourceAmount(human) > 0 && manager.GetResourceAmount(weapon) > 0 &&
                    manager.GetResourceAmount(warrior) < BuildingUpgradeService.MilitaryCapacity(warrior);
            }
        }

        private void Awake()
        {
            if (armButton != null) armButton.onClick.AddListener(Arm);
            if (disarmButton != null) disarmButton.onClick.AddListener(Disarm);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Close() => gameObject.SetActive(false);
        public void SelectUnit(ResourceType newWeapon, ResourceType newWarrior, UnitDescriptionDefinition description)
        {
            var oldWeapon = weapon;
            var oldWarrior = warrior;
            foreach(var image in GetComponentsInChildren<Image>(true))
            {
                if(oldWeapon != null && image.sprite == oldWeapon.resourceIcon) ResourceIconSizing.Apply(image,newWeapon.resourceIcon);
                else if(oldWarrior != null && image.sprite == oldWarrior.resourceIcon) ResourceIconSizing.Apply(image,newWarrior.resourceIcon);
            }
            weapon=newWeapon;warrior=newWarrior;unitDescription=description;
            foreach(var view in GetComponentsInChildren<UnitDescriptionView>(true)) view.Show(description);
            Refresh();
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            BuildingUpgradeService.Changed += Refresh;
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            BuildingUpgradeService.Changed -= Refresh;
            if (localization != null) localization.LanguageChanged -= Refresh;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        public void Arm()
        {
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            using var notification = GameNotifications.BeginAction();
            if (!CanArmWarrior) return;
            if (GlobalResourceManager.Instance?.TryExchangeResources(human, 1, weapon, 1, warrior, 1) == true)
            {
                MilitaryExperienceService.GetStored(warrior, GlobalResourceManager.Instance.GetResourceAmount(warrior));
                GameNotifications.Post(GameFoundation.Localization.GameText.Get("Вооружён: ") + (unitDescription != null ? unitDescription.Title : GameFoundation.Localization.GameText.Get("Воин")), NotificationKind.Positive, warrior.resourceIcon);
                GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.RecruitWarrior, .7f);
            }
            Refresh();
        }

        public void Disarm()
        {
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            using var notification = GameNotifications.BeginAction();
            var manager = GlobalResourceManager.Instance;
            if (manager != null && warrior != null && human != null && weapon != null &&
                manager.TrySpendResource(warrior, 1))
            {
                manager.AddResource(human, 1);
                manager.AddResource(weapon, 1);
                MilitaryExperienceService.RemoveStored(warrior);
                GameNotifications.Post(GameFoundation.Localization.GameText.Get("Воин разоружён"), NotificationKind.Normal, warrior.resourceIcon);
                GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.DisarmWarrior, .6f);
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
            BaseDisabledButtonHint.Set(armButton, humans <= 0 ? BaseDisabledButtonHint.Reason.Residents : warriors >= BuildingUpgradeService.MilitaryCapacity(warrior) ? BaseDisabledButtonHint.Reason.MilitaryCapacity : BaseDisabledButtonHint.Reason.Weapons);
            BaseDisabledButtonHint.Set(disarmButton, BaseDisabledButtonHint.Reason.Warriors);
            if (humanAmount != null) humanAmount.text = humans.ToString();
            if (weaponAmount != null) weaponAmount.text = weapons.ToString();
            if (warriorAmount != null) warriorAmount.text = warriors + " / " + BuildingUpgradeService.MilitaryCapacity(warrior);
            if (humanCostAmount != null) { humanCostAmount.text = "1"; humanCostAmount.color = humans > 0 ? affordableColor : unaffordableColor; }
            if (weaponCostAmount != null) { weaponCostAmount.text = "1"; weaponCostAmount.color = weapons > 0 ? affordableColor : unaffordableColor; }
            ApplyIcon(humanIcon, human);
            ApplyIcon(weaponIcon, weapon);
            ApplyIcon(warriorIcon, warrior);
            // DayCycleService consumes one berry for every resident, including each warrior.
            string daily = UnitDescriptionText.Get("unit.training.food_daily", GameFoundation.Localization.GameText.Get("{0} / день"));
            if (foodPerUnitAmount != null) foodPerUnitAmount.text = string.Format(daily, 1);
            if (totalFoodAmount != null) totalFoodAmount.text = string.Format(daily, Mathf.Max(0, warriors));
            if (unitDescription != null)
            {
                ApplyIcon(foodPerUnitIcon, unitDescription.food);
                ApplyIcon(totalFoodIcon, unitDescription.food);
            }
            AlignSummaryColumns();
        }

        private void AlignSummaryColumns()
        {
            if (foodPerUnitIcon == null || warriorIcon == null || totalFoodIcon == null) return;
            var reference = foodPerUnitIcon.transform.parent as RectTransform;
            if (reference == null) return;
            float iconWidth = Mathf.Max(warriorIcon.rectTransform.rect.width,
                Mathf.Max(foodPerUnitIcon.rectTransform.rect.width, totalFoodIcon.rectTransform.rect.width));
            float amountWidth = 104f;
            foreach (var value in new[] { warriorAmount, foodPerUnitAmount, totalFoodAmount })
                if (value != null) amountWidth = Mathf.Max(amountWidth, Mathf.Ceil(value.preferredWidth) + 4f);
            const float padding = 4f, gap = 12f;
            float left = reference.rect.xMin + padding;
            float amountLeft = reference.rect.xMax - padding - amountWidth;
            float iconCenter = amountLeft - gap - iconWidth * .5f;
            float labelWidth = Mathf.Max(64f, iconCenter - iconWidth * .5f - gap - left);
            AlignSummaryRow(reference, warriorIcon, warriorAmount, left, labelWidth, iconCenter, amountLeft, amountWidth);
            AlignSummaryRow(reference, foodPerUnitIcon, foodPerUnitAmount, left, labelWidth, iconCenter, amountLeft, amountWidth);
            AlignSummaryRow(reference, totalFoodIcon, totalFoodAmount, left, labelWidth, iconCenter, amountLeft, amountWidth);
        }

        private static void AlignSummaryRow(RectTransform reference, Image icon, Text amount,
            float left, float labelWidth, float iconCenter, float amountLeft, float amountWidth)
        {
            Transform row = icon.transform.parent;
            if (row.Find("Label") == null && row.parent != null && row.parent.Find("Label") != null) row = row.parent;
            var labelTransform = row.Find("Label");
            var label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
            if (label != null)
            {
                PlaceColumn(label.rectTransform, reference, left + labelWidth * .5f, labelWidth);
                label.alignment = TextAnchor.MiddleLeft;
            }
            PlaceColumn(icon.rectTransform, reference, iconCenter);
            if (amount != null)
            {
                PlaceColumn(amount.rectTransform, reference, amountLeft + amountWidth * .5f, amountWidth);
                amount.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static void PlaceColumn(RectTransform rect, RectTransform reference, float x, float width = -1f)
        {
            Vector3 world = reference.TransformPoint(new Vector3(x, 0, 0));
            Vector3 local = rect.parent.InverseTransformPoint(world);
            var position = rect.localPosition;
            position.x = local.x;
            rect.localPosition = position;
            if (width > 0) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void ApplyIcon(Image image, ResourceType resource)
        {
            if (image == null || resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
        }
    }
}
