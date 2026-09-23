#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Base;
using GameFoundation.MetaProgression;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

public static class MilitaryFeatureSetup
{
    private struct IconAmount { public UIImage icon; public Text text; }
    private struct WorldRow { public Button summon, recall; public Text stored, deployed; }

    [MenuItem("Tools/Game Setup/Rebuild Military Buildings")]
    public static void Build()
    {
        CreateResource("Assets/Prefabs/Resources/Swordsman.asset", "Мечник", "Assets/Prefabs/Units/RedSwordsman.prefab");
        CreateResource("Assets/Prefabs/Resources/Archer.asset", "Лучник", "Assets/Prefabs/Units/RedArcher.prefab");
        AssetDatabase.SaveAssets();
        ConfigureBase();
        ConfigureWorld();
        ResourceType sword = LoadResource("Sword"), bow = LoadResource("Bow");
        UpdateResourcesPrefab(sword, bow);
        AddLocalization();
        AssetDatabase.SaveAssets();
    }

    private static ResourceType CreateResource(string path, string displayName, string unitPath)
    {
        ResourceType asset = AssetDatabase.LoadAssetAtPath<ResourceType>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<ResourceType>(); AssetDatabase.CreateAsset(asset, path); }
        SpriteRenderer renderer = AssetDatabase.LoadAssetAtPath<GameObject>(unitPath)?.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
        asset.resourceName = displayName;
        asset.isHumanResource = false;
        asset.resourceIcon = renderer != null ? renderer.sprite : null;
        asset.defaultCarrySprite = asset.resourceIcon;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void AddResources(GlobalResourceManager manager, params ResourceType[] resources)
    {
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty available = so.FindProperty("availableResources");
        SerializedProperty initial = so.FindProperty("initialValues");
        foreach (ResourceType resource in resources)
        {
            bool exists = false;
            for (int i = 0; i < available.arraySize; i++) exists |= available.GetArrayElementAtIndex(i).objectReferenceValue == resource;
            if (!exists) { available.InsertArrayElementAtIndex(available.arraySize); available.GetArrayElementAtIndex(available.arraySize - 1).objectReferenceValue = resource; }
            exists = false;
            for (int i = 0; i < initial.arraySize; i++) exists |= initial.GetArrayElementAtIndex(i).FindPropertyRelative("resourceType").objectReferenceValue == resource;
            if (!exists)
            {
                initial.InsertArrayElementAtIndex(initial.arraySize);
                SerializedProperty entry = initial.GetArrayElementAtIndex(initial.arraySize - 1);
                entry.FindPropertyRelative("resourceType").objectReferenceValue = resource;
                entry.FindPropertyRelative("startAmount").intValue = 0;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void ConfigureBase()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Base.unity", OpenSceneMode.Single);
        ResourceType swordsmen = LoadResource("Swordsman"), archers = LoadResource("Archer");
        ResourceType human = LoadResource("Human"), sword = LoadResource("Sword"), bow = LoadResource("Bow");
        AddResources(Object.FindAnyObjectByType<GlobalResourceManager>(), swordsmen, archers, sword, bow);
        GameObject ui = GameObject.Find("LevelManager/Canvas/Base UI");
        Transform panel = ui.transform.Find("Base Panel");
        GameObject sourceButton = panel.Find("Warehouse").gameObject;
        GameObject fortButton = MakeBuildingButton(sourceButton, panel, "Fort", new Vector2(-200, -245), "base.base_panel.fort.label");
        GameObject rangeButton = MakeBuildingButton(sourceButton, panel, "Archery Range", new Vector2(200, -245), "base.base_panel.archery_range.label");
        GameObject sourcePopup = ui.transform.Find("Warehouse Popup").gameObject;
        GameObject fort = MakeTrainingPopup(sourcePopup, ui.transform, "Fort Popup", "base.fort_popup.title", human, sword, swordsmen);
        GameObject range = MakeTrainingPopup(sourcePopup, ui.transform, "Archery Range Popup", "base.archery_range_popup.title", human, bow, archers);
        MilitaryBuildingsController controller = ui.GetComponent<MilitaryBuildingsController>() ?? ui.AddComponent<MilitaryBuildingsController>();
        SetObject(controller, "fortButton", fortButton.GetComponent<Button>());
        SetObject(controller, "archeryRangeButton", rangeButton.GetComponent<Button>());
        SetObject(controller, "fortPopup", fort);
        SetObject(controller, "archeryRangePopup", range);
        PrefabUtility.SaveAsPrefabAssetAndConnect(fort, "Assets/Prefabs/Base/Fort Popup.prefab", InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(range, "Assets/Prefabs/Base/Archery Range Popup.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject MakeBuildingButton(GameObject source, Transform parent, string name, Vector2 position, string localizationKey)
    {
        Transform old = parent.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = Object.Instantiate(source, parent); go.name = name;
        ((RectTransform)go.transform).anchoredPosition = position;
        Text text = go.GetComponentInChildren<Text>(true); if (text != null) SetLocalized(text.gameObject, localizationKey);
        go.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        return go;
    }

    private static GameObject MakeTrainingPopup(GameObject source, Transform parent, string name, string titleKey, ResourceType human, ResourceType weapon, ResourceType warrior)
    {
        Transform old = parent.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = Object.Instantiate(source, parent); go.name = name; go.SetActive(true);
        WarehouseCartPurchaseView cart = go.GetComponent<WarehouseCartPurchaseView>(); if (cart != null) Object.DestroyImmediate(cart);
        GameObject standardButton = go.transform.Find("Buy Cart").gameObject;
        ((RectTransform)go.transform.Find("Artwork Frame")).sizeDelta = new Vector2(700, 500);
        Transform titleTransform = go.transform.Find("Title");
        SetLocalized(titleTransform.gameObject, titleKey);
        ((RectTransform)titleTransform).anchoredPosition = new Vector2(0, 200);
        Button close = go.transform.Find("Close").GetComponent<Button>(); close.onClick = new Button.ButtonClickedEvent();
        ((RectTransform)close.transform).anchoredPosition = new Vector2(315, 215);
        IconAmount stock = CreateStockRow(go.transform, new Vector2(0, 110), warrior);
        GameObject costs = NewRect("Arm Cost", go.transform, new Vector2(0, 30), new Vector2(430, 64));
        IconAmount humans = CreateIconAmount(costs.transform, "Human", new Vector2(-105, 0), human);
        CreateText(costs.transform, "Plus", Vector2.zero, new Vector2(40, 45), "+", 28);
        IconAmount weapons = CreateIconAmount(costs.transform, "Weapon", new Vector2(105, 0), weapon);
        Button arm = CloneButton(standardButton, go.transform, "Arm", new Vector2(0, -65), "base.military_popup.arm");
        Button disarm = CloneButton(standardButton, go.transform, "Disarm", new Vector2(0, -145), "base.military_popup.disarm");
        DestroyChild(go.transform, "Buy Cart"); DestroyChild(go.transform, "Cart Stock");
        MilitaryTrainingView view = go.AddComponent<MilitaryTrainingView>();
        SetObject(view, "human", human); SetObject(view, "weapon", weapon); SetObject(view, "warrior", warrior);
        SetObject(view, "armButton", arm); SetObject(view, "disarmButton", disarm);
        SetObject(view, "humanIcon", humans.icon); SetObject(view, "weaponIcon", weapons.icon); SetObject(view, "warriorIcon", stock.icon);
        SetObject(view, "humanAmount", humans.text); SetObject(view, "weaponAmount", weapons.text); SetObject(view, "warriorAmount", stock.text);
        go.SetActive(false);
        return go;
    }

    private static void ConfigureWorld()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Single);
        ResourceType swordsmen = LoadResource("Swordsman"), archers = LoadResource("Archer");
        AddResources(Object.FindAnyObjectByType<GlobalResourceManager>(), swordsmen, archers, LoadResource("Sword"), LoadResource("Bow"));
        Transform canvas = GameObject.Find("LevelManager/Canvas").transform;
        DestroyChild(canvas, "Military Controls");
        GameObject panel = NewRect("Military Controls", canvas, new Vector2(24, 96), new Vector2(490, 170));
        RectTransform rt = (RectTransform)panel.transform; rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
        UIImage background = panel.AddComponent<UIImage>(); background.color = new Color(0.10f, 0.065f, 0.10f, 0.98f);
        WorldMilitaryDeploymentController controller = panel.AddComponent<WorldMilitaryDeploymentController>();
        CreateText(panel.transform, "Title", new Vector2(0, 58), new Vector2(450, 40), "ВОЙСКА", 24);
        WorldRow swords = CreateWorldRow(panel.transform, "Swordsmen", new Vector2(0, 15), swordsmen);
        WorldRow bows = CreateWorldRow(panel.transform, "Archers", new Vector2(0, -45), archers);
        SetObject(controller, "portal", GameObject.Find("PortalTower").transform);
        SerializedObject so = new SerializedObject(controller);
        SetControl(so.FindProperty("swordsmen"), swordsmen, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/RedSwordsman.prefab"), swords);
        SetControl(so.FindProperty("archers"), archers, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/RedArcher.prefab"), bows);
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(panel, "Assets/Prefabs/UI/HUD/Military Controls.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene);
    }

    private static WorldRow CreateWorldRow(Transform parent, string name, Vector2 position, ResourceType resource)
    {
        GameObject row = NewRect(name, parent, position, new Vector2(460, 54));
        IconAmount stock = CreateIconAmount(row.transform, "Stock", new Vector2(-160, 0), resource);
        Text deployed = CreateText(row.transform, "Deployed", new Vector2(-72, 0), new Vector2(55, 42), "0", 21);
        Button summon = SimpleButton(row.transform, "Summon", new Vector2(52, 0), new Vector2(170, 46), "Призвать");
        Button recall = SimpleButton(row.transform, "Recall", new Vector2(184, 0), new Vector2(90, 46), "Отозвать");
        return new WorldRow { summon = summon, recall = recall, stored = stock.text, deployed = deployed };
    }

    private static void UpdateResourcesPrefab(params ResourceType[] resources)
    {
        const string path = "Assets/Prefabs/UI/ResourcesUI.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            SerializedObject so = new SerializedObject(root.GetComponent<ResourceUI>());
            SerializedProperty cells = so.FindProperty("globalCells");
            for (int n = 0; n < resources.Length; n++)
            {
                ResourceType resource = resources[n]; bool exists = false;
                for (int i = 0; i < cells.arraySize; i++) exists |= cells.GetArrayElementAtIndex(i).FindPropertyRelative("resource").objectReferenceValue == resource;
                if (exists) continue;
                GameObject cell = Object.Instantiate(root.transform.Find("MagicOre").gameObject, root.transform); cell.name = resource.name;
                ((RectTransform)cell.transform).anchoredPosition = new Vector2(544 + 136 * n, -40);
                UIImage icon = cell.transform.Find("Icon").GetComponent<UIImage>(); icon.sprite = resource.resourceIcon; SetSpriteSize((RectTransform)icon.transform, icon.sprite);
                ResourceTooltipTrigger tip = cell.GetComponent<ResourceTooltipTrigger>();
                if (tip != null) tip.Initialize(resource, cell.transform as RectTransform);
                cells.InsertArrayElementAtIndex(cells.arraySize);
                SerializedProperty entry = cells.GetArrayElementAtIndex(cells.arraySize - 1);
                entry.FindPropertyRelative("resource").objectReferenceValue = resource;
                entry.FindPropertyRelative("count").objectReferenceValue = cell.transform.Find("Count").GetComponent<TMPro.TextMeshProUGUI>();
            }
            int visibleIndex = 0;
            foreach (Transform child in root.transform)
            {
                if (child.GetComponent<ResourceTooltipTrigger>() == null) continue;
                ((RectTransform)child).anchoredPosition = new Vector2(-680 + 136 * visibleIndex++, -40);
            }
            ((RectTransform)root.transform).sizeDelta = new Vector2(1494, 80);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddLocalization()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prefabs/Base/Base Localization.asset");
        if (table == null) return;
        AddLocalizationEntry(table, "resource.Swordsman.name", "Мечники", "Swordsmen");
        AddLocalizationEntry(table, "resource.Archer.name", "Лучники", "Archers");
        AddLocalizationEntry(table, "base.base_panel.fort.label", "Форт", "Fort");
        AddLocalizationEntry(table, "base.base_panel.archery_range.label", "Стрельбище", "Archery range");
        AddLocalizationEntry(table, "base.fort_popup.title", "ФОРТ", "FORT");
        AddLocalizationEntry(table, "base.archery_range_popup.title", "СТРЕЛЬБИЩЕ", "ARCHERY RANGE");
        AddLocalizationEntry(table, "base.military_popup.arm", "Вооружить", "Arm");
        AddLocalizationEntry(table, "base.military_popup.disarm", "Разоружить", "Disarm");
        EditorUtility.SetDirty(table);
    }

    private static void AddLocalizationEntry(LocalizationTable table, string key, string russian, string english)
    {
        LocalizationTable.Entry entry = table.entries.Find(item => item.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        while (entry.values.Count < 2) entry.values.Add(string.Empty);
        entry.values[0] = russian; entry.values[1] = english;
    }

    private static IconAmount CreateStockRow(Transform parent, Vector2 position, ResourceType resource)
    {
        GameObject holder = NewRect("Stock", parent, position, new Vector2(420, 64));
        CreateText(holder.transform, "Label", new Vector2(-100, 0), new Vector2(190, 45), "В запасе", 22);
        return CreateIconAmount(holder.transform, "Resource", new Vector2(90, 0), resource);
    }

    private static IconAmount CreateIconAmount(Transform parent, string name, Vector2 position, ResourceType resource)
    {
        GameObject holder = NewRect(name, parent, position, new Vector2(150, 64));
        GameObject iconObject = NewRect("Icon", holder.transform, new Vector2(-35, 0), new Vector2(48, 48));
        UIImage icon = iconObject.AddComponent<UIImage>(); icon.sprite = resource.resourceIcon; icon.preserveAspect = true; icon.raycastTarget = false; SetSpriteSize((RectTransform)icon.transform, icon.sprite);
        Text amount = CreateText(holder.transform, "Amount", new Vector2(30, 0), new Vector2(70, 45), "0", 25);
        return new IconAmount { icon = icon, text = amount };
    }

    private static Button CloneButton(GameObject source, Transform parent, string name, Vector2 position, string localizationKey)
    {
        GameObject go = Object.Instantiate(source, parent); go.name = name;
        RectTransform rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(330, 58);
        Button button = go.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
        DestroyChild(go.transform, "Price");
        Text text = go.GetComponentInChildren<Text>(true); if (text != null) { SetLocalized(text.gameObject, localizationKey); text.fontSize = 22; }
        return button;
    }

    private static Button SimpleButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
    {
        GameObject go = NewRect(name, parent, position, size);
        UIImage image = go.AddComponent<UIImage>(); image.color = new Color(.36f, .29f, .40f);
        Button button = go.AddComponent<Button>(); button.targetGraphic = image;
        go.AddComponent<UnifiedButtonFeedback>();
        CreateText(go.transform, "Label", Vector2.zero, size, label, 18);
        return button;
    }

    private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, string value, int fontSize)
    {
        GameObject go = NewRect(name, parent, position, size);
        Text text = go.AddComponent<Text>(); text.text = value;
        Text[] existing = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
        text.font = existing.Length > 0 && existing[0].font != null ? existing[0].font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.94f, .90f, .78f); text.raycastTarget = false;
        return text;
    }

    private static GameObject NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform; rt.anchoredPosition = position; rt.sizeDelta = size;
        return go;
    }

    private static void SetControl(SerializedProperty property, ResourceType resource, GameObject prefab, WorldRow row)
    {
        property.FindPropertyRelative("resource").objectReferenceValue = resource;
        property.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        property.FindPropertyRelative("summonButton").objectReferenceValue = row.summon;
        property.FindPropertyRelative("recallButton").objectReferenceValue = row.recall;
        property.FindPropertyRelative("storedAmount").objectReferenceValue = row.stored;
        property.FindPropertyRelative("deployedAmount").objectReferenceValue = row.deployed;
    }

    private static void SetObject(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLocalized(GameObject target, string key)
    {
        LocalizedText localized = target.GetComponent<LocalizedText>() ?? target.AddComponent<LocalizedText>();
        SerializedObject so = new SerializedObject(localized);
        so.FindProperty("key").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ResourceType LoadResource(string name) => AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/" + name + ".asset");
    private static void DestroyChild(Transform parent, string name) { Transform child = parent.Find(name); if (child != null) Object.DestroyImmediate(child.gameObject); }
    private static void SetSpriteSize(RectTransform transform, Sprite sprite) { if (sprite != null) transform.sizeDelta = new Vector2(sprite.rect.width * 2, sprite.rect.height * 2); }
}
#endif
