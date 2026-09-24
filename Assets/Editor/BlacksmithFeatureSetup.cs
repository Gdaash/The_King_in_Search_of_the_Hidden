#if UNITY_EDITOR
using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BlacksmithFeatureSetup
{
    private const string ScenePath = "Assets/Scenes/Base.unity";
    private const string PopupPath = "Assets/Prefabs/Base/Blacksmith Popup.prefab";

    private struct RecipeUi
    {
        public Button button;
        public Image inputIcon;
        public Text inputAmount;
        public Image outputIcon;
        public Text outputAmount;
    }

    [MenuItem("Tools/Game Setup/Rebuild Blacksmith")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject ui = GameObject.Find("LevelManager/Canvas/Base UI");
        Transform panel = ui != null ? ui.transform.Find("Base Panel") : null;
        if (ui == null || panel == null) throw new System.InvalidOperationException("Base UI or Base Panel was not found.");

        ResourceType wood = LoadResource("Wood");
        ResourceType ironOre = LoadResource("IronOre");
        ResourceType sword = LoadResource("Sword");
        ResourceType bow = LoadResource("Bow");
        if (wood == null || ironOre == null || sword == null || bow == null)
            throw new System.InvalidOperationException("Blacksmith resources are missing.");

        GameObject building = CreateBuildingButton(panel);
        GameObject popup = CreatePopup(ui.transform, ironOre, sword, wood, bow);
        SetObject(ui.GetComponent<BaseUIController>(), "blacksmith", popup);

        AddLocalization();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        PrefabUtility.SaveAsPrefabAssetAndConnect(popup, PopupPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene);

        BaseBuildingConstructionSetup.Run();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = building;
    }

    private static GameObject CreateBuildingButton(Transform panel)
    {
        Transform old = panel.Find("Blacksmith");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject source = panel.Find("Warehouse").gameObject;
        GameObject button = Object.Instantiate(source, panel);
        button.name = "Blacksmith";
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -245f);
        button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null) SetLocalized(label.gameObject, "base.base_panel.blacksmith.label");
        return button;
    }

    private static GameObject CreatePopup(Transform parent, ResourceType ironOre, ResourceType sword,
        ResourceType wood, ResourceType bow)
    {
        Transform old = parent.Find("Blacksmith Popup");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject source = parent.Find("Warehouse Popup").gameObject;
        GameObject popup = Object.Instantiate(source, parent);
        popup.name = "Blacksmith Popup";
        popup.SetActive(true);

        WarehouseCartPurchaseView warehouse = popup.GetComponent<WarehouseCartPurchaseView>();
        if (warehouse != null) Object.DestroyImmediate(warehouse);
        GameObject standardButton = popup.transform.Find("Buy Cart").gameObject;
        DestroyChild(popup.transform, "Cart Stock");

        RectTransform frame = popup.transform.Find("Artwork Frame") as RectTransform;
        if (frame != null) frame.sizeDelta = new Vector2(760f, 520f);
        Transform title = popup.transform.Find("Title");
        SetLocalized(title.gameObject, "base.blacksmith_popup.title");
        ((RectTransform)title).anchoredPosition = new Vector2(0f, 210f);
        Button close = popup.transform.Find("Close").GetComponent<Button>();
        close.onClick = new Button.ButtonClickedEvent();
        ((RectTransform)close.transform).anchoredPosition = new Vector2(345f, 225f);

        RecipeUi swordUi = CreateRecipe(popup.transform, standardButton, "Sword Recipe", new Vector2(0f, 70f),
            "base.blacksmith_popup.sword", "base.blacksmith_popup.forge_sword", ironOre, sword);
        RecipeUi bowUi = CreateRecipe(popup.transform, standardButton, "Bow Recipe", new Vector2(0f, -95f),
            "base.blacksmith_popup.bow", "base.blacksmith_popup.make_bow", wood, bow);
        DestroyChild(popup.transform, "Buy Cart");

        BlacksmithProductionView view = popup.AddComponent<BlacksmithProductionView>();
        ConfigureRecipe(view, "swordRecipe", ironOre, sword, swordUi);
        ConfigureRecipe(view, "bowRecipe", wood, bow, bowUi);
        popup.SetActive(false);
        return popup;
    }

    private static RecipeUi CreateRecipe(Transform parent, GameObject sourceButton, string name, Vector2 position,
        string titleKey, string buttonKey, ResourceType input, ResourceType output)
    {
        GameObject row = NewRect(name, parent, position, new Vector2(650f, 145f));
        Text title = CreateText(row.transform, "Title", new Vector2(0f, 48f), new Vector2(610f, 36f), 22);
        SetLocalized(title.gameObject, titleKey);

        Image inputIcon = CreateIcon(row.transform, "Input Icon", new Vector2(-225f, -12f), input);
        Text inputAmount = CreateText(row.transform, "Input Amount", new Vector2(-165f, -12f), new Vector2(70f, 52f), 24);
        inputAmount.text = "1";
        Text arrow = CreateText(row.transform, "Arrow", new Vector2(-95f, -12f), new Vector2(55f, 52f), 28);
        arrow.text = "→";
        Image outputIcon = CreateIcon(row.transform, "Output Icon", new Vector2(-25f, -12f), output);
        Text outputAmount = CreateText(row.transform, "Output Amount", new Vector2(38f, -12f), new Vector2(70f, 52f), 24);
        outputAmount.text = "0";

        GameObject buttonObject = Object.Instantiate(sourceButton, row.transform);
        buttonObject.name = "Produce";
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(205f, -12f);
        buttonRect.sizeDelta = new Vector2(245f, 58f);
        DestroyChild(buttonObject.transform, "Price");
        Button button = buttonObject.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        Text buttonLabel = buttonObject.GetComponentInChildren<Text>(true);
        if (buttonLabel != null)
        {
            SetLocalized(buttonLabel.gameObject, buttonKey);
            buttonLabel.fontSize = 19;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform labelRect = buttonLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);
        }
        return new RecipeUi { button = button, inputIcon = inputIcon, inputAmount = inputAmount,
            outputIcon = outputIcon, outputAmount = outputAmount };
    }

    private static void ConfigureRecipe(BlacksmithProductionView view, string propertyName,
        ResourceType input, ResourceType output, RecipeUi controls)
    {
        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty recipe = serialized.FindProperty(propertyName);
        recipe.FindPropertyRelative("input").objectReferenceValue = input;
        recipe.FindPropertyRelative("inputAmount").intValue = 1;
        recipe.FindPropertyRelative("output").objectReferenceValue = output;
        recipe.FindPropertyRelative("outputAmount").intValue = 1;
        recipe.FindPropertyRelative("produceButton").objectReferenceValue = controls.button;
        recipe.FindPropertyRelative("inputIcon").objectReferenceValue = controls.inputIcon;
        recipe.FindPropertyRelative("inputAmountText").objectReferenceValue = controls.inputAmount;
        recipe.FindPropertyRelative("outputIcon").objectReferenceValue = controls.outputIcon;
        recipe.FindPropertyRelative("outputAmountText").objectReferenceValue = controls.outputAmount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreateIcon(Transform parent, string name, Vector2 position, ResourceType resource)
    {
        GameObject go = NewRect(name, parent, position, new Vector2(48f, 48f));
        Image image = go.AddComponent<Image>();
        ResourceIconSizing.Apply(image, resource.resourceIcon);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = NewRect(name, parent, position, size);
        Text text = go.AddComponent<Text>();
        Text sample = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        text.font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.94f, 0.91f, 0.82f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static GameObject NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static void AddLocalization()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prefabs/Base/Base Localization.asset");
        SetEntry(table, "base.base_panel.blacksmith.label", "Кузница", "Blacksmith");
        SetEntry(table, "base.blacksmith_popup.title", "КУЗНИЦА", "BLACKSMITH");
        SetEntry(table, "base.blacksmith_popup.sword", "Мечи", "Swords");
        SetEntry(table, "base.blacksmith_popup.bow", "Луки", "Bows");
        SetEntry(table, "base.blacksmith_popup.forge_sword", "Выковать меч", "Forge sword");
        SetEntry(table, "base.blacksmith_popup.make_bow", "Сделать лук", "Make bow");
        SetEntry(table, "base.building.blacksmith.description", "Куёт мечи из железной руды и изготавливает луки из дерева.", "Forges swords from iron ore and makes bows from wood.");
        EditorUtility.SetDirty(table);
    }

    private static void SetEntry(LocalizationTable table, string key, string russian, string english)
    {
        if (table == null) return;
        LocalizationTable.Entry entry = table.entries.Find(value => value.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new List<string> { russian, english };
    }

    private static void SetLocalized(GameObject target, string key)
    {
        LocalizedText localized = target.GetComponent<LocalizedText>() ?? target.AddComponent<LocalizedText>();
        SerializedObject serialized = new SerializedObject(localized);
        serialized.FindProperty("key").stringValue = key;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string field, Object value)
    {
        if (target == null) return;
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static ResourceType LoadResource(string name) =>
        AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/" + name + ".asset");

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
}
#endif
