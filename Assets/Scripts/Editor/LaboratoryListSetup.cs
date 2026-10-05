#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Authors the shared laboratory and row prefabs. Does not run automatically.</summary>
public static class LaboratoryListSetup
{
    public const string PopupPath = "Assets/Prefabs/Base/Laboratory Popup.prefab";
    public const string RowPath = "Assets/Prefabs/UI/Laboratory Upgrade Row.prefab";
    private static readonly Color Ink = new(.94f, .91f, .82f), Gold = new(.92f, .83f, .60f), Muted = new(.66f, .61f, .72f);
    private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
    private static Sprite Panel => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Popups/Sprites/Shared/PopupPanel9Slice.png");
    private static Sprite Lock => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Gameplay/Sprites/ConstructionPointLock.png");
    private static GlobalStats Stats => Resources.Load<GlobalStats>("Global/globalHexStats");

    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var table = Stats.UpgradeTable;
        ConfigureCatalog(table);
        ValidateEntries(table.entries);
        AddLocalization(table);
        // Preserve later Inspector styling when refreshing the catalog.
        if (AssetDatabase.LoadAssetAtPath<LaboratoryUpgradeRow>(RowPath) == null) CreateRow();
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
        if (existing == null || existing.GetComponent<LaboratoryUpgradeList>() == null) CreatePopup(table);
        RefreshPopupCatalog(table);
        foreach (string path in new[] { "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab",
                     "Assets/Prefabs/Base/Variants/Base Location Variant.prefab", "Assets/Prefabs/Base/Variants/Base Islands Backup.prefab" })
            ReplaceOldPopup(path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Laboratory: {table.entries.Select(e => e.GroupId).Distinct().Count()} grouped rows, {table.entries.Count} levels; catalog reachable without a softlock.");
    }

    public static void ConfigureCatalog(ScientificUpgradeTable table)
    {
        table.entries.RemoveAll(e => e.id == ScientificUpgrades.WarriorRetreat);
        var singles = new[] { ScientificUpgrades.PortalArrows, ScientificUpgrades.SharpAxes, ScientificUpgrades.QuietScouting,
            ScientificUpgrades.StrongWalls, ScientificUpgrades.FastHex, ScientificUpgrades.WarriorBaseRegen };
        string[] titles = { "Магические стрелы", "Острые топоры", "Тихая разведка", "Крепкие стены", "Открытие гексов", "Полевой лазарет" };
        string[] icons = { "Magic", "Sword", "Eye", "Shield", "Hourglass", "Healing" };
        int[] gates = { 0, 0, 0, 2, 3, 6 };
        for (int i = 0; i < singles.Length; i++) Assign(table.Find(singles[i]), singles[i], titles[i], 1, gates[i], Icon(icons[i]));
        for (int i = 0; i < 5; i++)
        {
            Assign(table.Find(ScientificUpgrades.Flashlights[i]), "crystal_cells", "Ячейки кристалла", i + 1, 1 + 3 * i, Icon("Magic"));
            Assign(table.Find(ScientificUpgrades.CrystalPower[i]), "crystal_power", "Мощность кристалла", i + 1, 2 + 3 * i,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Gameplay/Sprites/NewUi/iconEnergy.png"));
        }
        EditorUtility.SetDirty(table);
    }

    public static void RefreshPopupCatalog(ScientificUpgradeTable table)
    {
        var root = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            var view = root.GetComponent<LaboratoryUpgradeList>();
            foreach (var row in root.GetComponentsInChildren<LaboratoryUpgradeRow>(true))
                if (!table.entries.Any(e => e.GroupId == row.groupId)) Object.DestroyImmediate(row.gameObject);
            view.Refresh();
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Unit Stats/" + name + ".png");
    private static void Assign(ScientificUpgradeTable.Entry e, string group, string title, int level, int gate, Sprite icon)
    {
        if (e == null || !string.IsNullOrEmpty(e.groupId)) return;
        e.groupId = group; e.groupTitle = title; e.level = level; e.requiredPurchases = gate; e.icon = icon;
        e.parentId = ""; // Historic save IDs stay intact; old cross-branch dependencies are removed.
    }
    public static void ValidateEntries(List<ScientificUpgradeTable.Entry> entries)
    {
        if (entries.Any(e => e == null || string.IsNullOrWhiteSpace(e.id) || e.level < 1 || e.requiredPurchases < 0 || e.cost < 0 || (e.cost > 0 && e.costResource == null)) ||
            entries.Select(e => e.id).Distinct().Count() != entries.Count)
            throw new InvalidOperationException("Invalid scientific upgrade IDs, levels, thresholds or prices.");
        foreach (var group in entries.GroupBy(e => e.GroupId))
            if (!group.OrderBy(e => e.level).Select(e => e.level).SequenceEqual(Enumerable.Range(1, group.Count())))
                throw new InvalidOperationException("Levels must be consecutive, starting at 1: " + group.Key);
        var bought = new HashSet<string>();
        while (true)
        {
            var next = entries.FirstOrDefault(e => !bought.Contains(e.id) && e.requiredPurchases <= bought.Count &&
                entries.All(p => p.GroupId != e.GroupId || p.level >= e.level || bought.Contains(p.id)));
            if (next == null) break;
            bought.Add(next.id);
        }
        if (bought.Count != entries.Count) throw new InvalidOperationException("Unlock thresholds cause a softlock: " + string.Join(", ", entries.Where(e => !bought.Contains(e.id)).Select(e => e.id)));
    }

    private static RectTransform Rect(string name, Transform parent, float w, float h, float x = 0, float y = 0)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y); return r;
    }
    private static void Stretch(RectTransform r, float l = 0, float b = 0, float right = 0, float top = 0)
    { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new(l, b); r.offsetMax = new(-right, -top); }
    private static Image Image(RectTransform r, Color c, Sprite sprite = null)
    { var img = r.gameObject.AddComponent<Image>(); img.color = c; img.sprite = sprite; img.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; img.raycastTarget = false; return img; }
    private static Text Text(string name, Transform p, string value, float w, float h, float x, float y, int size = 24, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var t = Rect(name, p, w, h, x, y).gameObject.AddComponent<Text>(); t.font = Font; t.fontSize = size; t.text = value;
        t.color = Ink; t.alignment = align; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }
    private static void Localize(Text text, string key)
    { var s = new SerializedObject(text.gameObject.AddComponent<LocalizedText>()); s.FindProperty("key").stringValue = key; s.ApplyModifiedPropertiesWithoutUndo(); }
    private static void TopLeft(RectTransform r, float x, float y)
    { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new(x, -y); }

    private static void CreateRow()
    {
        var root = Rect("Laboratory Upgrade Row", null, 692, 108);
        var row = root.gameObject.AddComponent<LaboratoryUpgradeRow>();
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 108;
        row.lockIcon = Image(Rect("Lock", root, 30, 30), Gold, Lock);
        row.lockIcon.type = UnityEngine.UI.Image.Type.Simple; ResourceIconSizing.Apply(row.lockIcon, Lock); TopLeft(row.lockIcon.rectTransform, 15, 17);
        row.requirementLabel = Text("Required Purchases", root, "0", 60, 30, 0, 0, 22, TextAnchor.MiddleCenter);
        TopLeft(row.requirementLabel.rectTransform, 0, 55);
        var card = Rect("Buy Upgrade", root, 616, 100); Stretch(card, 68, 4, 4, 4);
        row.border = Image(card, row.affordableColor); row.border.raycastTarget = true;
        row.purchaseButton = card.gameObject.AddComponent<Button>(); row.purchaseButton.targetGraphic = row.border; row.purchaseButton.transition = Selectable.Transition.None;
        var body = Rect("Surface", card, 0, 0); Stretch(body, 3, 3, 3, 3); row.surface = Image(body, new Color(.16f, .13f, .19f));
        var selected = Rect("Selection Accent", card, 0, 0); Stretch(selected, 0, 7, 608, 7);
        selected.anchorMax = new Vector2(0, 1); selected.offsetMin = new Vector2(0, 7); selected.offsetMax = new Vector2(5, -7);
        row.selection = Image(selected, Gold);
        row.titleLabel = Text("Name and Level", card, "Магические стрелы", 574, 35, 0, 0, 24);
        TopLeft(row.titleLabel.rectTransform, 20, 12);
        row.statusLabel = Text("State and Progress", card, "Можно изучить", 420, 34, 0, 0, 18); TopLeft(row.statusLabel.rectTransform, 20, 54);
        var price = Rect("Price", card, 116, 50, 0, 0); price.anchorMin = price.anchorMax = new Vector2(1, 0); price.anchoredPosition = new(-68, 34);
        row.price = price.gameObject;
        row.costIcon = Image(Rect("Resource Icon", price, 48, 48, -28, 0), Color.white);
        row.costLabel = Text("Amount", price, "1", 46, 38, 25, 0, 24, TextAnchor.MiddleCenter);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPath); Object.DestroyImmediate(root.gameObject);
    }

    private static void CreatePopup(ScientificUpgradeTable table)
    {
        var root = Rect("Laboratory Popup", null, 0, 0); Stretch(root);
        var blocker = Image(root, Color.clear); blocker.raycastTarget = true;
        root.gameObject.AddComponent<PopupDimmerLink>();
        var view = root.gameObject.AddComponent<LaboratoryUpgradeList>(); view.stats = Stats;
        view.rowPrefab = AssetDatabase.LoadAssetAtPath<LaboratoryUpgradeRow>(RowPath);
        var window = Rect("Window", root, 1360, 848); window.gameObject.AddComponent<CanvasWindowFit>();
        Image(window, Color.white, Panel).raycastTarget = true;
        var title = Text("Title", window, "НАУЧНАЯ ЛАБОРАТОРИЯ", 1190, 50, 0, 356, 32, TextAnchor.MiddleCenter); title.color = Gold;
        Localize(title, "base.laboratory.title");
        var close = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Popup Close Icon.prefab"), window);
        close.name = "Close"; var cr = (RectTransform)close.transform; cr.anchorMin = cr.anchorMax = cr.pivot = new(.5f, .5f); cr.anchoredPosition = new(630, 378); cr.localScale = Vector3.one;
        view.totalLabel = Text("Total Learned", window, "Изучено улучшений: 0 / " + table.entries.Count, 690, 32, -292, 299, 22); view.totalLabel.color = Gold;
        var hint = Text("Unlock Hint", window, "Замок — нужно изучить улучшений", 690, 26, -292, 264, 18); hint.color = Muted; Localize(hint, "laboratory.lock_legend");
        Image(Rect("Header Divider", window, 1260, 2, 0, 236), new Color(.35f, .30f, .39f));
        var scroll = Rect("Upgrade List", window, 708, 598, -292, -78);
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>(); view.scroll = scrollRect;
        scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.movementType = ScrollRect.MovementType.Clamped; scrollRect.scrollSensitivity = 42; scrollRect.inertia = true;
        var viewport = Rect("Viewport", scroll, 0, 0); Stretch(viewport, 0, 0, 18, 0);
        Image(viewport, Color.white).raycastTarget = true; viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("Content", viewport, 0, 0); content.anchorMin = new(0, 1); content.anchorMax = new(1, 1); content.pivot = new(.5f, 1); content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.content = content; scrollRect.content = content; scrollRect.viewport = viewport;
        var bar = Rect("Scrollbar", scroll, 8, 598, 350, 0); Image(bar, new Color(.14f, .11f, .17f));
        var handle = Rect("Handle", bar, 8, 100); Stretch(handle); var handleImage = Image(handle, new Color(.50f, .43f, .56f)); handleImage.raycastTarget = true;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
        scrollRect.verticalScrollbar = scrollbar; scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        var footer = Text("Scroll Hint", window, "Колесо мыши — прокрутка", 690, 24, -292, -397, 16); footer.color = Muted; Localize(footer, "laboratory.scroll_hint");
        var detail = Rect("Upgrade Details", window, 520, 612, 368, -80); Image(detail, new Color(.12f, .095f, .145f));
        Image(Rect("Detail Top Rule", detail, 520, 3, 0, 306), new Color(.48f, .40f, .53f));
        view.detailIcon = Image(Rect("Upgrade Icon", detail, 32, 32, -212, 264), Gold);
        var eyebrow = Text("Detail Caption", detail, "ИССЛЕДОВАНИЕ", 380, 30, 22, 264, 18); eyebrow.color = Muted; Localize(eyebrow, "laboratory.research");
        view.detailTitle = Text("Upgrade Title", detail, "Магические стрелы", 464, 76, 0, 194, 30);
        view.detailTitle.color = Gold;
        view.detailLevel = Text("Level Progress", detail, "Изучено уровней: 0 / 1", 464, 32, 0, 136, 20); view.detailLevel.color = Muted;
        Image(Rect("Detail Divider", detail, 464, 2, 0, 104), new Color(.30f, .25f, .35f));
        view.detailDescription = Text("Effect Description", detail, "Кристалл на башне портала теперь может стрелять.", 464, 184, 0, -6, 23, TextAnchor.UpperLeft);
        view.detailDescription.lineSpacing = 1.12f;
        view.detailRequirement = Text("Unlock Progress", detail, "Нужно изучить улучшений: 0\nУже изучено: 0", 464, 65, 0, -142, 20); view.detailRequirement.color = Muted;
        var cost = Rect("Detail Price", detail, 464, 55, 0, -205); view.detailPrice = cost.gameObject;
        var costTitle = Text("Price Caption", cost, "Стоимость", 220, 40, -110, 0, 22); Localize(costTitle, "laboratory.price");
        view.detailCostIcon = Image(Rect("Resource Icon", cost, 48, 48, 137, 0), Color.white);
        view.detailCost = Text("Amount", cost, "1", 64, 40, 201, 0, 26, TextAnchor.MiddleCenter);
        view.detailStatus = Text("Purchase Hint", detail, "Нажмите на улучшение слева, чтобы изучить", 464, 62, 0, -269, 18);
        foreach (var g in table.entries.GroupBy(e => e.GroupId).OrderBy(g => g.First().requiredPurchases))
        {
            var row = (GameObject)PrefabUtility.InstantiatePrefab(view.rowPrefab.gameObject, content); row.name = g.Key;
            row.GetComponent<LaboratoryUpgradeRow>().groupId = g.Key;
        }
        view.Refresh(); Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PopupPath); Object.DestroyImmediate(root.gameObject);
    }

    private static void ReplaceOldPopup(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var old = root.GetComponentsInChildren<LaboratoryPanZoom>(true).FirstOrDefault();
            if (old != null)
            {
                var oldRoot = old.gameObject;
                var oldLink = oldRoot.GetComponent<PopupDimmerLink>();
                var dimmer = oldLink != null ? new SerializedObject(oldLink).FindProperty("dimmer").objectReferenceValue : null;
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath), old.transform.parent);
                replacement.transform.SetSiblingIndex(old.transform.GetSiblingIndex()); replacement.SetActive(oldRoot.activeSelf);
                foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && !c.transform.IsChildOf(old.transform)))
                {
                    var so = new SerializedObject(c); var prop = so.GetIterator(); bool changed = false;
                    while (prop.Next(true)) if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == oldRoot) { prop.objectReferenceValue = replacement; changed = true; }
                        else if (prop.objectReferenceValue == old.transform) { prop.objectReferenceValue = replacement.transform; changed = true; }
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
                var link = new SerializedObject(replacement.GetComponent<PopupDimmerLink>()); link.FindProperty("dimmer").objectReferenceValue = dimmer; link.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(oldRoot);
            }
            foreach (var indicator in root.GetComponentsInChildren<BuildingActionAvailabilityIndicator>(true))
            {
                var so = new SerializedObject(indicator);
                if (so.FindProperty("actionType").enumValueIndex != (int)BuildingActionAvailabilityIndicator.ActionType.LaboratoryUpgrade) continue;
                so.FindProperty("laboratoryStats").objectReferenceValue = Stats;
                so.FindProperty("laboratorySkills").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddLocalization(ScientificUpgradeTable table)
    {
        var loc = Resources.Load<LocalizationTable>("Localization/Base Localization");
        void Add(string key, string ru, string en)
        {
            var e = loc.entries.FirstOrDefault(x => x.key == key);
            if (e == null) { e = new LocalizationTable.Entry { key = key }; loc.entries.Add(e); }
            while (e.values.Count < loc.languages.Count) e.values.Add("");
            for (int i = 0; i < loc.languages.Count; i++) e.values[i] = loc.languages[i] == "ru" ? ru : en;
        }
        Add("base.laboratory.title", "НАУЧНАЯ ЛАБОРАТОРИЯ", "SCIENTIFIC LABORATORY");
        Add("laboratory.total", "Изучено улучшений", "Upgrades learned");
        Add("laboratory.lock_legend", "У замка — сколько улучшений нужно изучить", "Lock — upgrades required to unlock");
        Add("laboratory.scroll_hint", "Колесо мыши — прокрутка", "Mouse wheel — scroll");
        Add("laboratory.research", "ИССЛЕДОВАНИЕ", "RESEARCH");
        Add("laboratory.level", "ур.", "lv.");
        Add("laboratory.complete", "Изучено", "Learned");
        Add("laboratory.locked", "Заблокировано", "Locked");
        Add("laboratory.available", "Можно изучить", "Ready to learn");
        Add("laboratory.no_resources", "Не хватает ресурсов", "Not enough resources");
        Add("laboratory.learned_levels", "Изучено уровней", "Levels learned");
        Add("laboratory.requirement", "Нужно изучить улучшений", "Upgrades required");
        Add("laboratory.already", "Уже изучено", "Already learned");
        Add("laboratory.price", "Стоимость", "Cost");
        Add("laboratory.maximum", "Все уровни этого улучшения изучены", "All levels of this upgrade learned");
        Add("laboratory.unlock_hint", "Изучайте другие улучшения, чтобы открыть этот уровень", "Learn other upgrades to unlock this level");
        Add("laboratory.buy_hint", "Нажмите на улучшение слева, чтобы изучить", "Click the upgrade on the left to learn it");
        var enNames = new Dictionary<string, string> { [ScientificUpgrades.PortalArrows] = "Magic arrows", [ScientificUpgrades.SharpAxes] = "Sharp axes",
            [ScientificUpgrades.QuietScouting] = "Quiet scouting", [ScientificUpgrades.StrongWalls] = "Strong walls", [ScientificUpgrades.FastHex] = "Hex exploration",
            [ScientificUpgrades.WarriorBaseRegen] = "Field infirmary", ["crystal_cells"] = "Crystal cells", ["crystal_power"] = "Crystal power" };
        foreach (var g in table.entries.GroupBy(e => e.GroupId)) Add("laboratory.group." + g.Key, g.First().groupTitle, enNames.TryGetValue(g.Key, out string en) ? en : g.First().groupTitle);
        EditorUtility.SetDirty(loc);
    }
}
#endif
