using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BaseBuildingConstructionSetup
{
    private const string PanelPath = "Assets/Prefabs/Base/Base Panel.prefab";
    private const string TooltipPath = "Assets/Prefabs/Base/Building Construction Tooltip.prefab";
    private const string ScenePath = "Assets/Scenes/Base.unity";

    private sealed class Definition
    {
        public string objectName, id, nameKey, ruName, enName, descriptionKey, ruDescription, enDescription;
    }

    private static readonly Definition[] Definitions =
    {
        New("Laboratory", "laboratory", "base.base_panel.laboratory.label", "Научная лаборатория", "Laboratory",
            "base.building.laboratory.description", "Открывает дерево научных улучшений.", "Opens the scientific upgrade tree."),
        New("Housing", "housing", "base.base_panel.housing.label", "Жилой квартал", "Housing",
            "base.building.housing.description", "Показывает число жителей, расход еды и возможные смерти от голода.", "Shows population, food consumption, and possible starvation deaths."),
        New("Refugees", "refugees", "base.base_panel.refugees.label", "Лагерь беженцев", "Refugee camp",
            "base.building.refugees.description", "Позволяет принимать новых жителей в поселение.", "Allows new residents to join the settlement."),
        New("Square", "square", "base.base_panel.square.label", "Площадь", "Square",
            "base.building.square.description", "Место отдыха жителей поселения.", "A resting place for settlement residents."),
        New("Fort", "fort", "base.base_panel.fort.label", "Форт", "Fort",
            "base.building.fort.description", "Позволяет вооружать жителей мечами и создавать мечников.", "Turns residents with swords into swordsmen."),
        New("Archery Range", "archery_range", "base.base_panel.archery_range.label", "Стрельбище", "Archery range",
            "base.building.archery_range.description", "Позволяет вооружать жителей луками и создавать лучников.", "Turns residents with bows into archers.")
    };

    [MenuItem("Tools/Game Foundation/Setup Base Construction")]
    public static void Run()
    {
        ResourceType wood = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/Wood.asset");
        ResourceType stone = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/Stone.asset");
        Font font = FindAsset<Font>("OpenTTD-Sans");
        Sprite buttonSprite = FindSprite("PopupButton9Slice");
        Sprite panelSprite = FindSprite("PopupFrame9Slice") ?? FindSprite("PopupPanel9Slice") ?? buttonSprite;

        SetupPanelPrefab(wood, stone, buttonSprite, font);
        GameObject tooltipPrefab = CreateTooltipPrefab(wood, stone, panelSprite, font);
        SetupScene(wood, stone, buttonSprite, font, tooltipPrefab);
        SetupLocalization();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Definition New(string objectName, string id, string nameKey, string ruName, string enName,
        string descriptionKey, string ruDescription, string enDescription) => new Definition
        {
            objectName = objectName, id = id, nameKey = nameKey, ruName = ruName, enName = enName,
            descriptionKey = descriptionKey, ruDescription = ruDescription, enDescription = enDescription
        };

    private static void SetupPanelPrefab(ResourceType wood, ResourceType stone, Sprite buttonSprite, Font font)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PanelPath);
        foreach (Definition definition in Definitions)
        {
            if (definition.objectName == "Fort" || definition.objectName == "Archery Range") continue;
            Transform target = root.transform.Find(definition.objectName);
            if (target != null) ConfigureBuilding(target.gameObject, definition, wood, stone, buttonSprite, font);
        }
        PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetupScene(ResourceType wood, ResourceType stone, Sprite buttonSprite, Font font, GameObject tooltipPrefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        GameObject panel = GameObject.Find("Base Panel");
        foreach (Definition definition in Definitions)
        {
            if (definition.objectName != "Fort" && definition.objectName != "Archery Range") continue;
            Transform target = panel != null ? panel.transform.Find(definition.objectName) : null;
            if (target != null) ConfigureBuilding(target.gameObject, definition, wood, stone, buttonSprite, font);
        }

        GameObject old = GameObject.Find("Building Construction Tooltip");
        if (old != null) Object.DestroyImmediate(old);
        Transform baseUi = GameObject.Find("Base UI")?.transform;
        GameObject tooltip = PrefabUtility.InstantiatePrefab(tooltipPrefab, scene) as GameObject;
        tooltip.name = "Building Construction Tooltip";
        tooltip.transform.SetParent(baseUi != null ? baseUi : panel.transform.parent, false);
        tooltip.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureBuilding(GameObject target, Definition definition, ResourceType wood,
        ResourceType stone, Sprite buttonSprite, Font font)
    {
        Button buildingButton = target.GetComponent<Button>();
        BaseBuildingConstruction construction = target.GetComponent<BaseBuildingConstruction>();
        if (construction == null) construction = target.AddComponent<BaseBuildingConstruction>();

        Transform previous = target.transform.Find("Build Button");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        GameObject overlay = CreateUiObject("Build Button", target.transform);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = new Vector2(8f, 7f);
        overlayRect.offsetMax = new Vector2(-8f, -7f);
        Image image = overlay.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        Button buildButton = overlay.AddComponent<Button>();
        buildButton.targetGraphic = image;
        buildButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = buildButton.colors;
        colors.normalColor = new Color(0.55f, 0.44f, 0.58f, 1f);
        colors.highlightedColor = new Color(0.86f, 0.72f, 0.92f, 1f);
        colors.pressedColor = new Color(0.42f, 0.32f, 0.46f, 1f);
        colors.disabledColor = new Color(0.25f, 0.23f, 0.27f, 0.75f);
        colors.colorMultiplier = 1f;
        buildButton.colors = colors;

        Text label = CreateText("Label", overlay.transform, font, 18, TextAnchor.MiddleCenter);
        label.text = "Строить";
        Stretch(label.rectTransform, 4f);

        SerializedObject serialized = new SerializedObject(construction);
        serialized.FindProperty("buildingId").stringValue = definition.id;
        serialized.FindProperty("nameKey").stringValue = definition.nameKey;
        serialized.FindProperty("fallbackName").stringValue = definition.ruName;
        serialized.FindProperty("descriptionKey").stringValue = definition.descriptionKey;
        serialized.FindProperty("fallbackDescription").stringValue = definition.ruDescription;
        serialized.FindProperty("buildingButton").objectReferenceValue = buildingButton;
        serialized.FindProperty("buildButton").objectReferenceValue = buildButton;
        serialized.FindProperty("buildButtonLabel").objectReferenceValue = label;
        serialized.FindProperty("wood").objectReferenceValue = wood;
        serialized.FindProperty("woodCost").intValue = 1;
        serialized.FindProperty("stone").objectReferenceValue = stone;
        serialized.FindProperty("stoneCost").intValue = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateTooltipPrefab(ResourceType wood, ResourceType stone, Sprite panelSprite, Font font)
    {
        GameObject root = CreateUiObject("Building Construction Tooltip", null);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560f, 300f);
        rect.pivot = new Vector2(0f, 1f);
        Image background = root.AddComponent<Image>();
        background.sprite = panelSprite;
        background.type = Image.Type.Sliced;
        background.color = Color.white;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        Text title = CreateText("Title", root.transform, font, 25, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(24f, -22f), new Vector2(512f, 54f), new Vector2(0f, 1f));
        title.fontStyle = FontStyle.Bold;
        Text description = CreateText("Description", root.transform, font, 18, TextAnchor.UpperLeft);
        SetRect(description.rectTransform, new Vector2(34f, -82f), new Vector2(492f, 98f), new Vector2(0f, 1f));
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        description.verticalOverflow = VerticalWrapMode.Overflow;
        Text price = CreateText("Price", root.transform, font, 19, TextAnchor.MiddleLeft);
        SetRect(price.rectTransform, new Vector2(34f, -190f), new Vector2(492f, 32f), new Vector2(0f, 1f));

        GameObject woodIconObject = CreateUiObject("Wood Icon", root.transform);
        Image woodIcon = woodIconObject.AddComponent<Image>();
        ResourceIconSizing.Apply(woodIcon, wood.resourceIcon);
        SetRect(woodIcon.rectTransform, new Vector2(56f, -234f), woodIcon.rectTransform.sizeDelta, new Vector2(0f, 1f));
        Text woodAmount = CreateText("Wood Amount", root.transform, font, 22, TextAnchor.MiddleLeft);
        SetRect(woodAmount.rectTransform, new Vector2(122f, -242f), new Vector2(60f, 48f), new Vector2(0f, 1f));
        woodAmount.text = "1";

        GameObject stoneIconObject = CreateUiObject("Stone Icon", root.transform);
        Image stoneIcon = stoneIconObject.AddComponent<Image>();
        ResourceIconSizing.Apply(stoneIcon, stone.resourceIcon);
        SetRect(stoneIcon.rectTransform, new Vector2(250f, -234f), stoneIcon.rectTransform.sizeDelta, new Vector2(0f, 1f));
        Text stoneAmount = CreateText("Stone Amount", root.transform, font, 22, TextAnchor.MiddleLeft);
        SetRect(stoneAmount.rectTransform, new Vector2(316f, -242f), new Vector2(60f, 48f), new Vector2(0f, 1f));
        stoneAmount.text = "1";

        BuildingConstructionTooltip tooltip = root.AddComponent<BuildingConstructionTooltip>();
        SerializedObject serialized = new SerializedObject(tooltip);
        serialized.FindProperty("title").objectReferenceValue = title;
        serialized.FindProperty("description").objectReferenceValue = description;
        serialized.FindProperty("priceLabel").objectReferenceValue = price;
        serialized.FindProperty("woodIcon").objectReferenceValue = woodIcon;
        serialized.FindProperty("woodAmount").objectReferenceValue = woodAmount;
        serialized.FindProperty("stoneIcon").objectReferenceValue = stoneIcon;
        serialized.FindProperty("stoneAmount").objectReferenceValue = stoneAmount;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TooltipPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void SetupLocalization()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prefabs/Base/Base Localization.asset");
        SetEntry(table, "base.building.build", "Строить", "Build");
        SetEntry(table, "base.building.price", "Цена постройки", "Construction cost");
        foreach (Definition definition in Definitions)
        {
            SetEntry(table, definition.nameKey, definition.ruName, definition.enName);
            SetEntry(table, definition.descriptionKey, definition.ruDescription, definition.enDescription);
        }
        EditorUtility.SetDirty(table);
    }

    private static void SetEntry(LocalizationTable table, string key, string ru, string en)
    {
        LocalizationTable.Entry entry = table.entries.Find(value => value.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new List<string> { ru, en };
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform));
        if (parent != null) value.transform.SetParent(parent, false);
        return value;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        GameObject value = CreateUiObject(name, parent);
        Text text = value.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.91f, 0.82f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static T FindAsset<T>(string name) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:" + typeof(T).Name);
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite != null && sprite.name == name) return sprite;
        }
        return null;
    }
}
