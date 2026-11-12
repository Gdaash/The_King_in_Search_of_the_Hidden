using System.Collections.Generic;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed partial class MilitaryBuildingsController
    {
        private const int GarrisonTab = 3;
        private const int CellsPerPage = BuildingUpgradeService.GarrisonMaxCapacity;
        private readonly (Button Button, Image Icon, Text Number, PopupChoiceRowStyle Style)[] garrisonCells =
            new (Button, Image, Text, PopupChoiceRowStyle)[CellsPerPage];
        private readonly List<(ResourceType Unit, MilitaryProfile Profile)> garrisonWarriors = new();
        private RectTransform garrisonPage;
        private Text slotPurchaseLabel, pageNumber, disarmLabel, deploymentNote;
        private Button slotPurchase, garrisonDisarm, previousPage, nextPage;
        private UnitDescriptionView garrisonDetails;
        private UnitDescriptionCatalog unitDescriptions;
        private string selectedProfile;
        private int garrisonPageIndex;

        private void CreateGarrison(RectTransform details, Text template)
        {
            unitDescriptions = Resources.Load<UnitDescriptionCatalog>("UI/Unit Details/Unit Description Catalog");
            const float left = -538f;
            const float width = 420f;
            const float center = left + width * .5f;
            const float cellSize = 96f;
            const float cellStep = 108f;
            const int columns = 4;
            const float gridTop = FirstCardTop;
            const float buttonHeight = 66f;

            garrisonPage = Rect(details, "Garrison page", details.sizeDelta, Vector2.zero);
            for (int i = 0; i < garrisonCells.Length; i++)
            {
                int cell = i;
                var rect = Rect(garrisonPage, "Garrison slot " + (i + 1), new Vector2(cellSize, cellSize),
                    new Vector2(left + cellSize * .5f + i % columns * cellStep, gridTop - cellSize * .5f - i / columns * cellStep));
                var background = rect.gameObject.AddComponent<Image>();
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                var style = new PopupChoiceRowStyle(button, background);
                var icon = Rect(rect, "Warrior portrait", Vector2.one, Vector2.down * 4).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                var number = Label(rect, template, "Slot number", new Vector2(36, 24), new Vector2(-13, 21), 22, TextAnchor.MiddleLeft);
                garrisonCells[i] = (button, icon, number, style);
                button.onClick.AddListener(() =>
                {
                    int index = garrisonPageIndex * CellsPerPage + cell;
                    if (index >= garrisonWarriors.Count) return;
                    selectedProfile = garrisonWarriors[index].Profile.id;
                    RefreshGarrison();
                });
            }
            previousPage = GarrisonButton("Previous warriors", new Vector2(42, 38), new Vector2(left + width - 118, -175), template, out var previousLabel);
            nextPage = GarrisonButton("Next warriors", new Vector2(42, 38), new Vector2(left + width - 21, -175), template, out var nextLabel);
            previousLabel.text = "<";
            nextLabel.text = ">";
            pageNumber = Label(garrisonPage, template, "Roster page", new Vector2(52, 36), new Vector2(left + width - 69, -175), 22, TextAnchor.MiddleCenter);
            previousPage.onClick.AddListener(() => { garrisonPageIndex--; RefreshGarrison(); });
            nextPage.onClick.AddListener(() => { garrisonPageIndex++; RefreshGarrison(); });

            slotPurchase = GarrisonButton("Buy garrison slot", new Vector2(70, 70), Vector2.zero, template, out slotPurchaseLabel);
            slotPurchaseLabel.text = "+";
            slotPurchaseLabel.fontStyle = FontStyle.Bold;
            slotPurchaseLabel.fontSize = 40;
            slotPurchaseLabel.raycastTarget = false;
            slotPurchase.onClick.AddListener(() =>
            {
                if (BuildingUpgradeService.TryUpgrade(BuildingUpgradeService.Garrison))
                    TooltipManager.Instance?.Hide();
            });
            var hover = slotPurchase.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowGarrisonSlotPrice());
            hover.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => TooltipManager.Instance?.Hide());
            hover.triggers.Add(exit);
            slotPurchase.gameObject.SetActive(false);

            deploymentNote = Label(garrisonPage, template, "Deployment note", new Vector2(width, 48),
                new Vector2(center, -202), 22, TextAnchor.MiddleCenter);
            deploymentNote.color = SubtitleColor;
            garrisonDisarm = GarrisonButton("Disarm selected warrior", new Vector2(width, buttonHeight), new Vector2(center, UnitCardBottom + buttonHeight * .5f + 3f), template, out disarmLabel);
            garrisonDisarm.onClick.AddListener(DisarmGarrisonWarrior);

            var profileCard = (RectTransform)barracksPopup.transform.Find("Window/Unit Description Card");
            garrisonDetails = Instantiate(profileCard, garrisonPage).GetComponent<UnitDescriptionView>();
            garrisonDetails.UseFixedHeight();
            garrisonPage.gameObject.SetActive(false);
        }

        private Button GarrisonButton(string name, Vector2 size, Vector2 position, Text template, out Text caption)
        {
            var rect = Rect(garrisonPage, name, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme").textButtonSprite;
            image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            rect.gameObject.AddComponent<UnifiedButtonFeedback>();
            caption = Label(rect, template, "Label", size, Vector2.zero, 24, TextAnchor.MiddleCenter);
            return button;
        }

        private void RefreshGarrison()
        {
            var stock = GlobalResourceManager.Instance;
            int capacity = BuildingUpgradeService.Capacity(BuildingUpgradeService.Garrison);
            names[GarrisonTab].text = GameText.Get("Гарнизон");
            (statuses[GarrisonTab].text, statuses[GarrisonTab].color) = PopupChoiceRowStyle.PurchaseStatus(true, false, false, construction: true);
            PopupChoiceRowStyle.PlaceCaptions(names[GarrisonTab], statuses[GarrisonTab], 20, 20, 20);
            bool shown = selected == GarrisonTab;
            cards[GarrisonTab].SetSelected(shown);
            garrisonPage.gameObject.SetActive(shown);
            if (!shown) return;

            deploymentNote.text = UnitDescriptionText.Get("unit.training.deploy_hint", "В начале забега весь отряд автоматически выходит из портала.");
            garrisonWarriors.Clear();
            if (stock != null)
                foreach (var unit in ResourceCatalog.All.Where(BuildingUpgradeService.IsGarrisonUnit))
                    foreach (var profile in MilitaryExperienceService.GetStored(unit, stock.GetResourceAmount(unit)))
                        garrisonWarriors.Add((unit, profile));
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(garrisonWarriors.Count / (float)CellsPerPage));
            garrisonPageIndex = Mathf.Clamp(garrisonPageIndex, 0, pageCount - 1);
            previousPage.gameObject.SetActive(pageCount > 1);
            nextPage.gameObject.SetActive(pageCount > 1);
            pageNumber.gameObject.SetActive(pageCount > 1);
            pageNumber.text = (garrisonPageIndex + 1) + " / " + pageCount;
            previousPage.interactable = garrisonPageIndex > 0;
            nextPage.interactable = garrisonPageIndex < pageCount - 1;

            var lockSprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme").lockIconSprite;
            var next = BuildingUpgradeService.Next(BuildingUpgradeService.Garrison);
            bool showPurchase = false;
            for (int i = 0; i < garrisonCells.Length; i++)
            {
                int index = garrisonPageIndex * CellsPerPage + i;
                bool occupied = index < garrisonWarriors.Count;
                bool locked = !occupied && index >= capacity;
                var (button, icon, number, style) = garrisonCells[i];
                button.interactable = occupied;
                ResourceIconSizing.Apply(icon, occupied ? garrisonWarriors[index].Unit.resourceIcon : locked ? lockSprite : null);
                bool buy = index == capacity && next != null;
                icon.enabled = (occupied || locked) && !buy;
                number.text = locked || buy ? string.Empty : (index + 1).ToString();
                if (buy)
                {
                    var purchaseRect = (RectTransform)slotPurchase.transform;
                    purchaseRect.SetParent(button.transform, false);
                    purchaseRect.anchorMin = purchaseRect.anchorMax = new Vector2(.5f, .5f);
                    purchaseRect.anchoredPosition = Vector2.zero;
                    bool affordable = HasGarrisonResource(next.resourceA, next.costA) && HasGarrisonResource(next.resourceB, next.costB);
                    slotPurchase.interactable = BuildingUpgradeService.CanUpgrade(BuildingUpgradeService.Garrison);
                    slotPurchaseLabel.color = affordable ? new Color32(140, 217, 122, 255) : new Color32(252, 125, 128, 255);
                    showPurchase = true;
                }
                style.SetSelected(occupied && garrisonWarriors[index].Profile.id == selectedProfile);
            }

            slotPurchase.gameObject.SetActive(showPurchase);

            var selection = garrisonWarriors.Find(item => item.Profile.id == selectedProfile);
            var definition = unitDescriptions != null ? unitDescriptions.Find(selection.Unit) : null;
            garrisonDetails.gameObject.SetActive(definition != null);
            if (definition != null) garrisonDetails.Show(definition, selection.Profile);
            garrisonDisarm.gameObject.SetActive(selection.Profile != null);
            garrisonDisarm.interactable = CraftProduction.Morning;
            disarmLabel.text = GameText.Get("Разоружить");
            disarmLabel.color = garrisonDisarm.interactable ? new Color(.55f, .85f, .54f) : new Color(.64f, .59f, .68f);
        }

        private static bool HasGarrisonResource(ResourceType resource, int cost) =>
            resource != null && GlobalResourceManager.Instance?.GetResourceAmount(resource) >= cost;

        private void ShowGarrisonSlotPrice()
        {
            var next = BuildingUpgradeService.Next(BuildingUpgradeService.Garrison);
            if (next == null) return;
            TooltipManager.Instance?.Show(PortalArsenalPopup.T("slots.buy", "Купить ячейку"),
                (RectTransform)slotPurchase.transform, priceIcon: next.resourceA?.resourceIcon, price: next.costA,
                priceAffordable: HasGarrisonResource(next.resourceA, next.costA),
                secondPriceIcon: next.resourceB?.resourceIcon, secondPrice: next.costB,
                secondPriceAffordable: HasGarrisonResource(next.resourceB, next.costB));
        }

        private void DisarmGarrisonWarrior()
        {
            var choice = garrisonWarriors.Find(item => item.Profile.id == selectedProfile);
            if (choice.Profile == null) return;
            selectedProfile = null;
            string weapon = choice.Unit.Id switch
            {
                "Spearman" => "Spear",
                "Archer" => "Bow",
                "Swordsman" => "Sword",
                "Crossbowman" => "Crossbow",
                _ => null
            };
            MilitaryTrainingView.DisarmStored(choice.Unit, ResourceCatalog.Find(weapon), choice.Profile);
        }
    }
}
