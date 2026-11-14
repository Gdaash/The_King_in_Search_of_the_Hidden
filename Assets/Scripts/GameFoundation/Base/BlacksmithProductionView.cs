using GameFoundation.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BlacksmithProductionView : MonoBehaviour
    {
        [System.Serializable]
        private sealed class RecipeView
        {
            public ResourceType input;
            [Min(1)] public int inputAmount = 1;
            public ResourceType output;
            [Min(1)] public int outputAmount = 1;
            public Button produceButton;
            public Image inputIcon;
            public Text inputAmountText;
            public Image outputIcon;
            public Text outputAmountText;
            public ResourceType fuel;
            public Text fuelAmountText;
        }

        [SerializeField] private RecipeView swordRecipe;
        [SerializeField] private RecipeView bowRecipe;
        [SerializeField] private RecipeView spearRecipe;
        private RecipeView leatherArmorRecipe, tanneryRecipe, coalRecipe, ingotRecipe, ironArmorRecipe, crossbowRecipe;
        [SerializeField] private Color enoughColor = new Color(0.55f, 0.85f, 0.35f);
        [SerializeField] private Color notEnoughColor = new Color(0.95f, 0.38f, 0.35f);
        [SerializeField] private Color stockColor = new Color(0.94f, 0.91f, 0.82f);

        public bool CanProduceAny => CanImproveEquipment() || CraftProduction.CanQueue(CraftProduction.Carpentry, ResourceCatalog.Find("Wood"), WarehouseCartPurchaseView.DefaultWoodCost) ||
            CanProduce(bowRecipe) || CanProduce(spearRecipe) ||
            (transform.Find("Window/Recipes Scroll") == null || BuildingUpgradeService.IsBuilt(CraftQuarterLayout.Smithy)) && CanProduce(swordRecipe) ||
            BuildingUpgradeService.CanBuildAnnex(CraftQuarterLayout.Smithy) ||
            BuildingUpgradeService.CanBuildAnnex(CraftQuarterLayout.LeatherShop) ||
            BuildingUpgradeService.CanBuildAnnex(CraftQuarterLayout.Tannery) ||
            BuildingUpgradeService.CanBuildAnnex(CraftQuarterLayout.CharcoalPit) ||
            CraftProduction.CanQueue(CraftProduction.LeatherShop, ResourceCatalog.Find("Leather"), 1) ||
            CraftProduction.CanQueue(CraftProduction.Tannery, ResourceCatalog.Find("Hide"), 1) ||
            CraftProduction.CanQueue(CraftProduction.CharcoalPit, ResourceCatalog.Find("Wood"), 1) ||
            CraftProduction.CanQueue(CraftProduction.Smithy, ResourceCatalog.Find("IronOre"), 1, ResourceCatalog.Find("Coal"), 1) ||
            CraftProduction.CanQueue(CraftProduction.Smithy, ResourceCatalog.Find("IronIngot"), 1);

        private static bool CanImproveEquipment() =>
            BuildingUpgradeService.IsBuilt("blacksmith") && (
                EquipmentUpgrades.CanBuy(EquipmentKind.Cart, ResourceCatalog.Find("Wood")) ||
                EquipmentUpgrades.CanBuy(EquipmentKind.Spear, ResourceCatalog.Find("Wood")) ||
                EquipmentUpgrades.CanBuy(EquipmentKind.Bow, ResourceCatalog.Find("Wood")) ||
                BuildingUpgradeService.IsBuilt(CraftQuarterLayout.LeatherShop) && EquipmentUpgrades.CanBuy(EquipmentKind.LeatherArmor, ResourceCatalog.Find("Leather")) ||
                BuildingUpgradeService.IsBuilt(CraftQuarterLayout.Smithy) && (
                    EquipmentUpgrades.CanBuy(EquipmentKind.Sword, ResourceCatalog.Find("Iron")) ||
                    EquipmentUpgrades.CanBuy(EquipmentKind.IronArmor, ResourceCatalog.Find("IronIngot"))));

        private void Awake()
        {
            if (swordRecipe != null)
            {
                swordRecipe.input = ResourceCatalog.Find("IronIngot");
                swordRecipe.inputAmount = 1;
            }
            Bind(swordRecipe);
            Bind(bowRecipe);
            Bind(spearRecipe);
        }

        private void Start()
        {
            var window = transform.Find("Window") as RectTransform;
            var content = window != null ? window.Find("Recipes Scroll/Viewport/Content") : null;
            if (content != null &&
                content.Find("Cart Recipe") is RectTransform cart &&
                content.Find("Spear Recipe") is RectTransform spear &&
                content.Find("Bow Recipe") is RectTransform bow &&
                content.Find("Sword Recipe") is RectTransform sword)
                gameObject.AddComponent<CraftQuarterLayout>().Build(window, new[] { cart, spear, bow, sword }, this);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;

        private void OnDestroy()
        {
            Unbind(swordRecipe);
            Unbind(bowRecipe);
            Unbind(spearRecipe);
            Unbind(leatherArmorRecipe);
            Unbind(tanneryRecipe);
            Unbind(coalRecipe);
            Unbind(ingotRecipe);
            Unbind(ironArmorRecipe);
            Unbind(crossbowRecipe);
        }

        internal void BindLeatherArmor(RectTransform row) =>
            leatherArmorRecipe = BindRecipe(row, "Leather", "LeatherArmor");

        internal void BindTannery(RectTransform row) =>
            tanneryRecipe = BindRecipe(row, "Hide", "Leather");

        internal void BindCrossbow(RectTransform row) =>
            crossbowRecipe = BindRecipe(row, "IronIngot", "Crossbow");

        internal void BindForge(RectTransform coal, RectTransform ingot, RectTransform armor)
        {
            coalRecipe = BindRecipe(coal, "Wood", "Coal");
            ingotRecipe = BindRecipe(ingot, "IronOre", "IronIngot");
            ingotRecipe.fuel = ResourceCatalog.Find("Coal");
            var button = ingotRecipe.produceButton;
            var icon = Instantiate(ingotRecipe.inputIcon, button.transform);
            ApplyIcon(icon, ingotRecipe.fuel);
            var amount = ingotRecipe.fuelAmountText = Instantiate(ingotRecipe.inputAmountText, button.transform);
            amount.text = "1";
            var layout = button.GetComponent<GameFoundation.UI.PriceButtonLayout>();
            layout.prices = new[] { ingotRecipe.inputIcon.rectTransform, ingotRecipe.inputAmountText.rectTransform,
                icon.rectTransform, amount.rectTransform };
            layout.Invalidate();
            ironArmorRecipe = BindRecipe(armor, "IronIngot", "IronArmor");
            ironArmorRecipe.inputAmount = 2;
        }

        private RecipeView BindRecipe(RectTransform row, string input, string output)
        {
            var button = row.Find("Produce").GetComponent<Button>();
            var recipe = new RecipeView
            {
                input = ResourceCatalog.Find(input),
                output = ResourceCatalog.Find(output),
                produceButton = button,
                inputIcon = button.transform.Find("Input Icon").GetComponent<Image>(),
                inputAmountText = button.transform.Find("Input Amount").GetComponent<Text>(),
                outputIcon = row.Find("Output Icon").GetComponent<Image>(),
                outputAmountText = row.Find("Output Amount").GetComponent<Text>()
            };
            Bind(recipe);
            return recipe;
        }

        private void Bind(RecipeView recipe)
        {
            if (recipe?.produceButton != null)
                recipe.produceButton.onClick.AddListener(() => Produce(recipe));
        }

        private void Unbind(RecipeView recipe)
        {
            if (recipe?.produceButton != null)
                recipe.produceButton.onClick.RemoveAllListeners();
        }

        private void Produce(RecipeView recipe)
        {
            if (recipe?.input == null || recipe.output == null) return;
            int quantity = BatchSize(recipe);
            CraftProduction.Queue(WorkshopOf(recipe), recipe.input, recipe.inputAmount * quantity,
                recipe.output, recipe.outputAmount * quantity, 1, recipe.fuel, recipe.fuel != null ? quantity : 0);
        }

        private int BatchSize(RecipeView recipe)
        {
            if (recipe == tanneryRecipe || recipe == coalRecipe || recipe == ingotRecipe)
            {
                var stock = GlobalResourceManager.Instance;
                int available = (stock?.GetResourceAmount(recipe.input) ?? 0) / Mathf.Max(1, recipe.inputAmount);
                if (recipe.fuel != null) available = Mathf.Min(available, stock?.GetResourceAmount(recipe.fuel) ?? 0);
                return Mathf.Clamp(available, 1, 3);
            }
            return 1;
        }

        private int WorkshopOf(RecipeView recipe) =>
            recipe == leatherArmorRecipe ? CraftProduction.LeatherShop :
            recipe == tanneryRecipe ? CraftProduction.Tannery :
            recipe == coalRecipe ? CraftProduction.CharcoalPit :
            recipe == swordRecipe || recipe == ingotRecipe || recipe == ironArmorRecipe || recipe == crossbowRecipe
                ? CraftProduction.Smithy : CraftProduction.Carpentry;

        internal void RefreshProduction() => Refresh();

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            Refresh(swordRecipe);
            Refresh(bowRecipe);
            Refresh(spearRecipe);
            Refresh(leatherArmorRecipe);
            Refresh(tanneryRecipe);
            Refresh(coalRecipe);
            Refresh(ingotRecipe);
            Refresh(ironArmorRecipe);
            Refresh(crossbowRecipe);
        }

        private void Refresh(RecipeView recipe)
        {
            if (recipe == null) return;
            GlobalResourceManager manager = GlobalResourceManager.Instance;
            int outputStock = manager != null && recipe.output != null ? manager.GetResourceAmount(recipe.output) : 0;
            bool canProduce = CanProduce(recipe);

            if (recipe.produceButton != null) recipe.produceButton.interactable = canProduce;
            BaseDisabledButtonHint.Set(recipe.produceButton, BaseDisabledButtonHint.Reason.Resources,
                reasonText: CraftProduction.StartBlockReason(WorkshopOf(recipe)));
            if (recipe.inputAmountText != null)
            {
                recipe.inputAmountText.text = (recipe.inputAmount * BatchSize(recipe)).ToString();
                recipe.inputAmountText.color = manager != null && recipe.input != null &&
                    manager.GetResourceAmount(recipe.input) >= recipe.inputAmount * BatchSize(recipe) ? enoughColor : notEnoughColor;
            }
            if (recipe.outputAmountText != null)
            {
                recipe.outputAmountText.text = outputStock.ToString();
                recipe.outputAmountText.color = stockColor;
            }
            if (recipe.fuelAmountText != null)
            {
                recipe.fuelAmountText.text = BatchSize(recipe).ToString();
                recipe.fuelAmountText.color = manager != null && manager.GetResourceAmount(recipe.fuel) >= BatchSize(recipe) ? enoughColor : notEnoughColor;
            }
            if ((recipe == coalRecipe || recipe == ingotRecipe) && recipe.produceButton != null)
            {
                var caption = recipe.produceButton.GetComponentInChildren<Text>(true);
                if (caption != null) caption.text = GameFoundation.Localization.GameText.Get(recipe == ingotRecipe ? "Выплавить" : "Сделать") + " ×" + BatchSize(recipe);
            }
            ApplyIcon(recipe.inputIcon, recipe.input);
            ApplyIcon(recipe.outputIcon, recipe.output);
        }

        private bool CanProduce(RecipeView recipe) => recipe?.output != null &&
            CraftProduction.CanQueue(WorkshopOf(recipe), recipe.input, recipe.inputAmount * BatchSize(recipe), recipe.fuel, recipe.fuel != null ? BatchSize(recipe) : 0);

        private static void ApplyIcon(Image image, ResourceType resource)
        {
            if (image == null || resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
            image.preserveAspect = true;
        }
    }
}
