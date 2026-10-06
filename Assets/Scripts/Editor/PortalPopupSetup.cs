#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Authors the portal window and shared upgrade instances; never edits player saves or saves open scenes.</summary>
public static class PortalPopupSetup
{
    public const string PopupPath = "Assets/Prefabs/Base/Portal Popup.prefab";
    public const string RowPath = "Assets/Prefabs/UI/Portal Location Row.prefab";
    private const string UpgradePath = "Assets/Prefabs/Base/Building Upgrade Button.prefab";
    private static readonly Color Ink = new(.94f, .91f, .82f), Gold = new(.92f, .83f, .60f), Muted = new(.66f, .61f, .72f);
    private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
    private static Sprite Panel => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Evolution/Popups/Sprites/Shared/PopupPanel9Slice.png");
    private static Sprite Lock => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Evolution/Gameplay/Sprites/ConstructionPointLock.png");

    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring prefabs.");
        ConfigureProgression();
        Localize();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPath) == null) CreateRow();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath) == null) CreatePopup();
        var popup = PrefabUtility.LoadPrefabContents(PopupPath);
        try { ConfigureClose(popup.GetComponent<PortalPopupView>()); PrefabUtility.SaveAsPrefabAsset(popup, PopupPath); }
        finally { PrefabUtility.UnloadPrefabContents(popup); }
        foreach (string path in new[] { "Assets/Prefabs/Base/Islands/Portal Island.prefab", "Assets/Prefabs/Base/Base Panel.prefab",
                     "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab", "Assets/Prefabs/Base/Variants/Base Location Variant.prefab",
                     "Assets/Prefabs/Base/Variants/Base Islands Backup.prefab" })
            UpdatePrefab(path);
        AssetDatabase.SaveAssets();
        Debug.Log("Portal: shared two-column popup, five destinations and four persistent upgrades authored.");
    }

    private static void ConfigureProgression()
    {
        var catalog = BuildingUpgradeService.Catalog;
        bool added = catalog.Find(PortalProgression.BuildingId) == null;
        if (added)
        {
            var definition = new BuildingUpgradeCatalog.Building { id = PortalProgression.BuildingId, nameKey = "base.portal", displayName = "Портал",
                builtByDefault = true, effect = BuildingUpgradeCatalog.UpgradeEffect.PortalAccess, constructionCapacity = 0 };
            var ore = Resources.Load<ResourceType>("ResourceTypes/MagicOre");
            foreach (int cost in new[] { 10, 15, 20, 25 }) definition.levels.Add(new BuildingUpgradeCatalog.Level { additionalCapacity = 0, resourceA = ore, costA = cost, resourceB = null, costB = 0 });
            catalog.buildings.Add(definition);
            EditorUtility.SetDirty(catalog);
            int level = 0;
            foreach (var location in DayCycleService.GetPortalLocations())
            {
                var so = new SerializedObject(location); so.FindProperty("requiredPortalLevel").intValue = level++; so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static RectTransform Rect(string name, Transform parent, float w, float h, float x = 0, float y = 0)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new(.5f, .5f); rt.sizeDelta = new(w, h); rt.anchoredPosition = new(x, y); return rt;
    }
    private static void Stretch(RectTransform r, float inset = 0)
    { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.one * inset; r.offsetMax = -Vector2.one * inset; }
    private static Image Image(RectTransform r, Color color, Sprite sprite = null)
    {
        var image = r.gameObject.AddComponent<Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false;
        image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; return image;
    }
    private static Image Icon(string name, Transform parent, Sprite sprite, float x, float y, Color? color = null)
    {
        var image = Image(Rect(name, parent, 0, 0, x, y), color ?? Color.white, sprite);
        image.type = UnityEngine.UI.Image.Type.Simple; ResourceIconSizing.Apply(image, sprite); return image;
    }
    private static Text Text(string name, Transform parent, string text, float w, float h, float x, float y, int size = 24, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var label = Rect(name, parent, w, h, x, y).gameObject.AddComponent<Text>(); label.text = text; label.font = Font; label.fontSize = size;
        label.color = Ink; label.alignment = align; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
    }
    private static void Translate(Text text, string key)
    { var so = new SerializedObject(text.gameObject.AddComponent<LocalizedText>()); so.FindProperty("key").stringValue = key; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Set(Object target, string field, Object value)
    { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

    private static void CreateRow()
    {
        var root = Rect("Portal Location Row", null, 428, 88);
        var row = root.gameObject.AddComponent<PortalLocationRow>();
        row.border = Image(root, row.normalColor); row.border.raycastTarget = true;
        row.button = root.gameObject.AddComponent<Button>(); row.button.targetGraphic = row.border; row.button.transition = Selectable.Transition.None;
        var surface = Rect("Surface", root, 0, 0); Stretch(surface, 2); row.surface = Image(surface, new(.14f, .115f, .17f));
        row.selection = Image(Rect("Selected", root, 4, 72, -209, 0), Gold);
        row.title = Text("Name", root, "Магические горы", 342, 36, -18, 16, 26);
        row.status = Text("Access", root, "Улучшите портал до ур. 4", 342, 28, -18, -20, 19);
        row.lockIcon = Icon("Lock", root, Lock, 184, 0, Gold);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPath); Object.DestroyImmediate(root.gameObject);
    }

    private static void CreatePopup()
    {
        var root = Rect("Portal Popup", null, 0, 0); Stretch(root); Image(root, Color.clear).raycastTarget = true;
        root.gameObject.AddComponent<PopupDimmerLink>(); var view = root.gameObject.AddComponent<PortalPopupView>();
        var window = Rect("Window", root, 1280, 800); window.gameObject.AddComponent<CanvasWindowFit>(); Image(window, Color.white, Panel).raycastTarget = true;
        var heading = Text("Title", window, "ПОРТАЛ", 1100, 50, 0, 338, 36, TextAnchor.MiddleCenter); heading.color = Gold; Translate(heading, "base.portal.popup_title");
        view.portalLevel = Text("Portal Level", window, "Уровень портала: 0 / 4", 1110, 32, 0, 292, 22, TextAnchor.MiddleCenter); view.portalLevel.color = Muted;
        Image(Rect("Header Divider", window, 1196, 2, 0, 262), new(.35f, .30f, .39f));
        var close = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Popup Close Icon.prefab"), window);
        close.name = "Close"; var cr = (RectTransform)close.transform; cr.anchorMin = cr.anchorMax = cr.pivot = new(.5f, .5f); cr.anchoredPosition = new(590, 352); cr.localScale = Vector3.one;
        ConfigureClose(view);
        var caption = Text("Locations Caption", window, "ВЫБЕРИТЕ ЛОКАЦИЮ", 428, 34, -384, 226, 21); caption.color = Muted; Translate(caption, "base.portal.locations");
        var list = Rect("Locations", window, 428, 488, -384, -41);
        var rows = new List<PortalLocationRow>(); int index = 0;
        foreach (var definition in DayCycleService.GetPortalLocations())
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RowPath), list); go.name = definition.LocationId;
            ((RectTransform)go.transform).anchoredPosition = new(0, 200 - index++ * 100);
            var row = go.GetComponent<PortalLocationRow>(); row.location = definition; rows.Add(row); PrefabUtility.RecordPrefabInstancePropertyModifications(row);
        }
        view.locations = rows.ToArray();
        var detail = Rect("Destination Details", window, 728, 564, 232, -30); Image(detail, new(.12f, .095f, .145f));
        Image(Rect("Top Rule", detail, 728, 3, 0, 282), new(.48f, .40f, .53f));
        view.title = Text("Location Name", detail, "Лес", 672, 46, 0, 236, 32); view.title.color = Gold;
        view.description = Text("Description", detail, "Базовая лесная локация.", 672, 88, 0, 158, 24, TextAnchor.UpperLeft); view.description.lineSpacing = 1.1f;
        var danger = Text("Danger Label", detail, "Опасность", 210, 50, -231, 81, 22); Translate(danger, "base.portal.tooltip.danger");
        var skull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Common/DangerSkull.png");
        view.dangerSkulls = Enumerable.Range(0, 5).Select(i => Icon("Danger Skull " + (i + 1), detail, skull, -75 + i * (skull.rect.width * 2 + 12), 81)).ToArray();
        Image(Rect("Resources Divider", detail, 672, 2, 0, 47), new(.30f, .25f, .35f));
        var resources = Text("Resources Caption", detail, "РЕСУРСЫ ЛОКАЦИИ", 672, 30, 0, 21, 20); resources.color = Muted; Translate(resources, "base.portal.resources");
        var resourceRows = new List<PortalPopupView.ResourceRow>();
        for (int i = 0; i < 4; i++)
        {
            var rr = Rect("Resource " + (i + 1), detail, 672, 56, 0, -35 - i * 63); Image(rr, i % 2 == 0 ? new Color(.17f, .14f, .195f) : new Color(.135f, .11f, .16f));
            var resource = DayCycleService.GetPortalLocations()[0].Resources[i];
            var icon = Icon("Resource Icon", rr, resource.resource.resourceIcon, -288, 0);
            var abundance = Text("Abundance", rr, resource.fallbackAbundance, 520, 50, 50, 0, 24);
            resourceRows.Add(new PortalPopupView.ResourceRow { root = rr.gameObject, icon = icon, abundance = abundance });
        }
        view.resourceRows = resourceRows.ToArray();
        var once = Text("Travel Rule", window, "Путешествовать через портал можно только один раз в день.", 428, 72, -384, -348, 19); once.color = Muted; Translate(once, "base.portal.travel_once");
        view.status = Text("Travel Status", window, "Портал готов к переходу", 350, 74, 52, -350, 21);
        var travel = Rect("Travel", window, 336, 68, 428, -350);
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePath);
        var graphic = Image(travel, Color.white, template.GetComponent<Image>().sprite); graphic.raycastTarget = true;
        view.travelButton = travel.gameObject.AddComponent<Button>(); view.travelButton.targetGraphic = graphic; view.travelButton.transition = Selectable.Transition.None;
        var feedback = travel.gameObject.AddComponent<UnifiedButtonFeedback>(); EditorUtility.CopySerialized(template.GetComponent<UnifiedButtonFeedback>(), feedback);
        var feedbackData = new SerializedObject(feedback); feedbackData.FindProperty("animateScale").boolValue = true; feedbackData.ApplyModifiedPropertiesWithoutUndo();
        view.travelLabel = Text("Label", travel, "Отправиться", 310, 50, 0, 0, 26, TextAnchor.MiddleCenter);
        view.Refresh();
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PopupPath); Object.DestroyImmediate(root.gameObject);
    }

    private static void UpdatePrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var controller in root.GetComponentsInChildren<BaseUIController>(true))
            {
                var fields = new SerializedObject(controller);
                var old = fields.FindProperty("globalMap").objectReferenceValue as GameObject;
                if (old == null || old.GetComponent<PortalPopupView>() != null) continue;
                var oldLink = old.GetComponent<PopupDimmerLink>();
                var dimmer = oldLink != null ? new SerializedObject(oldLink).FindProperty("dimmer").objectReferenceValue : null;
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath), old.transform.parent);
                replacement.transform.SetSiblingIndex(old.transform.GetSiblingIndex()); replacement.SetActive(old.activeSelf);
                foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && !c.transform.IsChildOf(old.transform)))
                {
                    var so = new SerializedObject(c); var p = so.GetIterator(); bool changed = false;
                    while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (p.objectReferenceValue == old) { p.objectReferenceValue = replacement; changed = true; }
                        else if (p.objectReferenceValue == old.transform) { p.objectReferenceValue = replacement.transform; changed = true; }
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
                fields.Update(); fields.FindProperty("globalMap").objectReferenceValue = replacement;
                fields.FindProperty("portalButtons").arraySize = 0;
                fields.FindProperty("portalTravelNotice").objectReferenceValue = null;
                fields.FindProperty("portalTravelUsedNotice").objectReferenceValue = null;
                fields.ApplyModifiedPropertiesWithoutUndo();
                Set(replacement.GetComponent<PopupDimmerLink>(), "dimmer", dimmer);
                Object.DestroyImmediate(old);
            }
            foreach (var portal in root.GetComponentsInChildren<Button>(true).Where(b => b.name == "Portal" || b.name == "Portal Island").ToArray())
                AttachUpgrade(portal);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureClose(PortalPopupView view)
    {
        var close = view.transform.Find("Window/Close");
        var graphic = close.GetComponent<Image>(); graphic.raycastTarget = true;
        view.closeButton = close.GetComponent<Button>() ?? close.gameObject.AddComponent<Button>();
        view.closeButton.targetGraphic = graphic; view.closeButton.transition = Selectable.Transition.None;
        var feedback = close.GetComponent<UnifiedButtonFeedback>();
        if (feedback == null)
        {
            feedback = close.gameObject.AddComponent<UnifiedButtonFeedback>();
            EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePath).GetComponent<UnifiedButtonFeedback>(), feedback);
            var so = new SerializedObject(feedback); so.FindProperty("hoverScaleMultiplier").floatValue = 1.3f; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AttachUpgrade(Button portal)
    {
        if (portal.GetComponentInChildren<BuildingUpgradeButton>(true) != null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePath), portal.transform);
        var upgrade = go.GetComponent<BuildingUpgradeButton>(); var so = new SerializedObject(upgrade);
        so.FindProperty("buildingId").stringValue = PortalProgression.BuildingId; so.ApplyModifiedPropertiesWithoutUndo();
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new(.5f, .5f); rt.anchoredPosition = new(0, -86); rt.sizeDelta = new(216, 48); rt.localScale = Vector3.one;
        var label = go.GetComponentInChildren<Text>(true); label.text = "Улучшить 0/4";
        var world = portal.GetComponent<WorldBuildingButton>(); if (world != null) Set(world, "upgradeButton", rt);
        IndependentBuildingButtonFeedbackSetup.Configure(portal);
        foreach (var component in new Component[] { rt, upgrade, label, world })
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static void Localize()
    {
        var table = Resources.Load<LocalizationTable>("Localization/Base Localization");
        void Add(string key, string ru, string en)
        {
            var e = table.entries.Find(x => x.key == key); if (e == null) { e = new LocalizationTable.Entry { key = key }; table.entries.Add(e); }
            while (e.values.Count < table.languages.Count) e.values.Add("");
            for (int i = 0; i < table.languages.Count; i++) e.values[i] = table.languages[i] == "ru" ? ru : en;
        }
        Add("base.portal.popup_title", "ПОРТАЛ", "PORTAL");
        Add("base.portal.level", "Уровень портала: {0} / {1}", "Portal level: {0} / {1}");
        Add("base.portal.locations", "ВЫБЕРИТЕ ЛОКАЦИЮ", "CHOOSE A DESTINATION");
        Add("base.portal.resources", "РЕСУРСЫ ЛОКАЦИИ", "LOCATION RESOURCES");
        Add("base.portal.available", "Доступно", "Available");
        Add("base.portal.requires_level", "Улучшите портал до ур. {0}", "Upgrade portal to level {0}");
        Add("base.portal.depart", "Отправиться", "Travel");
        Add("base.portal.ready", "Портал готов к переходу", "The portal is ready");
        Add("base.portal.upgrade_effect", "Открывает локацию: {0}\nДоступ сохраняется между походами.", "Unlocks: {0}\nAccess is permanent between expeditions.");
        EditorUtility.SetDirty(table);
    }
}
#endif
