using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

public static class CastleFeatureSetup
{
    private const string HudPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    private const string LocalizationPath = "Assets/Resources/Localization/Base Localization.asset";

    [MenuItem("Tools/Game Foundation/Setup Castle")]
    public static void Run()
    {
        UpdateLocalization();
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform baseUi = FindDeep(root.transform, "Base UI");
            Transform panel = FindDeep(root.transform, "Base Panel");
            Transform source = panel != null ? panel.Find("Blacksmith") : null;
            Transform popupSource = FindDeep(root.transform, "Blacksmith Popup");
            ResourceType wood = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
            ResourceType stone = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Stone.asset");
            if (baseUi == null || source == null || popupSource == null || wood == null || stone == null)
                throw new System.InvalidOperationException("Castle setup source objects are missing.");

            RemoveExisting(baseUi, panel);
            Font font = source.GetComponentInChildren<Text>(true).font;
            GameObject castle = Object.Instantiate(source.gameObject, panel);
            castle.name = "Castle";
            ((RectTransform)castle.transform).anchoredPosition = new Vector2(0f, -325f);
            SetLocalized(castle.GetComponentInChildren<LocalizedText>(true).gameObject, "base.base_panel.castle.label");
            ConfigureConstruction(castle.GetComponent<BaseBuildingConstruction>(), castle.GetComponent<Button>(), wood, stone);

            GameObject popup = CreatePopup(baseUi, popupSource.GetComponent<UIImage>(), font);
            CastleBuildingController controller = baseUi.gameObject.AddComponent<CastleBuildingController>();
            SetObject(controller, "castleButton", castle.GetComponent<Button>());
            SetObject(controller, "popup", popup.GetComponent<RoyalDecreePopupView>());
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static void RemoveExisting(Transform baseUi, Transform panel)
    {
        Transform oldCastle = panel != null ? panel.Find("Castle") : null;
        if (oldCastle != null) Object.DestroyImmediate(oldCastle.gameObject);
        Transform oldPopup = baseUi.Find("Castle Popup");
        if (oldPopup != null) Object.DestroyImmediate(oldPopup.gameObject);
        CastleBuildingController oldController = baseUi.GetComponent<CastleBuildingController>();
        if (oldController != null) Object.DestroyImmediate(oldController);
    }

    private static void ConfigureConstruction(BaseBuildingConstruction construction, Button buildingButton, ResourceType wood, ResourceType stone)
    {
        if (construction == null) construction = buildingButton.gameObject.AddComponent<BaseBuildingConstruction>();
        SerializedObject serialized = new SerializedObject(construction);
        serialized.FindProperty("buildingId").stringValue = "castle";
        serialized.FindProperty("nameKey").stringValue = "base.base_panel.castle.label";
        serialized.FindProperty("fallbackName").stringValue = "Замок";
        serialized.FindProperty("descriptionKey").stringValue = "base.building.castle.description";
        serialized.FindProperty("fallbackDescription").stringValue = "Позволяет издавать указы, меняющие работу систем.";
        serialized.FindProperty("buildingButton").objectReferenceValue = buildingButton;
        serialized.FindProperty("wood").objectReferenceValue = wood;
        serialized.FindProperty("woodCost").intValue = 1;
        serialized.FindProperty("stone").objectReferenceValue = stone;
        serialized.FindProperty("stoneCost").intValue = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePopup(Transform parent, UIImage source, Font font)
    {
        RectTransform root = CreateRect("Castle Popup", parent, Vector2.zero, new Vector2(780f, 680f));
        UIImage background = root.gameObject.AddComponent<UIImage>();
        background.sprite = source.sprite;
        background.type = UIImage.Type.Sliced;
        background.color = source.color;
        Text title = CreateText("Title", root, new Vector2(0f, 285f), new Vector2(620f, 52f), font, 32);
        SetLocalized(title.gameObject, "base.castle_popup.title");

        Button cautiousButton = CreateDecreeCard(root, source, font, "Cautious Warriors Card", new Vector2(0f, 120f),
            "base.castle_popup.decree.cautious.name", "base.castle_popup.decree.cautious.description",
            out Text cautiousButtonLabel, out Text cautiousState);
        Button finishOffButton = CreateDecreeCard(root, source, font, "Finish Off Enemies Card", new Vector2(0f, -150f),
            "base.castle_popup.decree.finish_off.name", "base.castle_popup.decree.finish_off.description",
            out Text finishOffButtonLabel, out Text finishOffState);

        RectTransform close = CreateRect("Close", root, new Vector2(350f, 305f), new Vector2(46f, 46f));
        UIImage closeImage = close.gameObject.AddComponent<UIImage>();
        closeImage.sprite = source.sprite;
        closeImage.type = UIImage.Type.Sliced;
        closeImage.color = new Color(.58f, .14f, .2f);
        Button closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        Text closeLabel = CreateText("Label", close, Vector2.zero, new Vector2(42f, 42f), font, 26);
        closeLabel.text = "×";

        RoyalDecreePopupView view = root.gameObject.AddComponent<RoyalDecreePopupView>();
        SetObject(view, "closeButton", closeButton);
        SetObject(view, "cautiousWarriorsButton", cautiousButton);
        SetObject(view, "cautiousWarriorsButtonLabel", cautiousButtonLabel);
        SetObject(view, "cautiousWarriorsState", cautiousState);
        SetObject(view, "finishOffEnemiesButton", finishOffButton);
        SetObject(view, "finishOffEnemiesButtonLabel", finishOffButtonLabel);
        SetObject(view, "finishOffEnemiesState", finishOffState);
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static Button CreateDecreeCard(Transform parent, UIImage source, Font font, string cardName, Vector2 position,
        string nameKey, string descriptionKey, out Text buttonLabel, out Text state)
    {
        RectTransform card = CreateRect(cardName, parent, position, new Vector2(700f, 230f));
        UIImage cardImage = card.gameObject.AddComponent<UIImage>();
        cardImage.sprite = source.sprite;
        cardImage.type = UIImage.Type.Sliced;
        cardImage.color = new Color(.13f, .09f, .16f, .96f);

        Text decree = CreateText("Decree", card, new Vector2(0f, 88f), new Vector2(610f, 28f), font, 16);
        decree.text = "ПРИКАЗ";
        Text name = CreateText("Name", card, new Vector2(0f, 52f), new Vector2(640f, 36f), font, 24);
        SetLocalized(name.gameObject, nameKey);
        Text description = CreateText("Description", card, new Vector2(0f, 4f), new Vector2(630f, 58f), font, 18);
        SetLocalized(description.gameObject, descriptionKey);
        state = CreateText("State", card, new Vector2(-145f, -68f), new Vector2(250f, 34f), font, 18);
        state.color = new Color(.95f, .35f, .32f);

        RectTransform toggle = CreateRect("Toggle", card, new Vector2(175f, -68f), new Vector2(220f, 50f));
        UIImage toggleImage = toggle.gameObject.AddComponent<UIImage>();
        toggleImage.sprite = source.sprite;
        toggleImage.type = UIImage.Type.Sliced;
        toggleImage.color = new Color(.43f, .35f, .52f);
        Button button = toggle.gameObject.AddComponent<Button>();
        button.targetGraphic = toggleImage;
        buttonLabel = CreateText("Label", toggle, Vector2.zero, new Vector2(195f, 38f), font, 18);
        buttonLabel.text = "Включить";
        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, Font font, int fontSize)
    {
        Text text = CreateRect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(.94f, .91f, .82f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
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
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void UpdateLocalization()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationPath);
        Add(table, "base.base_panel.castle.label", "Замок", "Castle");
        Add(table, "base.building.castle.description", "Позволяет издавать указы, меняющие работу систем.", "Issues decrees that change how game systems work.");
        Add(table, "base.castle_popup.title", "ЗАМОК", "CASTLE");
        Add(table, "base.castle_popup.decree.cautious.name", "Не трус, а осторожный", "Not cowardly, but cautious");
        Add(table, "base.castle_popup.decree.cautious.description", "Если здоровье воина упало ниже 10%, то воин бежит с поля боя в портал.", "If a warrior's health falls below 10%, they flee the battlefield through the portal.");
        Add(table, "base.castle_popup.decree.finish_off.name", "Бей раненых", "Finish off the wounded");
        Add(table, "base.castle_popup.decree.finish_off.description", "Лучники атакуют врагов с наименьшим процентом здоровья", "Archers attack enemies with the lowest health percentage.");
        Add(table, "base.castle.decree.enabled", "Указ включён", "Decree enabled");
        Add(table, "base.castle.decree.disabled", "Указ выключен", "Decree disabled");
        Add(table, "base.castle.decree.turn_on", "Включить", "Enable");
        Add(table, "base.castle.decree.turn_off", "Выключить", "Disable");
        EditorUtility.SetDirty(table);
    }

    private static void Add(LocalizationTable table, string key, string ru, string en)
    {
        LocalizationTable.Entry entry = table.entries.Find(item => item.key == key);
        if (entry == null)
        {
            entry = new LocalizationTable.Entry { key = key };
            table.entries.Add(entry);
        }
        entry.values = new List<string> { ru, en };
    }
}
