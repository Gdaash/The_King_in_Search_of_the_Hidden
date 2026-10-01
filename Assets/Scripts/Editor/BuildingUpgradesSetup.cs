#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Idempotent authoring of the shared upgrade assets. Does not touch player saves.</summary>
public static class BuildingUpgradesSetup
{
    private const string ButtonPath = "Assets/Prefabs/Base/Building Upgrade Button.prefab";
    public static void Run()
    {
        string catalogPath = "Assets/Resources/Base/Building Upgrades.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Base")) AssetDatabase.CreateFolder("Assets/Resources", "Base");
        var catalog = AssetDatabase.LoadAssetAtPath<BuildingUpgradeCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BuildingUpgradeCatalog>();
            Add(catalog, "fort", "base.fort", "Форт", "Swordsman", 0, 3, 7, 1);
            Add(catalog, "archery_range", "base.archery_range", "Стрельбище", "Archer", 0, 3, 7, 1);
            Add(catalog, "housing", "base.housing", "Жилой квартал", "Human", 3, 5, 9, 5);
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath) == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab")
                .GetComponentsInChildren<BaseBuildingConstruction>(true).First(b => b.BuildingId == "housing").transform.Find("Build Button");
            var go = Object.Instantiate(source.gameObject);
            go.name = "Building Upgrade Button";
            foreach (var badge in go.GetComponents<BuildingActionAvailabilityIndicator>()) Object.DestroyImmediate(badge);
            foreach (Transform child in go.transform.Cast<Transform>().ToArray())
                if (child.name != "Label") Object.DestroyImmediate(child.gameObject);
            var button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.targetGraphic = go.GetComponent<Image>();
            var group = go.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            var view = go.AddComponent<BuildingUpgradeButton>();
            Set(view, "button", button); Set(view, "label", go.GetComponentInChildren<Text>(true)); Set(view, "visibility", group);
            var label = go.GetComponentInChildren<Text>(true);
            label.text = "Улучшить 0/7"; label.fontSize = 16; label.raycastTarget = false;
            label.resizeTextForBestFit = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(190, 40); rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one;
            go.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(go, ButtonPath);
            Object.DestroyImmediate(go);
        }
        Attach("Assets/Prefabs/Base/Base Panel.prefab", "housing");
        Attach("Assets/Prefabs/UI/Screens/Base Screen HUD.prefab", "fort", "archery_range");
        foreach (string path in new[] { "Assets/Prefabs/Base/Fort Popup.prefab", "Assets/Prefabs/Base/Archery Range Popup.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponentInChildren<MilitaryTrainingView>(true);
                var text = (Text)new SerializedObject(view).FindProperty("warriorAmount").objectReferenceValue;
                text.rectTransform.sizeDelta = new Vector2(108, text.rectTransform.sizeDelta.y);
                text.rectTransform.anchoredPosition = new Vector2(49, text.rectTransform.anchoredPosition.y);
                text.text = "0 / 3"; text.fontSize = Mathf.Min(text.fontSize, 22);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Housing();
        PolishLayout();
        var localization = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Localize(localization, "base.upgrade.action", "Улучшить", "Upgrade");
        Localize(localization, "base.upgrade.max", "Максимум", "Maximum");
        Localize(localization, "base.upgrade.price", "Цена улучшения", "Upgrade cost");
        Localize(localization, "base.upgrade.effect", "Дополнительные места: +{0}\nВместимость: {1} → {2}", "Additional slots: +{0}\nCapacity: {1} → {2}");
        Localize(localization, "base.housing.capacity", "Места для жителей", "Civilian capacity");
        EditorUtility.SetDirty(localization);
        AssetDatabase.SaveAssets();
        Debug.Log("Building upgrades: catalog, three prefab buttons, capacity displays and localization authored.");
    }
    private static void Add(BuildingUpgradeCatalog catalog, string id, string key, string name, string resource, int before, int built, int count, int bonus)
    {
        var b = new BuildingUpgradeCatalog.Building { id = id, nameKey = key, displayName = name, capacityResource = Resource(resource), capacityBeforeConstruction = before, constructionCapacity = built };
        for (int i = 0; i < count; i++) b.levels.Add(new BuildingUpgradeCatalog.Level { additionalCapacity = bonus, resourceA = Resource("Wood"), resourceB = Resource("Stone") });
        catalog.buildings.Add(b);
    }
    private static ResourceType Resource(string name) => AssetDatabase.FindAssets("t:ResourceType").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ResourceType>).First(r => r.name == name);
    private static void Attach(string path, params string[] ids)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var building in root.GetComponentsInChildren<BaseBuildingConstruction>(true).Where(b => ids.Contains(b.BuildingId)))
            {
                if (building.GetComponentInChildren<BuildingUpgradeButton>(true) != null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), building.transform);
                var view = go.GetComponent<BuildingUpgradeButton>();
                var so = new SerializedObject(view); so.FindProperty("buildingId").stringValue = building.BuildingId; so.ApplyModifiedPropertiesWithoutUndo();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, 0); rt.pivot = new Vector2(.5f, 1);
                rt.anchoredPosition = new Vector2(0, -8); rt.localScale = Vector3.one;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Housing()
    {
        const string path = "Assets/Prefabs/Base/Housing Popup.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponentInChildren<HousingCapacityView>(true) != null) return;
            var go = new GameObject("Housing Capacity", typeof(RectTransform)); go.transform.SetParent(root.transform, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(650, 50); rect.anchoredPosition = new Vector2(0, -145);
            var view = go.AddComponent<HousingCapacityView>();
            var template = root.transform.Find("Description").GetComponent<Text>();
            Text Create(string name, string text, float x, float width)
            {
                var t = Object.Instantiate(template, go.transform); t.name = name;
                foreach (var c in t.GetComponents<LocalizedText>()) Object.DestroyImmediate(c);
                t.text = text; t.fontSize = 20; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(.5f, .5f);
                t.rectTransform.anchoredPosition = new Vector2(x, 0); t.rectTransform.sizeDelta = new Vector2(width, 50);
                return t;
            }
            var label = Create("Label", "Места для жителей", -100, 320);
            var amount = Create("Amount", "3 / 8", 205, 120);
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<Image>(); icon.sprite = Resource("Human").resourceIcon; icon.raycastTarget = false; icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = icon.sprite.rect.size * 2; icon.rectTransform.anchoredPosition = new Vector2(115, 0);
            Set(view, "capacityLabel", label); Set(view, "amount", amount); Set(view, "icon", icon);
            var rootRect = root.GetComponent<RectTransform>(); rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, Mathf.Max(390, rootRect.sizeDelta.y));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static void PolishLayout()
    {
        var button = PrefabUtility.LoadPrefabContents(ButtonPath);
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab")
                .GetComponentsInChildren<BaseBuildingConstruction>(true).First(b => b.BuildingId == "housing").transform.Find("Label").GetComponent<Text>();
            var label = button.GetComponentInChildren<Text>(true);
            label.font = source.font; label.fontStyle = source.fontStyle; label.fontSize = 16;
            label.material = source.material;
            PrefabUtility.SaveAsPrefabAsset(button, ButtonPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(button); }
        const string path = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
        var hud = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var building in hud.GetComponentsInChildren<BaseBuildingConstruction>(true))
                if (building.BuildingId == "fort" || building.BuildingId == "archery_range")
                {
                    var rt = building.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -210);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
                }
            var housing = hud.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Housing Popup");
            ((RectTransform)housing.Find("Artwork Frame")).sizeDelta = new Vector2(800, 380);
            foreach (var pair in new[] { ("Title", 132f), ("Description", 65f), ("Forecast", -20f), ("Housing Capacity", -120f) })
            {
                var rt = (RectTransform)housing.Find(pair.Item1);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, pair.Item2);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
            }
            var close = (RectTransform)housing.Find("Close"); close.anchoredPosition = new Vector2(365, 162);
            PrefabUtility.RecordPrefabInstancePropertyModifications(close);
            PrefabUtility.SaveAsPrefabAsset(hud, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(hud); }
    }
    private static void Set(Object target, string field, Object value) { var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Localize(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.Find(e => e.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        while (entry.values.Count < table.languages.Count) entry.values.Add("");
        entry.values[table.languages.IndexOf("ru")] = ru; entry.values[table.languages.IndexOf("en")] = en;
    }
}
#endif
