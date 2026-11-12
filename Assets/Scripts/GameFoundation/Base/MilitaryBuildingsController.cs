using GameFoundation.Localization;
using GameFoundation.Saves;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed partial class MilitaryBuildingsController : MonoBehaviour
    {
        [SerializeField] private Button fortButton, barracksButton;
        [SerializeField] private Button archeryRangeButton;
        [SerializeField] private GameObject fortPopup, barracksPopup;
        [SerializeField] private GameObject archeryRangePopup;

        private const float FirstCardTop = 379f;
        private const float UnitCardHeight = 676f;
        private const float UnitCardBottom = FirstCardTop - UnitCardHeight;
        private const float TrainingTabHeight = 48f;
        private const float TrainingColumnWidth = 380f;
        private const float PopupHeightReduction = 120f;
        private static readonly Color SubtitleColor = new(.69f, .64f, .75f);
        private const float TrainingTabCenter = FirstCardTop - TrainingTabHeight * .5f - 2f;
        internal const string DistrictName = "Военный квартал";
        private readonly BaseBuildingConstruction[] buildings = new BaseBuildingConstruction[3];
        private readonly PopupChoiceRowStyle[] cards = new PopupChoiceRowStyle[4];
        private readonly PopupChoicePrice[,] prices = new PopupChoicePrice[3, 2];
        private readonly Text[] names = new Text[4];
        private readonly Text[] statuses = new Text[4];
        private GameObject[] pages;
        private RectTransform district;
        private RectTransform roster;
        private readonly RectTransform[] dimmerSections = new RectTransform[4];
        private Text title, sidebarHeading, heading, lockedLabel, buildLabel;
        private Button build;
        private PopupChoicePrice[] buildPrices;
        private LocalizationService language;
        private int selected;
        private RangeTrainingTabs rangeTraining;
        private Button spearmenTab, slingersTab, knightsTab;
        private bool slingersSelected;

        private void Awake()
        {
            if (barracksButton == null || barracksPopup == null || archeryRangePopup == null || fortPopup == null)
            {
                if (fortButton != null) fortButton.onClick.AddListener(() => fortPopup.SetActive(true));
                if (archeryRangeButton != null) archeryRangeButton.onClick.AddListener(() => archeryRangePopup.SetActive(true));
                return;
            }

            pages = new[] { barracksPopup, archeryRangePopup, fortPopup };
            var buttons = new[] { barracksButton, archeryRangeButton, fortButton };
            for (int i = 0; i < pages.Length; i++)
            {
                buildings[i] = buttons[i].GetComponent<BaseBuildingConstruction>();
                pages[i].SetActive(false);
            }
            rangeTraining = archeryRangePopup.GetComponent<RangeTrainingTabs>();
            rangeTraining.UseDistrictRange();
            buildings[0].DistrictUnlock = buildings[1];
            barracksButton.onClick.AddListener(Open);
            if (barracksButton.TryGetComponent<BuildingActionAvailabilityIndicator>(out var indicator))
                indicator.SetMilitaryDistrict(HasAvailableAction);
        }

        private void Start()
        {
            if (pages == null) return;
            // Older settlements could build a range or fort before the barracks existed.
            if (!buildings[0].IsBuilt && (buildings[1].IsBuilt || buildings[2].IsBuilt))
            {
                SaveSlotPrefs.SetInt("foundation.building.barracks.built", 1);
                SaveSlotPrefs.Save();
            }
            foreach (var building in buildings) building.Refresh();
        }

        private void OnEnable()
        {
            if (pages == null) return;
            BuildingUpgradeService.Changed += Refresh;
            GameFoundation.Quests.ContentUnlocks.Changed += Refresh;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            language = LocalizationService.Instance;
            if (language != null) language.LanguageChanged += Refresh;
        }

        private void OnDisable()
        {
            BuildingUpgradeService.Changed -= Refresh;
            GameFoundation.Quests.ContentUnlocks.Changed -= Refresh;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (language != null) language.LanguageChanged -= Refresh;
        }

        private bool HasAvailableAction()
        {
            for (int i = 0; i < pages.Length; i++)
            {
                var facility = buildings[i];
                if (!facility.IsBuilt && facility.CanAffordConstruction) return true;
                if (!facility.IsBuilt) continue;
                var training = pages[i].GetComponent<MilitaryTrainingView>();
                if (i == 0 && rangeTraining.CanTrainSlinger(training)) return true;
                if (training.TryGetComponent<RangeTrainingTabs>(out var range) ? range.CanTrain : training.CanArmWarrior)
                    return true;
            }
            return BuildingUpgradeService.CanUpgrade(BuildingUpgradeService.Garrison);
        }

        private void Open()
        {
            if (!buildings[0].IsBuilt) return;
            if (district == null) CreateDistrict();
            district.SetAsLastSibling();
            district.gameObject.SetActive(true);
            UpdateDimmer();
            Refresh();
        }

        private void LateUpdate()
        {
            if (district != null && district.gameObject.activeInHierarchy) UpdateDimmer();
        }

        private void UpdateDimmer()
        {
            Rect bounds = district.rect;
            float left = bounds.xMax, right = bounds.xMax, bottom = bounds.yMin, top = bounds.yMax;
            if (roster != null && roster.gameObject.activeInHierarchy && roster.rect.width > 0)
            {
                Vector3 min = district.InverseTransformPoint(roster.TransformPoint(roster.rect.min));
                Vector3 max = district.InverseTransformPoint(roster.TransformPoint(roster.rect.max));
                left = Mathf.Clamp(min.x, bounds.xMin, bounds.xMax);
                right = Mathf.Clamp(max.x, left, bounds.xMax);
                bottom = Mathf.Clamp(min.y, bounds.yMin, bounds.yMax);
                top = Mathf.Clamp(max.y, bottom, bounds.yMax);
            }
            PlaceDimmer(0, bounds.xMin, bounds.yMin, left, bounds.yMax);
            PlaceDimmer(1, right, bounds.yMin, bounds.xMax, bounds.yMax);
            PlaceDimmer(2, left, bounds.yMin, right, bottom);
            PlaceDimmer(3, left, top, right, bounds.yMax);
        }

        private void PlaceDimmer(int index, float left, float bottom, float right, float top)
        {
            var section = dimmerSections[index];
            section.sizeDelta = new Vector2(right - left, top - bottom);
            section.anchoredPosition = new Vector2((left + right) * .5f, (bottom + top) * .5f);
        }

        private void Close()
        {
            PopupMotion.Hide(district.gameObject);
        }

        private void CreateDistrict()
        {
            var template = (RectTransform)barracksPopup.transform.Find("Window");
            var frame = template.Find("Artwork Frame").GetComponent<Image>();
            var titleTemplate = template.Find("Title").GetComponent<Text>();
            var closeTemplate = template.Find("Close").GetComponent<Button>();
            district = Rect(barracksPopup.transform.parent, DistrictName, Vector2.zero, Vector2.zero);
            district.gameObject.SetActive(false);
            district.anchorMin = Vector2.zero;
            district.anchorMax = Vector2.one;
            roster = transform.root.GetComponentInChildren<VerticalMilitaryPanel>()?.transform as RectTransform;
            var dimmer = Rect(district, "Dimmer", Vector2.zero, Vector2.zero);
            dimmer.anchorMin = Vector2.zero;
            dimmer.anchorMax = Vector2.one;
            district.gameObject.AddComponent<PopupDimmerLink>().Bind(dimmer.gameObject);
            for (int i = 0; i < dimmerSections.Length; i++)
            {
                var section = Rect(dimmer, "Dimmer section", Vector2.zero, Vector2.zero);
                section.anchorMin = section.anchorMax = district.pivot;
                var shade = section.gameObject.AddComponent<Image>();
                shade.color = new Color(0, 0, 0, .62f);
                dimmerSections[i] = section;
            }
            var window = Rect(district, "Window", new Vector2(1570, 1038 - PopupHeightReduction), Vector2.zero);
            window.gameObject.AddComponent<CanvasWindowFit>();
            var outerFrame = Frame(window, frame, Vector2.zero, window.sizeDelta);
            title = Label(window, titleTemplate, "Title", new Vector2(900, 52), new Vector2(0, 474 - PopupHeightReduction * .5f), 36, TextAnchor.MiddleCenter);
            var close = Instantiate(closeTemplate, window);
            Place((RectTransform)close.transform, new Vector2(40, 40), new Vector2(751, 485));
            if (close.TryGetComponent<PopupClosePlacement>(out var closePlacement)) closePlacement.frame = outerFrame;
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(Close);

            var sidebar = Frame(window, frame, new Vector2(-572, -29), new Vector2(378, 900 - PopupHeightReduction));
            var details = Frame(window, frame, new Vector2(195, -29), new Vector2(1124, 900 - PopupHeightReduction));
            sidebarHeading = Label(sidebar, titleTemplate, "Buildings", new Vector2(332, 42), new Vector2(0, 407), 24, TextAnchor.MiddleLeft);
            var priceFont = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            const float rowHeight = 94f;
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                var row = Rect(sidebar, "Building choice", new Vector2(332, rowHeight), new Vector2(0, FirstCardTop - rowHeight * .5f - 110 * i));
                var image = row.gameObject.AddComponent<Image>();
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                cards[i] = new PopupChoiceRowStyle(button, image);
                names[i] = Label(row, titleTemplate, "Building name", new Vector2(220, 36), new Vector2(-40, 19), 24, TextAnchor.MiddleLeft);
                statuses[i] = Label(row, titleTemplate, "Construction status", new Vector2(290, 30), new Vector2(-5, -22), 22, TextAnchor.MiddleLeft);
                if (i < pages.Length)
                {
                    prices[i, 0] = new PopupChoicePrice(row, priceFont, 12, 13);
                    prices[i, 1] = new PopupChoicePrice(row, priceFont, 90, 13);
                    EmbedPage(pages[i], details, frame);
                }
                button.onClick.AddListener(() => { selected = index; Refresh(); });
            }

            AddBarracksTabs();
            AddFortTabs();

            heading = Label(details, titleTemplate, "Section title", new Vector2(1076, 42), new Vector2(0, 407), 24, TextAnchor.MiddleLeft);
            lockedLabel = Label(details, titleTemplate, "Construction status", new Vector2(1000, 64), new Vector2(0, 64), 24, TextAnchor.MiddleCenter);
            var arm = barracksPopup.GetComponent<MilitaryTrainingView>().ArmButton;
            build = Instantiate(arm, details);
            build.name = "Build military facility";
            Place((RectTransform)build.transform, new Vector2(490, 68), Vector2.zero);
            build.onClick = new Button.ButtonClickedEvent();
            foreach (Transform child in build.transform) child.gameObject.SetActive(false);
            if (build.TryGetComponent<PriceButtonLayout>(out var priceLayout)) priceLayout.enabled = false;
            if (build.TryGetComponent<ActionButtonLabelColor>(out var labelStyle)) labelStyle.enabled = false;
            buildLabel = Label(build.transform, titleTemplate, "Build label", new Vector2(265, 54), new Vector2(-90, 0), 24, TextAnchor.MiddleLeft);
            buildPrices = new[] { new PopupChoicePrice((RectTransform)build.transform, priceFont, 24), new PopupChoicePrice((RectTransform)build.transform, priceFont, 104) };
            var feedback = build.GetComponent<UnifiedButtonFeedback>();
            if (feedback != null && build.targetGraphic != null && build.targetGraphic.gameObject != build.gameObject)
                build.targetGraphic.gameObject.SetActive(true);
            build.onClick.AddListener(() => buildings[selected].Build(district.gameObject));
            CreateGarrison(details, titleTemplate);
            sidebarHeading.color = heading.color = SubtitleColor;
            foreach (RectTransform column in new[] { sidebar, details })
                foreach (RectTransform child in column)
                    child.anchoredPosition += Vector2.down * (PopupHeightReduction * .5f);
        }

        private void EmbedPage(GameObject page, RectTransform details, Image frame)
        {
            page.transform.SetParent(details, false);
            var training = page.GetComponent<MilitaryTrainingView>();
            training.DistrictPage = true;
            if (page.TryGetComponent<Image>(out var pageBackground)) pageBackground.enabled = false;
            if (page.TryGetComponent<PopupDimmerLink>(out var dimmer)) dimmer.enabled = false;
            var window = (RectTransform)page.transform.Find("Window");
            Place((RectTransform)page.transform, details.sizeDelta, Vector2.zero);
            if (window.TryGetComponent<CanvasWindowFit>(out var fit)) fit.enabled = false;
            window.localScale = Vector3.one;
            Place(window, details.sizeDelta, Vector2.zero);
            foreach (string name in new[] { "Artwork Frame", "Title", "Close", "Available Caption", "Arm Cost", "Cost Caption", "Recruitment Recipe", "Food Per Warrior", "Squad Food", "Upgrade Details", "Deployment Hint" })
                window.Find(name)?.gameObject.SetActive(false);

            Move(window, "Archers Tab", -422, TrainingTabCenter, 184, TrainingTabHeight);
            Move(window, "Slingers Tab", -226, TrainingTabCenter, 184, TrainingTabHeight);
            Move(window, "Stock", -324, 282, TrainingColumnWidth, 58);
            training.ConfigureDistrictStock((RectTransform)window.Find("Stock"));
            Move(window, "Arm", -324, 204, TrainingColumnWidth, 66);
            Move(window, "Disarm", -324, 126, TrainingColumnWidth, 66);
            Move(window, "Disarm Hint", -324, 53, TrainingColumnWidth, 64);
            var card = (RectTransform)window.Find("Unit Description Card");
            Place(card, new Vector2(640, UnitCardHeight), new Vector2(218, FirstCardTop - UnitCardHeight * .5f));
            var cardFrame = card.GetComponent<Image>();
            cardFrame.sprite = frame.sprite;
            cardFrame.type = frame.type;
            cardFrame.pixelsPerUnitMultiplier = frame.pixelsPerUnitMultiplier;
            cardFrame.color = frame.color;
            cardFrame.fillCenter = frame.fillCenter;
            foreach (RectTransform child in card)
                child.anchoredPosition += Vector2.down * 10f;
            card.GetComponent<UnitDescriptionView>().UseFixedHeight();
        }

        private void AddBarracksTabs()
        {
            var view = barracksPopup.GetComponent<MilitaryTrainingView>();
            var definition = view.GetComponentInChildren<UnitDescriptionView>(true).Definition;
            var spear = ResourceCatalog.Find("Spear");
            var spearman = ResourceCatalog.Find("Spearman");
            var window = view.transform.Find("Window");
            var template = archeryRangePopup.transform.Find("Window/Archers Tab").GetComponent<Button>();
            slingersTab = AddTrainingTab(template, window, "base.range.slingers", -422);
            spearmenTab = AddTrainingTab(template, window, "base.barracks.spearmen", -226);
            spearmenTab.onClick.AddListener(() =>
            {
                slingersSelected = false;
                view.SelectUnit(spear, spearman, definition);
                RefreshBarracksIndicators(view);
            });
            slingersTab.onClick.AddListener(() =>
            {
                slingersSelected = true;
                rangeTraining.SelectSlinger(view);
                RefreshBarracksIndicators(view);
            });
            slingersTab.onClick.Invoke();
        }

        private void RefreshBarracksIndicators(MilitaryTrainingView view)
        {
            RangeTrainingTabs.Tint(spearmenTab, !slingersSelected,
                view.CanArm(ResourceCatalog.Find("Spear"), ResourceCatalog.Find("Spearman")));
            RangeTrainingTabs.Tint(slingersTab, slingersSelected, rangeTraining.CanTrainSlinger(view));
        }

        private void AddFortTabs()
        {
            var window = fortPopup.transform.Find("Window");
            var template = archeryRangePopup.transform.Find("Window/Archers Tab").GetComponent<Button>();
            knightsTab = AddTrainingTab(template, window, "base.fort.knights", -422);
            var speed = AddTrainingTab(template, window, "base.fort.speed", -226);
            speed.interactable = false;
            RangeTrainingTabs.Tint(speed, false, false);
        }

        private static Button AddTrainingTab(Button template, Transform window, string key, float x)
        {
            var tab = Instantiate(template, window);
            tab.onClick = new Button.ButtonClickedEvent();
            tab.interactable = true;
            Place((RectTransform)tab.transform, new Vector2(184, TrainingTabHeight), new Vector2(x, TrainingTabCenter));
            tab.GetComponentInChildren<LocalizedText>(true)?.SetKey(key);
            if (tab.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = false;
            return tab;
        }

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            if (district == null || !district.gameObject.activeSelf) return;
            title.text = GameText.Get(DistrictName);
            sidebarHeading.text = GameText.Get("Здания");
            buildLabel.text = GameText.Get("Построить");
            for (int i = 0; i < pages.Length; i++)
            {
                var facility = buildings[i];
                bool needsBuild = !facility.IsBuilt;
                names[i].text = i == 0 ? GameText.Get("Казарма") : facility.DisplayName;
                (statuses[i].text, statuses[i].color) = PopupChoiceRowStyle.PurchaseStatus(
                    !needsBuild, !facility.ConstructionUnlocked || !CraftProduction.Morning, facility.CanAffordConstruction, construction: true);
                cards[i].SetSelected(i == selected);
                SetPrice(prices[i, 0], prices[i, 1], facility, needsBuild);
                float titleRight = !needsBuild ? 20 : facility.WoodCost > 0 && facility.StoneCost > 0 ? 166 : 90;
                PopupChoiceRowStyle.PlaceCaptions(names[i], statuses[i], 20, titleRight, 20);
                pages[i].SetActive(i == selected && !needsBuild);
            }

            if (selected == 0) RefreshBarracksIndicators(barracksPopup.GetComponent<MilitaryTrainingView>());
            if (selected == 1 && archeryRangePopup.activeInHierarchy) rangeTraining.RefreshIndicators();
            if (selected == 2) RangeTrainingTabs.Tint(knightsTab, true, fortPopup.GetComponent<MilitaryTrainingView>().CanArmWarrior);
            RefreshGarrison();
            heading.text = names[selected].text;
            if (selected == GarrisonTab)
            {
                build.gameObject.SetActive(false);
                lockedLabel.gameObject.SetActive(false);
                return;
            }
            var active = buildings[selected];
            build.gameObject.SetActive(!active.IsBuilt && active.ConstructionUnlocked);
            build.interactable = active.CanAffordConstruction;
            buildLabel.color = build.interactable ? new Color(.55f, .85f, .54f) : new Color(.64f, .59f, .68f);
            BaseDisabledButtonHint.Set(build, BaseDisabledButtonHint.Reason.Resources,
                reasonText: !CraftProduction.Morning ? GameText.Get("Доступно утром") : null);
            SetPrice(buildPrices[0], buildPrices[1], active, !active.IsBuilt);
            lockedLabel.gameObject.SetActive(!active.IsBuilt && !active.ConstructionUnlocked);
            lockedLabel.text = GameText.Get("Заблокировано");
        }

        private static void SetPrice(PopupChoicePrice right, PopupChoicePrice left, BaseBuildingConstruction facility, bool visible)
        {
            bool hasFirst = facility.WoodCost > 0;
            right.Set(hasFirst ? facility.Wood : facility.Stone, visible ? (hasFirst ? facility.WoodCost : facility.StoneCost) : 0);
            left.Set(facility.Stone, visible && hasFirst ? facility.StoneCost : 0);
        }

        private static RectTransform Frame(Transform parent, Image template, Vector2 position, Vector2 size)
        {
            var frame = Instantiate(template, parent);
            Place(frame.rectTransform, size, position);
            frame.raycastTarget = false;
            return frame.rectTransform;
        }

        private static Text Label(Transform parent, Text template, string name, Vector2 size, Vector2 position, int fontSize, TextAnchor alignment)
        {
            var rect = Rect(parent, name, size, position);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = template.font;
            label.fontSize = fontSize;
            label.color = template.color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, size, position);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Move(Transform root, string name, float x, float y, float width, float height)
        {
            if (root.Find(name) is RectTransform rect)
                Place(rect, new Vector2(width, height), new Vector2(x, y));
        }
    }
}
