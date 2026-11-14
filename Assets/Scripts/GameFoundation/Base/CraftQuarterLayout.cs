using System.Collections.Generic;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using TMPro;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    internal sealed class CraftQuarterLayout : MonoBehaviour
    {
        internal const string Smithy = "craft_smithy";
        internal const string LeatherShop = "craft_leather_shop";
        internal const string Tannery = "craft_tannery";
        internal const string CharcoalPit = "craft_charcoal_pit";
        private static readonly string[] WorkshopIds = { null, Smithy, Tannery, LeatherShop, CharcoalPit };
        private static readonly Color Gold = new(.9f, .78f, .47f);
        private static readonly Color Muted = new(.69f, .64f, .75f);
        private static readonly Color Ink = new(.94f, .90f, .81f);
        private static readonly string[] Names = { "Столярный цех", "Кузница", "Дубильня", "Кожевенная мастерская", "Яма углежога" };
        private readonly List<(Text Label, string Source)> localized = new();
        private readonly PopupChoiceRowStyle[] cards = new PopupChoiceRowStyle[CraftProduction.WorkshopCount];
        private readonly Text[] workshopTitles = new Text[CraftProduction.WorkshopCount];
        private readonly Text[] statuses = new Text[CraftProduction.WorkshopCount];
        private readonly GameObject[] pages = new GameObject[CraftProduction.WorkshopCount];
        private readonly PopupChoicePrice[,] cardPrices = new PopupChoicePrice[CraftProduction.WorkshopCount, 2];
        private readonly PopupChoicePrice[,] buttonPrices = new PopupChoicePrice[CraftProduction.WorkshopCount, 2];
        private readonly RectTransform[] cardPriceAreas = new RectTransform[CraftProduction.WorkshopCount];
        private readonly Button[] construction = new Button[CraftProduction.WorkshopCount];
        private readonly GameObject[] productionCards = new GameObject[CraftProduction.WorkshopCount];
        private readonly Text[] productionStatuses = new Text[CraftProduction.WorkshopCount];
        private readonly RectTransform[] formulas = new RectTransform[CraftProduction.WorkshopCount];
        private readonly Image[,] formulaIcons = new Image[CraftProduction.WorkshopCount, 4];
        private readonly Text[,] formulaAmounts = new Text[CraftProduction.WorkshopCount, 4];
        private readonly Text[,] formulaSigns = new Text[CraftProduction.WorkshopCount, 3];
        private readonly Button[] cancelButtons = new Button[CraftProduction.WorkshopCount];
        private Text busyAmount;
        private ScrollRect productionScroll;
        private BlacksmithProductionView productionView;
        private DayCycleService calendar;
        private RectTransform swordRecipe, armorRecipe, tanneryRecipe, coalRecipe, ingotRecipe, ironArmorRecipe, crossbowRecipe;
        private Font font;
        private LocalizationService language;

        internal void Build(RectTransform window, RectTransform[] recipes, BlacksmithProductionView production)
        {
            productionView = production;
            var title = window.Find("Title").GetComponent<Text>();
            font = title.font;
            var oldLocalization = title.GetComponent<LocalizedText>();
            if (oldLocalization != null) oldLocalization.enabled = false;
            localized.Add((title, "Ремесленный квартал"));
            title.color = Gold;
            title.fontSize = 36;

            var priceFont = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            var buttonTemplate = recipes[0].Find("Upgrade Equipment").GetComponent<Button>();
            swordRecipe = recipes[3];
            var frame = window.Find("Artwork Frame").GetComponent<Image>();
            const float extraWidth = 520;
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, window.rect.width + extraWidth);
            frame.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frame.rectTransform.rect.width + extraWidth);
            var left = Panel(window, "Workshops", frame, 436, 642, -634, -37);
            var details = Panel(window, "Workshop details", frame, 858, 642, 25, -37);
            var right = Panel(window, "Quarter production", frame, 376, 642, 654, -37);
            Label(right, "Производство", 332, 36, 0, 278, 24, Muted);
            var residents = Rect(right, "Busy resident icon", 48, 48, 83, 278).gameObject.AddComponent<Image>();
            residents.raycastTarget = false;
            ResourceIconSizing.Apply(residents, ResourceCatalog.Find("Human")?.resourceIcon);
            busyAmount = Label(right, "", 36, 44, 130, 278, 22, Ink);
            Label(left, "Мастерские", 392, 36, 0, 278, 24, Muted);
            productionScroll = PortalExpeditionPanel.CreateVerticalScroll(right, "Production list", 332, 548, -24);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                var card = Rect(left, Names[i], 392, 98, 0, 201 - i * 112);
                var image = card.gameObject.AddComponent<Image>();
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                cards[i] = new PopupChoiceRowStyle(button, image);
                button.onClick.AddListener(() => Select(index));
                workshopTitles[i] = Label(card, Names[i], 208, 62, -76, 12, 24, Ink);
                statuses[i] = Label(card, "", 360, 28, 0, -31, 22, Muted);
                if (i > 0)
                {
                    var prices = cardPriceAreas[i] = Rect(card, "Construction price", 352, 48, 0, 0);
                    cardPrices[i, 0] = new PopupChoicePrice(prices, priceFont, 86);
                    cardPrices[i, 1] = new PopupChoicePrice(prices, priceFont, 0);
                }

                var page = Rect(details, Names[i], 810, 600, 0, 0);
                pages[i] = page.gameObject;
                Label(page, Names[i], 810, 40, 0, 278, 24, Muted);
                if (i > 0) AddBuildButton(page, i, buttonTemplate, priceFont);
                AddProductionCard(productionScroll.content, i, buttonTemplate);
            }

            for (int i = 0; i < recipes.Length; i++)
                ArrangeRecipe(recipes[i], pages[i == 3 ? 1 : 0].transform, i == 3 ? 0 : i);

            tanneryRecipe = MakeRecipe(recipes[1], CraftProduction.Tannery, 0, "Leather recipe", "Кожа", "Выделать кожу");
            armorRecipe = MakeRecipe(recipes[1], CraftProduction.LeatherShop, 0, "Leather armor recipe", "Кожаный доспех", "Сделать доспех");
            production.BindTannery(tanneryRecipe);
            production.BindLeatherArmor(armorRecipe);
            coalRecipe = MakeRecipe(recipes[1], CraftProduction.CharcoalPit, 0, "Coal recipe", "Уголь", "Сделать уголь");
            ingotRecipe = MakeRecipe(recipes[1], 1, 0, "Iron ingot recipe", "Железный слиток", "Выплавить");
            ironArmorRecipe = MakeRecipe(recipes[1], 1, 2, "Iron armor recipe", "Железная броня", "Сковать броню");
            ArrangeRecipe(swordRecipe, pages[1].transform, 1);
            production.BindForge(coalRecipe, ingotRecipe, ironArmorRecipe);
            crossbowRecipe = MakeRecipe(recipes[1], CraftProduction.Smithy, 3, "Crossbow recipe", "Арбалет", "Сделать арбалет");
            production.BindCrossbow(crossbowRecipe);
            StretchProductionButton(crossbowRecipe);
            var upgrades = GetComponent<BlacksmithUpgradeView>();
            AddUpgrade(upgrades, recipes[1], EquipmentKind.Spear, "Wood", 1);
            AddUpgrade(upgrades, armorRecipe, EquipmentKind.LeatherArmor, "Leather", 0);
            AddUpgrade(upgrades, ironArmorRecipe, EquipmentKind.IronArmor, "IronIngot", 2);
            StretchProductionButton(tanneryRecipe);
            StretchProductionButton(coalRecipe);
            StretchProductionButton(ingotRecipe);
            upgrades.Refresh();
            for (int i = 0; i < recipes.Length; i++)
                AddDuration(recipes[i], i == 0 ? "2 дня" : "1 день");
            AddDuration(armorRecipe, "1 день");
            AddDuration(tanneryRecipe, "1 день");
            AddDuration(coalRecipe, "1 день");

            foreach (string name in new[] { "Craft Header Divider", "Product Column", "Price Column", "Stock Column", "Recipes Scroll" })
                if (window.Find(name) is Transform old) old.gameObject.SetActive(false);
            CraftProduction.ReconcileWorkers();
            Select(0);
            RefreshLanguage();
            RefreshConstruction();
        }

        private void OnEnable()
        {
            BuildingUpgradeService.Changed += RefreshConstruction;
            CraftProduction.Changed += RefreshProduction;
            calendar = DayCycleService.Instance;
            if (calendar != null) calendar.Changed += RefreshConstruction;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            language = LocalizationService.Instance;
            if (language != null) language.LanguageChanged += RefreshLanguage;
            RefreshLanguage();
            RefreshConstruction();
        }

        private void OnDisable()
        {
            BuildingUpgradeService.Changed -= RefreshConstruction;
            CraftProduction.Changed -= RefreshProduction;
            if (calendar != null) calendar.Changed -= RefreshConstruction;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (language != null) language.LanguageChanged -= RefreshLanguage;
            language = null;
        }

        private void AddBuildButton(RectTransform page, int index, Button template, TMP_FontAsset priceFont)
        {
            var button = construction[index] = Instantiate(template, page);
            button.name = "Build workshop";
            Pose((RectTransform)button.transform, 520, 64, 0, 0);
            var label = button.GetComponentInChildren<Text>(true);
            Pose(label.rectTransform, 282, 48, -102, 0);
            label.fontSize = 22;
            localized.Add((label, "Построить"));
            button.transform.Find("Upgrade Resource").gameObject.SetActive(false);
            button.transform.Find("Upgrade Cost").gameObject.SetActive(false);
            buttonPrices[index, 0] = new PopupChoicePrice((RectTransform)button.transform, priceFont, 110);
            buttonPrices[index, 1] = new PopupChoicePrice((RectTransform)button.transform, priceFont, 22);
            button.onClick.AddListener(() =>
            {
                if (BuildingUpgradeService.TryBuildAnnex(WorkshopIds[index]))
                    GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.BuildingComplete);
            });
        }

        private RectTransform MakeRecipe(RectTransform template, int workshop, int index, string name, string title, string action)
        {
            var row = Instantiate(template, pages[workshop].transform);
            row.name = name;
            ArrangeRecipe(row, pages[workshop].transform, index);
            SetRecipeLabel(row.Find("Title").GetComponent<Text>(), title);
            SetRecipeLabel(row.Find("Produce").GetComponentInChildren<Text>(true), action);
            if (workshop == 1) AddDuration(row, "1 день");
            return row;
        }

        private static void AddUpgrade(BlacksmithUpgradeView view, RectTransform recipe, EquipmentKind kind, string cost, int index)
        {
            var source = System.Array.Find(view.rows, row => row.equipment == EquipmentKind.Sword);
            var button = Instantiate(source.button, recipe);
            button.name = "Upgrade Equipment";
            var level = Instantiate(source.level, recipe);
            level.name = "Equipment Level";
            var pips = Instantiate(source.pips, recipe);
            pips.name = "Equipment Level Pips";
            view.AddRow(new BlacksmithUpgradeView.Row
            {
                equipment = kind, resource = ResourceCatalog.Find(cost), button = button, level = level, pips = pips,
                label = button.GetComponentInChildren<Text>(true),
                icon = button.transform.Find("Upgrade Resource").GetComponent<Image>(),
                amount = button.transform.Find("Upgrade Cost").GetComponent<Text>()
            });
            ArrangeRecipe(recipe, recipe.parent, index);
        }

        private static void StretchProductionButton(RectTransform recipe) =>
            Pose((RectTransform)recipe.Find("Produce"), 622, 64, 80, -25);

        private void AddDuration(RectTransform row, string source) =>
            Label(row, source, 174, 30, -75, 44, 22, Muted);

        private void AddProductionCard(RectTransform parent, int index, Button template)
        {
            var panel = Rect(parent, "Production status " + Names[index], 0, 128, 0, -64);
            panel.anchorMin = new Vector2(0, 1);
            panel.anchorMax = new Vector2(1, 1);
            panel.sizeDelta = new Vector2(0, 128);
            panel.anchoredPosition = new Vector2(0, -64);
            var plate = panel.gameObject.AddComponent<Image>();
            plate.color = index % 2 == 0 ? new Color(.17f, .14f, .195f) : new Color(.135f, .11f, .16f);
            plate.raycastTarget = false;
            productionCards[index] = panel.gameObject;
            formulas[index] = Rect(panel, "Recipe formula", 272, 46, 0, 37);
            productionStatuses[index] = Label(panel, "", 272, 32, 0, -2, 22, Muted);
            for (int i = 0; i < 4; i++)
            {
                var icon = formulaIcons[index, i] = Rect(formulas[index], "Resource", 48, 48, 0, 0).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                formulaAmounts[index, i] = Label(formulas[index], "", 32, 32, 0, 0, 22, Ink);
                if (i < 3) formulaSigns[index, i] = Label(formulas[index], "", 16, 32, 0, 0, 22, Ink);
            }
            var cancel = cancelButtons[index] = Instantiate(template, panel);
            cancel.name = "Cancel production";
            Pose((RectTransform)cancel.transform, 272, 44, 0, -37);
            cancel.transform.Find("Upgrade Resource").gameObject.SetActive(false);
            cancel.transform.Find("Upgrade Cost").gameObject.SetActive(false);
            var label = cancel.GetComponentInChildren<Text>(true);
            if (cancel.TryGetComponent<PriceButtonLayout>(out var priceLayout)) priceLayout.enabled = false;
            Pose(label.rectTransform, 250, 40, 0, 0);
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            localized.Add((label, "Отменить"));
            if (cancel.TryGetComponent<ActionButtonLabelColor>(out var tint)) tint.Negative = true;
            cancel.onClick.AddListener(() => CraftProduction.Cancel(index));
        }

        private void RefreshProduction()
        {
            if (ironArmorRecipe == null) return;
            productionView.RefreshProduction();
            GetComponent<WarehouseCartPurchaseView>()?.RefreshProduction();
            int busy = 0;
            int visible = 0;
            for (int i = 0; i < productionCards.Length; i++)
            {
                if (productionCards[i] == null) continue;
                var workshop = CraftProduction.Read(i);
                var order = workshop.orders.Count > 0 ? workshop.orders[0] : null;
                productionCards[i].SetActive(order != null);
                if (order == null) continue;
                ((RectTransform)productionCards[i].transform).anchoredPosition = new Vector2(0, -64 - 140 * visible++);
                productionStatuses[i].text = GameText.Format("Осталось времени: {0} дн.", order.days);
                SetFormula(i, workshop.worker, order);
                var cancel = cancelButtons[i];
                cancel.interactable = true;
                BaseDisabledButtonHint.Set(cancel, BaseDisabledButtonHint.Reason.Resources,
                    reasonText: null);
                if (workshop.worker) busy++;
            }
            busyAmount.text = busy.ToString();
            bool overflow = visible > 4;
            float height = overflow ? visible * 140 - 12 : 548;
            productionScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            productionScroll.viewport.offsetMax = new Vector2(overflow ? -16 : 0, 0);
            productionScroll.vertical = overflow;
            if (!overflow) productionScroll.content.anchoredPosition = Vector2.zero;
        }

        private void SetFormula(int index, bool worker, CraftProduction.Order order)
        {
            var terms = new List<(ResourceType Resource, int Amount)>(4);
            if (worker) terms.Add((ResourceCatalog.Find("Human"), 1));
            if (order.price > 0) terms.Add((ResourceCatalog.Find(order.input), order.price));
            if (order.fuelCost > 0) terms.Add((ResourceCatalog.Find(order.fuel), order.fuelCost));
            terms.RemoveAll(term => term.Resource == null);
            terms.Add((ResourceCatalog.Find(order.resource), order.amount));
            var elements = new List<RectTransform>(11);
            for (int i = 0; i < 4; i++)
            {
                bool show = i < terms.Count;
                var icon = formulaIcons[index, i];
                var amount = formulaAmounts[index, i];
                icon.gameObject.SetActive(show);
                amount.gameObject.SetActive(show && terms[i].Amount != 1);
                if (show)
                {
                    ResourceIconSizing.Apply(icon, terms[i].Resource?.resourceIcon);
                    elements.Add(icon.rectTransform);
                    if (amount.gameObject.activeSelf)
                    {
                        amount.text = terms[i].Amount.ToString();
                        amount.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(14, amount.preferredWidth));
                        elements.Add(amount.rectTransform);
                    }
                }
                if (i >= 3) continue;
                var sign = formulaSigns[index, i];
                sign.gameObject.SetActive(i + 1 < terms.Count);
                if (sign.gameObject.activeSelf)
                {
                    sign.text = i + 2 == terms.Count ? "=" : "+";
                    elements.Add(sign.rectTransform);
                }
            }
            const float gap = 3;
            float width = gap * (elements.Count - 1);
            foreach (var element in elements) width += element.rect.width;
            float x = -width * .5f;
            foreach (var element in elements)
            {
                element.anchoredPosition = new Vector2(x + element.rect.width * .5f, 0);
                x += element.rect.width + gap;
            }
        }

        private void OnResourceChanged(ResourceType _, int __) => RefreshConstruction();

        private void RefreshConstruction()
        {
            if (swordRecipe == null || construction[3] == null) return;
            swordRecipe.gameObject.SetActive(BuildingUpgradeService.IsBuilt(Smithy));
            armorRecipe.gameObject.SetActive(BuildingUpgradeService.IsBuilt(LeatherShop));
            tanneryRecipe.gameObject.SetActive(BuildingUpgradeService.IsBuilt(Tannery));
            bool smithyBuilt = BuildingUpgradeService.IsBuilt(Smithy);
            coalRecipe.gameObject.SetActive(BuildingUpgradeService.IsBuilt(CharcoalPit));
            ingotRecipe.gameObject.SetActive(smithyBuilt);
            ironArmorRecipe.gameObject.SetActive(smithyBuilt);
            crossbowRecipe.gameObject.SetActive(smithyBuilt);
            (statuses[0].text, statuses[0].color) = PopupChoiceRowStyle.PurchaseStatus(true, false, false, construction: true);
            PopupChoiceRowStyle.PlaceCaptions(workshopTitles[0], statuses[0], 20, 20, 20);
            for (int i = 1; i < WorkshopIds.Length; i++)
            {
                if (construction[i] == null) continue;
                string id = WorkshopIds[i];
                bool built = BuildingUpgradeService.IsBuilt(id);
                var cost = built ? null : BuildingUpgradeService.Catalog?.Find(id)?.constructionCost;
                construction[i].gameObject.SetActive(!built && cost != null);
                construction[i].interactable = BuildingUpgradeService.CanBuildAnnex(id);
                (statuses[i].text, statuses[i].color) = PopupChoiceRowStyle.PurchaseStatus(
                    built, cost == null, construction[i].interactable, construction: true);
                float titleRight = cost == null ? 20 : cost.costB > 0 ? 176 : 90;
                PopupChoiceRowStyle.PlaceCaptions(workshopTitles[i], statuses[i], 20, titleRight, 20);
                cardPriceAreas[i].anchoredPosition = new Vector2(cost?.costB > 0 ? 0 : 86, 13);
                cardPrices[i, 0].Set(cost?.resourceA, cost?.costA ?? 0);
                cardPrices[i, 1].Set(cost?.resourceB, cost?.costB ?? 0);
                buttonPrices[i, 0].Set(cost?.resourceA, cost?.costA ?? 0);
                buttonPrices[i, 1].Set(cost?.resourceB, cost?.costB ?? 0);
            }
            RefreshProduction();
        }

        private void Select(int selected)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                cards[i].SetSelected(i == selected);
                pages[i].SetActive(i == selected);
            }
        }

        private static void ArrangeRecipe(RectTransform row, Transform parent, int index)
        {
            row.SetParent(parent, false);
            Pose(row, 810, 126, 0, 187 - index * 136);
            if (row.TryGetComponent<Image>(out var plate))
            {
                plate.sprite = null;
                plate.type = Image.Type.Simple;
                plate.material = null;
                plate.color = index % 2 == 0 ? new Color(.17f, .14f, .195f) : new Color(.135f, .11f, .16f);
            }

            Place(row, "Title", 276, 44, -247, 41);
            Place(row, "Cart Title", 276, 44, -247, 41);
            Place(row, "Output Icon", 0, 0, -352, -25);
            Place(row, "Output Amount", 84, 46, -281, -25);
            Place(row, "Cart Icon", 0, 0, -352, -25);
            Place(row, "Cart Amount", 84, 46, -281, -25);
            if (row.Find("Accent") is Transform accent) accent.gameObject.SetActive(false);
            if (row.Find("Stock Caption") is Transform caption) caption.gameObject.SetActive(false);
            var make = (row.Find("Produce") ?? row.Find("Make Cart")) as RectTransform;
            if (make != null) Pose(make, 324, 64, -69, -25);
            var upgrade = row.Find("Upgrade Equipment") as RectTransform;
            if (upgrade == null) return;
            Pose(upgrade, 290, 64, 246, -25);
            var label = upgrade.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                Pose(label.rectTransform, 178, 48, -30, 0);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.fontSize = 22;
            }
            Place(upgrade, "Upgrade Resource", 0, 0, 70, 0);
            Place(upgrade, "Upgrade Cost", 28, 48, 105, 0);
            Place(row, "Equipment Level", 252, 30, 246, 47);
            Place(row, "Equipment Level Pips", 156, 16, 246, 25);
        }

        private void SetRecipeLabel(Text label, string source)
        {
            var previous = label.GetComponent<LocalizedText>();
            if (previous != null) previous.enabled = false;
            localized.Add((label, source));
        }

        private void RefreshLanguage()
        {
            foreach (var entry in localized) entry.Label.text = GameText.Get(entry.Source);
            RefreshConstruction();
        }

        private Text Label(Transform parent, string source, float width, float height, float x, float y, int size, Color color)
        {
            var rect = Rect(parent, source, width, height, x, y);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            if (!string.IsNullOrEmpty(source)) localized.Add((label, source));
            return label;
        }

        private static RectTransform Panel(Transform parent, string name, Image frame, float width, float height, float x, float y)
        {
            var rect = Rect(parent, name, width, height, x, y);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = frame.sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = frame.pixelsPerUnitMultiplier;
            image.color = frame.color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(Transform parent, string name, float width, float height, float x, float y)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Pose(rect, width, height, x, y);
            return rect;
        }

        private static void Place(Transform parent, string name, float width, float height, float x, float y)
        {
            if (parent.Find(name) is RectTransform rect)
                Pose(rect, width > 0 ? width : rect.rect.width, height > 0 ? height : rect.rect.height, x, y);
        }

        private static void Pose(RectTransform rect, float width, float height, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = Vector3.one;
        }
    }
}
