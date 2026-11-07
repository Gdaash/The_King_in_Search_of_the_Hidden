using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    internal sealed class PortalExpeditionPanel
    {
        private const float ColumnWidth = 480f;
        private static readonly string[] PartyIds = { "Human", "Cart" };
        private readonly List<(ResourceType type, TMP_Text name, TMP_Text count)> party = new();
        private readonly List<ResourceType> troopTypes = new();
        private readonly List<(RectTransform row, Image icon, TMP_Text name, TMP_Text count)> troopRows = new();
        private readonly TMP_Text partyTitle, troopTitle;
        private readonly ScrollRect troopScroll;
        private const float TroopViewportHeight = 300f;
        private const float PartyRowHeight = 50f;
        private readonly Button configureLoadout;
        private readonly TMP_Text configureLabel;
        private const string MagicTowerId = "magic_library";

        public PortalExpeditionPanel(PortalPopupView view)
        {
            var detail = (RectTransform)view.title.transform.parent;
            var window = (RectTransform)detail.parent;
            float detailScale = ColumnWidth / detail.rect.width;
            const float leftWidth = ColumnWidth, detailWidth = ColumnWidth, rightWidth = ColumnWidth, gap = 24;
            float contentWidth = leftWidth + detailWidth + rightWidth + gap * 2;
            float width = contentWidth + 80;
            float left = -contentWidth * .5f + leftWidth * .5f;
            float middle = left + leftWidth * .5f + gap + detailWidth * .5f;
            float right = middle + detailWidth * .5f + gap + rightWidth * .5f;
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (window.Find("Travel Rule") is RectTransform travelRule) travelRule.gameObject.SetActive(false);
            var list = window.Find("Locations") as RectTransform;
            var caption = window.Find("Locations Caption")?.GetComponent<Text>();
            var leftPanel = Rect("Location choice background", window, ColumnWidth, detail.rect.height, left, detail.anchoredPosition.y);
            PanelBackground(leftPanel);
            leftPanel.SetSiblingIndex(0);
            if (list != null)
            {
                Place(list, 432, 458, left, detail.anchoredPosition.y - 17);
                for(int i=0;i<view.locations.Length;i++)
                    Place((RectTransform)view.locations[i].transform,432,82,0,188-i*94);
            }
            if (caption != null)
                Place(caption.rectTransform, ColumnWidth - 48, 36, left, detail.anchoredPosition.y + 248);
            if (window.Find("Header Divider") is RectTransform divider)
                divider.gameObject.SetActive(false);
            var close = (RectTransform)view.closeButton.transform;
            Vector2 closeSize = close.rect.size;
            close.SetParent(window, false);
            close.anchorMin = close.anchorMax = close.pivot = Vector2.one;
            close.sizeDelta = closeSize;
            close.anchoredPosition = new Vector2(-14, -14);
            close.SetAsLastSibling();

            detail.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, detailWidth);
            PanelBackground(detail);
            MoveX(detail, middle);
            foreach (RectTransform child in detail)
            {
                Compress(child, detailScale);
                if (child.name.StartsWith("Resource ") && child.GetComponent<Image>() != null)
                    foreach (RectTransform inner in child) Compress(inner, detailScale);
            }
            if (detail.Find("Top Rule") is RectTransform topRule) topRule.gameObject.SetActive(false);
            var theme=Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme");
            int titleSize = theme != null ? theme.popupSubtitleSize : 24;
            Color titleColor = theme != null ? theme.popupSubtitleColor : new Color(.66f,.61f,.72f);
            if(caption!=null){caption.fontSize=titleSize;caption.color=titleColor;caption.alignment=TextAnchor.MiddleLeft;}
            view.title.fontSize = titleSize;
            view.title.color = titleColor;
            view.title.alignment=TextAnchor.MiddleLeft;
            Place(view.title.rectTransform, ColumnWidth - 48, 36, 0, 248);
            Place(view.description.rectTransform,ColumnWidth-48,104,0,166);
            if (detail.Find("Danger Label")?.GetComponent<Text>() is Text dangerLabel)
            {
                dangerLabel.fontSize = view.resourceRows[0].abundance.fontSize;
                Place(dangerLabel.rectTransform, 140, 44, -148, 81);
            }
            ConfigureDanger(view, detail);
            if (detail.Find("Resources Caption") is RectTransform resourceHeading)
                Place(resourceHeading, ColumnWidth - 48, 32, 0, 31);
            for (int i = 0; i < view.resourceRows.Length; i++)
            {
                Place((RectTransform)view.resourceRows[i].root.transform, ColumnWidth - 48, 50, 0, -25 - i * 50);
                if (view.resourceRows[i].root.TryGetComponent<Image>(out var rowImage))
                    rowImage.color = i % 2 == 0 ? new Color(.17f, .14f, .195f) : new Color(.135f, .11f, .16f);
            }
            var panel = Rect("Expedition", window, rightWidth, detail.rect.height, right, detail.anchoredPosition.y);
            PanelBackground(panel);
            partyTitle = Label(panel, "Party title", ColumnWidth - 48, 36, 0, 248, titleSize);
            partyTitle.alignment = TextAlignmentOptions.MidlineLeft;
            partyTitle.color = titleColor;
            for (int i = 0; i < PartyIds.Length; i++)
            {
                var type = ResourceCatalog.Find(PartyIds[i]);
                if (type == null) continue;
                var row = CreatePartyRow(panel, i, 197 - i * PartyRowHeight);
                ResourceIconSizing.Apply(row.icon, type.resourceIcon);
                party.Add((type, row.name, row.count));
            }

            troopTitle = Label(panel, "Troops title", ColumnWidth - 48, 36, 0, 67, titleSize);
            troopTitle.alignment = partyTitle.alignment;
            troopTitle.color = titleColor;
            troopScroll = CreateTroopScroll(panel);

            var fit = window.GetComponent<GameFoundation.UI.CanvasWindowFit>();
            fit?.ReserveFooter(0);
            const float footerHeight = 206;
            float columnBottom = detail.anchoredPosition.y - detail.rect.height * .5f;
            float heightChange = footerHeight - (columnBottom - window.rect.yMin);
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, window.rect.height + heightChange);
            foreach (RectTransform child in window)
                if (child != close && child.gameObject.activeSelf)
                    child.anchoredPosition += new Vector2(0, heightChange * .5f);

            float footerY = window.rect.yMin + footerHeight * .5f;
            var balance = Resources.Load<PortalTowerBalance>("World/PortalTowerBalance");
            if (balance != null && balance.useEnergyLoadout && balance.weaponSlotsPrefab != null)
            {
                var slots = Object.Instantiate(balance.weaponSlotsPrefab, window).GetComponent<PortalWeaponSlots>();
                slots.Bind(balance, PortalWeaponSlots.Context.Preview);
                var slotsRect = (RectTransform)slots.transform;
                Place(slotsRect, slotsRect.rect.width, slotsRect.rect.height, -290, footerY + 18);
                var configure = Button(window, "Configure loadout", 280, 48, -290, footerY - 52,
                    () =>
                    {
                        if (!BuildingUpgradeService.IsBuilt(MagicTowerId)) return;
                        var baseUI = Object.FindFirstObjectByType<BaseUIController>();
                        if (baseUI == null) return;
                        view.Close();
                        baseUI.OpenMagicLibrary();
                    }, out configureLabel);
                configureLoadout = configure.GetComponent<Button>();
                BaseDisabledButtonHint.Set(configureLoadout, BaseDisabledButtonHint.Reason.MagicTower);
            }

            float travelX = configureLoadout != null ? 290 : 0;
            var status = (RectTransform)view.status.transform;
            status.SetParent(window, false);
            Place(status, 360, 88, travelX, footerY + 53);
            view.status.alignment = TextAnchor.LowerCenter;
            var travel = (RectTransform)view.travelButton.transform;
            travel.SetParent(window, false);
            Place(travel, 350, 60, travelX, footerY - 31);
            window.GetComponent<GameFoundation.UI.CanvasWindowFit>()?.Fit();
        }

        public void Refresh()
        {
            partyTitle.text = PortalPopupView.Tr("expedition.roster_heading", "Состав отряда").ToUpperInvariant();
            if (configureLoadout != null)
            {
                configureLoadout.interactable = BuildingUpgradeService.IsBuilt(MagicTowerId);
                configureLabel.text = PortalPopupView.Tr("expedition.configure_loadout", "Настроить");
            }
            foreach (var entry in party)
            {
                int value = GlobalResourceManager.Instance != null ? GlobalResourceManager.Instance.GetResourceAmount(entry.type) : 0;
                string name = entry.type.Id == "Human" ? PortalPopupView.Tr("expedition.peasants", "Крестьяне") : entry.type.DisplayName;
                entry.name.text = name;
                entry.count.text = value.ToString();
            }
            RefreshTroops();
        }

        private void RefreshTroops()
        {
            troopTypes.Clear();
            var resources = GlobalResourceManager.Instance;
            if (resources != null)
                foreach (var type in ResourceCatalog.All)
                    if (type != null && type.isMilitaryUnit && resources.GetResourceAmount(type) > 0)
                        troopTypes.Add(type);
            troopTypes.Sort((a, b) =>
            {
                int order = a.militaryDisplayOrder.CompareTo(b.militaryDisplayOrder);
                return order != 0 ? order : string.CompareOrdinal(a.Id, b.Id);
            });

            int count = troopTypes.Count;
            troopTitle.text = PortalPopupView.Tr("base.military_overview.title", "Войска").ToUpperInvariant();
            troopTitle.gameObject.SetActive(count > 0);
            troopScroll.gameObject.SetActive(count > 0);
            bool overflow = count * PartyRowHeight > TroopViewportHeight;
            troopScroll.viewport.offsetMax = new Vector2(overflow ? -16 : 0, 0);
            troopScroll.content.sizeDelta = new Vector2(0, count * PartyRowHeight);
            for (int i = 0; i < count; i++)
            {
                if (i == troopRows.Count)
                    troopRows.Add(CreatePartyRow(troopScroll.content, i, 0));
                var entry = troopRows[i];
                var type = troopTypes[i];
                entry.row.gameObject.SetActive(true);
                entry.row.anchorMin = new Vector2(0, 1);
                entry.row.anchorMax = new Vector2(1, 1);
                entry.row.sizeDelta = new Vector2(0, PartyRowHeight);
                entry.row.anchoredPosition = new Vector2(0, -(i + .5f) * PartyRowHeight);
                entry.name.text = type.DisplayName;
                entry.count.text = resources.GetResourceAmount(type).ToString();
                ResourceIconSizing.Apply(entry.icon, type.resourceIcon);
            }
            for (int i = count; i < troopRows.Count; i++)
                troopRows[i].row.gameObject.SetActive(false);
            if (!overflow)
                troopScroll.content.anchoredPosition = Vector2.zero;
        }

        private static (RectTransform row, Image icon, TMP_Text name, TMP_Text count) CreatePartyRow(
            RectTransform parent, int index, float y)
        {
            var row = Rect("Party resource", parent, 432, PartyRowHeight, 0, y);
            var background = row.gameObject.AddComponent<Image>();
            background.color = index % 2 == 0 ? new Color(.17f, .14f, .195f) : new Color(.135f, .11f, .16f);
            background.raycastTarget = false;
            var icon = Rect("Party icon", row, 0, 0, -179, 0).gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            var name = Label(row, "Party name", 252, 44, -23, 0, 24);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var count = Label(row, "Party count", 82, 44, 156, 0, 24);
            count.alignment = TextAlignmentOptions.MidlineRight;
            return (row, icon, name, count);
        }

        private static ScrollRect CreateTroopScroll(RectTransform panel)
        {
            var root = Rect("Troop list", panel, ColumnWidth - 48, TroopViewportHeight, 0, -106);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42;
            var viewport = Rect("Viewport", root, 0, 0, 0, 0);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.gameObject.AddComponent<Image>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport, 0, 0, 0, 0);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            scroll.content = content;
            scroll.viewport = viewport;

            var bar = Rect("Scrollbar", root, 8, TroopViewportHeight, (ColumnWidth - 48 - 8) * .5f, 0);
            bar.gameObject.AddComponent<Image>().color = new Color(.14f, .11f, .17f);
            var track = Rect("Sliding area", bar, 0, 0, 0, 0);
            track.anchorMin = Vector2.zero;
            track.anchorMax = Vector2.one;
            var handle = Rect("Handle", track, 0, 0, 0, 0);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(.50f, .43f, .56f);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static void PanelBackground(RectTransform rect)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme")?.popupPanelSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void ConfigureDanger(PortalPopupView view, RectTransform detail)
        {
            var active = view.dangerSkulls[0].sprite;
            foreach (var skull in view.dangerSkulls) if (skull != null) skull.gameObject.SetActive(false);
            var inactive = Resources.Load<Sprite>("Expedition/Skull Inactive");
            view.dangerSkulls = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                var rect = Rect("Danger skull " + (i + 1), detail, 0, 0, -50 + i * 28, 81);
                var image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                ResourceIconSizing.Apply(image, active);
                view.dangerSkulls[i] = image;
            }
            view.SetExpeditionSkulls(active, inactive);
        }

        private static void Compress(RectTransform rect, float scale)
        {
            MoveX(rect, rect.anchoredPosition.x * scale);
            if (rect.GetComponent<Image>() == null || rect.rect.width > 150)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.rect.width * scale);
        }

        private static RectTransform Button(RectTransform parent, string name, float w, float h, float x, float y,
            UnityEngine.Events.UnityAction click, out TMP_Text label)
        {
            var rect = Rect(name, parent, w, h, x, y);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme")?.textButtonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(click);
            rect.gameObject.AddComponent<GameFoundation.UI.UnifiedButtonFeedback>();
            label = Label(rect, "Label", w - 24, h - 6, 0, 0, 22);
            rect.gameObject.AddComponent<GameFoundation.UI.ActionButtonLabelColor>().Configure(null, label);
            return rect;
        }

        private static TMP_Text Label(RectTransform parent, string name, float w, float h, float x, float y, float size)
        {
            var label = Rect(name, parent, w, h, x, y).gameObject.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            if (font != null) label.font = font;
            label.fontSize = Mathf.Max(22, size);
            label.enableAutoSizing = true;
            label.fontSizeMin = 22;
            label.fontSizeMax = Mathf.Max(22, size);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.94f, .88f, .73f);
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, float w, float h, float x, float y)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Place(rect, w, h, x, y);
            return rect;
        }

        private static void Place(RectTransform rect, float w, float h, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void MoveX(RectTransform rect, float x) => rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }
}
