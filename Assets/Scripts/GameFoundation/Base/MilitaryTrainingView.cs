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
        private Image armorCostIcon;
        private Text armorCostAmount;
        private const float TrainingButtonWidth = 390;
        private readonly RectTransform[] actionStack = new RectTransform[4];
        private readonly float[] actionY = new float[4];
        private RectTransform trainingWindow;
        private Text deploymentHint, rules;
        private string alignedDeployment, alignedRules;
        private float alignedRulesY = float.NaN;

        private static ResourceType ArmorFor(ResourceType unit) => unit?.Id switch
        {
            "Archer" => ResourceCatalog.Find("LeatherArmor"),
            "Swordsman" or "Crossbowman" => ResourceCatalog.Find("IronArmor"),
            _ => null
        };

        internal bool DistrictPage { get; set; }
        internal Button ArmButton => armButton;
        public bool CanArmWarrior => CanArm(weapon, warrior);
        internal bool CanArm(ResourceType equipment, ResourceType unit)
        {
            var manager = GlobalResourceManager.Instance;
            var armor = ArmorFor(unit);
            return manager != null && human != null && unit != null &&
                CraftProduction.FreeResidents(human) > 0 && (equipment == null || manager.GetResourceAmount(equipment) > 0) &&
                (armor == null || manager.GetResourceAmount(armor) > 0) &&
                BuildingUpgradeService.GarrisonCount(manager) < BuildingUpgradeService.MilitaryCapacity(unit);
        }

        private Image[] equipmentImages, warriorImages;
        private Text[] recipeSeparators;
        private LocalizedText disarmHint;
        private void CacheRoleImages()
        {
            if (equipmentImages != null) return;
            var images = GetComponentsInChildren<Image>(true);
            recipeSeparators = System.Array.FindAll(GetComponentsInChildren<Text>(true), text => text.name == "Plus");
            disarmHint = transform.Find("Window/Disarm Hint")?.GetComponent<LocalizedText>();
            equipmentImages = System.Array.FindAll(images,
                image => weapon != null && weapon.resourceIcon != null && image.sprite == weapon.resourceIcon);
            warriorImages = System.Array.FindAll(images,
                image => warrior != null && warrior.resourceIcon != null && image.sprite == warrior.resourceIcon);
        }

        private void Awake()
        {
            if (armButton != null)
            {
                armButton.onClick.AddListener(Arm);

            }
            if (disarmButton != null) disarmButton.onClick.AddListener(Disarm);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            if (DistrictPage) { Refresh(); return; }
            if (transform.Find("Window") is not RectTransform window) return;
            trainingWindow = window;
            const float extraWidth = 48;
            ResizeWidth(window, window.rect.width + extraWidth);
            foreach (string name in new[] { "Artwork Frame", "Upgrade Details", "Stock", "Food Per Warrior", "Squad Food" })
                if (window.Find(name) is RectTransform rect) ResizeWidth(rect, rect.rect.width + extraWidth);
            foreach (string name in new[] { "Available Caption", "Arm Cost" })
                if (window.Find(name) is Transform group) group.gameObject.SetActive(false);
            if (window.Find("Unit Description Card") is RectTransform card)
            {
                card.anchoredPosition += Vector2.right * (extraWidth * .5f);
                rules = card.Find("Rules")?.GetComponent<Text>();
            }
            deploymentHint = window.Find("Deployment Hint")?.GetComponent<Text>();
            actionStack[0] = armButton?.transform as RectTransform;
            actionStack[1] = disarmButton?.transform as RectTransform;
            actionStack[2] = window.Find("Disarm Hint") as RectTransform;
            actionStack[3] = deploymentHint?.rectTransform;
            for (int i = 0; i < actionStack.Length; i++)
            {
                if (actionStack[i] == null) continue;
                ResizeWidth(actionStack[i], TrainingButtonWidth);
                actionY[i] = actionStack[i].anchoredPosition.y;
            }
            Refresh();
        }

        private void LateUpdate()
        {
            if (DistrictPage || deploymentHint == null || rules == null || !rules.isActiveAndEnabled) return;
            float rulesY = trainingWindow.InverseTransformPoint(rules.rectTransform.position).y;
            if (alignedDeployment == deploymentHint.text && alignedRules == rules.text &&
                Mathf.Approximately(alignedRulesY, rulesY)) return;
            alignedDeployment = deploymentHint.text;
            alignedRules = rules.text;
            alignedRulesY = rulesY;
            float offset = LastLineY(rules) - LastLineY(deploymentHint);
            if (float.IsNaN(offset)) return;
            float shift = deploymentHint.rectTransform.anchoredPosition.y - actionY[3] + offset;
            for (int i = 0; i < actionStack.Length; i++)
            {
                var rect = actionStack[i];
                if (rect == null) continue;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, actionY[i] + shift);
            }
        }

        private float LastLineY(Text text)
        {
            var generator = text.cachedTextGenerator;
            generator.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
            float bottom = float.PositiveInfinity;
            for (int i = 0; i < generator.verts.Count - 4; i++)
                bottom = Mathf.Min(bottom, generator.verts[i].position.y);
            if (float.IsInfinity(bottom)) return float.NaN;
            Vector3 point = text.rectTransform.TransformPoint(Vector3.up * (bottom / text.pixelsPerUnit));
            return trainingWindow.InverseTransformPoint(point).y;
        }

        private static void ResizeWidth(RectTransform rect, float width) =>
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        public void Close() => GameFoundation.UI.PopupMotion.Hide(gameObject);
        public void SelectUnit(ResourceType newWeapon, ResourceType newWarrior, UnitDescriptionDefinition description)
        {
            CacheRoleImages();
            foreach (var image in equipmentImages) ApplyIcon(image, newWeapon);
            foreach (var image in warriorImages) ApplyIcon(image, newWarrior);
            weapon=newWeapon;warrior=newWarrior;unitDescription=description;
            foreach (var separator in recipeSeparators) separator.gameObject.SetActive(weapon != null);
            disarmHint?.SetKey(weapon != null ? "unit.training.disarm_hint" : "unit.training.release_slinger_hint");
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
            var armor = ArmorFor(warrior);
            if (GlobalResourceManager.Instance?.TryExchangeResources(human, 1, weapon ?? human,
                weapon != null ? 1 : 0, warrior, 1, armor, armor != null ? 1 : 0) == true)
            {
                MilitaryExperienceService.GetStored(warrior, GlobalResourceManager.Instance.GetResourceAmount(warrior));
                GameNotifications.Post(GameFoundation.Localization.GameText.Get("Вооружён: ") + (unitDescription != null ? unitDescription.Title : GameFoundation.Localization.GameText.Get("Воин")), NotificationKind.Positive, warrior.resourceIcon);
                GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.RecruitWarrior, .7f);
            }
            Refresh();
        }

        public void Disarm()
        {
            DisarmStored(warrior, weapon, null);
            Refresh();
        }

        internal static void DisarmStored(ResourceType unit, ResourceType equipment, MilitaryProfile profile)
        {
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            using var notification = GameNotifications.BeginAction();
            var manager = GlobalResourceManager.Instance;
            var resident = ResourceCatalog.Find("Human");
            if (manager == null || unit == null || resident == null ||
                !manager.TrySpendResource(unit, 1)) return;
            manager.AddResource(resident, 1);
            if (equipment != null) manager.AddResource(equipment, 1);
            var armor = ArmorFor(unit);
            if (armor != null) manager.AddResource(armor, 1);
            if (profile != null) MilitaryExperienceService.Remove(profile);
            else MilitaryExperienceService.RemoveStored(unit);
            GameNotifications.Post(GameText.Get("Воин разоружён"), NotificationKind.Normal, unit.resourceIcon);
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.DisarmWarrior, .6f);
            BuildingUpgradeService.NotifyChanged();
        }

        private void Refresh()
        {
            var manager = GlobalResourceManager.Instance;
            int humans = CraftProduction.FreeResidents(human);
            int weapons = manager != null && weapon != null ? manager.GetResourceAmount(weapon) : 0;
            int warriors = manager != null && warrior != null ? manager.GetResourceAmount(warrior) : 0;
            if (armButton != null) armButton.interactable = CanArmWarrior;
            if (disarmButton != null) disarmButton.interactable = manager != null && warriors > 0;
            var armor = ArmorFor(warrior);
            bool hasArmor = armor == null || manager != null && manager.GetResourceAmount(armor) > 0;
            RefreshArmorPrice(armor, hasArmor);
            bool garrisonFull = BuildingUpgradeService.GarrisonCount(manager) >= BuildingUpgradeService.MilitaryCapacity(warrior);
            BaseDisabledButtonHint.Set(armButton, humans <= 0 ? BaseDisabledButtonHint.Reason.Residents : garrisonFull ? BaseDisabledButtonHint.Reason.MilitaryCapacity : BaseDisabledButtonHint.Reason.Weapons,
                reasonText: garrisonFull && humans > 0 ? GameText.Get("Гарнизон заполнен") :
                    humans > 0 && !hasArmor ? GameText.Get("Недостаточно ресурсов") : null);
            BaseDisabledButtonHint.Set(disarmButton, BaseDisabledButtonHint.Reason.Warriors);
            if (humanAmount != null) humanAmount.text = humans.ToString();
            if (weaponAmount != null)
            {
                weaponAmount.transform.parent.gameObject.SetActive(weapon != null);
                weaponAmount.text = weapons.ToString();
            }
            if (warriorAmount != null) warriorAmount.text = warriors.ToString();
            if (humanCostAmount != null) { humanCostAmount.text = "1"; humanCostAmount.color = humans > 0 ? affordableColor : unaffordableColor; }
            if (weaponCostAmount != null) { weaponCostAmount.gameObject.SetActive(weapon != null); weaponCostAmount.text = "1"; weaponCostAmount.color = weapons > 0 ? affordableColor : unaffordableColor; }
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

        private void RefreshArmorPrice(ResourceType armor, bool affordable)
        {
            if (armButton == null || !armButton.TryGetComponent<PriceButtonLayout>(out var layout)) return;
            if (armorCostIcon == null)
            {
                if (armor == null || layout.prices.Length < 2) return;
                armorCostIcon = Instantiate(layout.prices[0].GetComponent<Image>(), layout.visual);
                armorCostAmount = Instantiate(layout.prices[1].GetComponent<Text>(), layout.visual);
                armorCostIcon.name = "Armor cost icon";
                armorCostAmount.name = "Armor cost amount";
                armorCostIcon.raycastTarget = armorCostAmount.raycastTarget = false;
                var prices = layout.prices;
                System.Array.Resize(ref prices, prices.Length + 2);
                prices[^2] = armorCostIcon.rectTransform;
                prices[^1] = armorCostAmount.rectTransform;
                layout.prices = prices;
            }
            armorCostIcon.gameObject.SetActive(armor != null);
            armorCostAmount.gameObject.SetActive(armor != null);
            layout.horizontalPadding = armor != null ? 12 : 20;
            layout.priceGroupGap = armor != null ? 6 : 14;
            layout.iconAmountGap = armor != null ? 2 : 6;
            layout.labelGap = armor != null ? 6 : 16;
            ApplyIcon(armorCostIcon, armor);
            armorCostAmount.text = "1";
            armorCostAmount.color = affordable ? affordableColor : unaffordableColor;
        }

        private RectTransform districtStock;

        internal void ConfigureDistrictStock(RectTransform stock)
        {
            districtStock = stock;
            var background = stock.GetComponent<Image>() ?? stock.gameObject.AddComponent<Image>();
            background.color = new Color(.32f, .27f, .36f, .23f);
            background.raycastTarget = false;
            var label = stock.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                if (label.TryGetComponent<LocalizedText>(out var localized)) localized.SetKey("unit.training.hired");
                else label.text = GameText.Get("Нанято:");
            }
        }

        private void AlignSummaryColumns()
        {
            if (districtStock != null && warriorIcon != null)
            {
                float leftInset = districtStock.rect.xMin + 18f;
                float amountStart = districtStock.rect.xMax - 56f;
                float iconPosition = amountStart - 12f - warriorIcon.rectTransform.rect.width * .5f;
                AlignSummaryRow(districtStock, warriorIcon, warriorAmount, leftInset, 205f, iconPosition, amountStart, 40f);
                return;
            }
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
            if (image == null) return;
            image.enabled = resource != null;
            if (resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
        }
    }
}
