#if UNITY_EDITOR
using System.IO;
using GameFoundation.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildingActionAvailabilityIndicatorSetup
{
    private const string HudPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    private const string IconPath = "Assets/Sprites/UI/ActionAvailableIndicator.png";
    private const string PrefabPath = "Assets/Prefabs/UI/Action Available Indicator.prefab";
    private const string ExclamationPrefabPath = "Assets/Prefabs/UI/Action Available Exclamation.prefab";

    [MenuItem("Tools/Game Foundation/Setup Building Action Indicators")]
    public static void Run()
    {
        Sprite icon = CreateIcon();
        GameObject markerPrefab = CreateMarkerPrefab(icon);
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform baseUi = FindDeep(root.transform, "Base UI");
            Configure(baseUi, "Warehouse", "Warehouse Popup", markerPrefab,
                BuildingActionAvailabilityIndicator.ActionType.CartPurchase);
            Configure(baseUi, "Blacksmith", "Blacksmith Popup", markerPrefab,
                BuildingActionAvailabilityIndicator.ActionType.BlacksmithProduction);
            Configure(baseUi, "Fort", "Fort Popup", markerPrefab,
                BuildingActionAvailabilityIndicator.ActionType.MilitaryTraining);
            Configure(baseUi, "Archery Range", "Archery Range Popup", markerPrefab,
                BuildingActionAvailabilityIndicator.ActionType.MilitaryTraining);
            ConfigureConstructionBadges(baseUi, markerPrefab);
            ConfigureLaboratoryBadge(baseUi, markerPrefab);
            ConfigurePortalBadge(baseUi, markerPrefab);
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void Configure(Transform baseUi, string buttonName, string popupName, GameObject markerPrefab,
        BuildingActionAvailabilityIndicator.ActionType type)
    {
        Transform button = FindDeep(baseUi, buttonName);
        Transform popup = FindDeep(baseUi, popupName);
        if (button == null || popup == null) throw new System.InvalidOperationException("Missing building UI: " + buttonName);

        BuildingActionAvailabilityIndicator indicator = AddIndicator(button, markerPrefab);
        SerializedObject serialized = new SerializedObject(indicator);
        serialized.FindProperty("actionType").enumValueIndex = (int)type;
        serialized.FindProperty("construction").objectReferenceValue = button.GetComponent<BaseBuildingConstruction>();
        serialized.FindProperty("warehouse").objectReferenceValue = popup.GetComponent<WarehouseCartPurchaseView>();
        serialized.FindProperty("blacksmith").objectReferenceValue = popup.GetComponent<BlacksmithProductionView>();
        serialized.FindProperty("training").objectReferenceValue = popup.GetComponent<MilitaryTrainingView>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureConstructionBadges(Transform baseUi, GameObject markerPrefab)
    {
        foreach (BaseBuildingConstruction construction in baseUi.GetComponentsInChildren<BaseBuildingConstruction>(true))
        {
            Transform buildButton = construction.transform.Find("Build Button");
            if (buildButton == null) continue;
            BuildingActionAvailabilityIndicator indicator = AddIndicator(buildButton, markerPrefab);
            SerializedObject serialized = new SerializedObject(indicator);
            serialized.FindProperty("actionType").enumValueIndex = (int)BuildingActionAvailabilityIndicator.ActionType.BuildingConstruction;
            serialized.FindProperty("construction").objectReferenceValue = construction;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureLaboratoryBadge(Transform baseUi, GameObject markerPrefab)
    {
        Transform laboratory = FindDeep(baseUi, "Laboratory");
        Transform popup = FindDeep(baseUi, "Laboratory Popup");
        if (laboratory == null || popup == null) throw new System.InvalidOperationException("Missing laboratory UI.");
        BuildingActionAvailabilityIndicator indicator = AddIndicator(laboratory, markerPrefab);
        SerializedObject serialized = new SerializedObject(indicator);
        serialized.FindProperty("actionType").enumValueIndex = (int)BuildingActionAvailabilityIndicator.ActionType.LaboratoryUpgrade;
        serialized.FindProperty("construction").objectReferenceValue = laboratory.GetComponent<BaseBuildingConstruction>();
        SerializedProperty skills = serialized.FindProperty("laboratorySkills");
        SkillButton[] values = popup.GetComponentsInChildren<SkillButton>(true);
        skills.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) skills.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePortalBadge(Transform baseUi, GameObject markerPrefab)
    {
        Transform portal = FindDeep(baseUi, "Portal");
        if (portal == null) throw new System.InvalidOperationException("Missing portal UI.");
        BuildingActionAvailabilityIndicator indicator = AddIndicator(portal, markerPrefab);
        SerializedObject serialized = new SerializedObject(indicator);
        serialized.FindProperty("actionType").enumValueIndex = (int)BuildingActionAvailabilityIndicator.ActionType.PortalTravel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static BuildingActionAvailabilityIndicator AddIndicator(Transform button, GameObject markerPrefab)
    {
        Transform oldMarker = button.Find("Action Available Indicator");
        if (oldMarker != null) Object.DestroyImmediate(oldMarker.gameObject);
        BuildingActionAvailabilityIndicator oldIndicator = button.GetComponent<BuildingActionAvailabilityIndicator>();
        if (oldIndicator != null) Object.DestroyImmediate(oldIndicator);

        GameObject marker = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab, button);
        marker.name = "Action Available Indicator";
        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-4f, -4f);
        rect.sizeDelta = new Vector2(32f, 32f);

        BuildingActionAvailabilityIndicator indicator = button.gameObject.AddComponent<BuildingActionAvailabilityIndicator>();
        SerializedObject serialized = new SerializedObject(indicator);
        serialized.FindProperty("marker").objectReferenceValue = marker;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return indicator;
    }

    private static GameObject CreateMarkerPrefab(Sprite icon)
    {
        GameObject root = new GameObject("Action Available Indicator", typeof(RectTransform), typeof(Image));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

        // The circle is deliberately a separate background layer under the reusable exclamation prefab.
        Image image = root.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        GameObject exclamationPrefab = CreateExclamationPrefab();
        GameObject exclamation = (GameObject)PrefabUtility.InstantiatePrefab(exclamationPrefab, root.transform);
        exclamation.name = "Exclamation";
        RectTransform labelRect = exclamation.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateExclamationPrefab()
    {
        Font font = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include)?.font;
        GameObject exclamation = new GameObject("Action Available Exclamation", typeof(RectTransform), typeof(Text), typeof(Outline));
        Text label = exclamation.GetComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "!";
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        Outline outline = exclamation.GetComponent<Outline>();
        outline.effectColor = new Color(.18f, .05f, .06f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(exclamation, ExclamationPrefabPath);
        Object.DestroyImmediate(exclamation);
        return prefab;
    }

    private static Sprite CreateIcon()
    {
        const int size = 16;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new(7.5f, 7.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
            Color color = distance <= 6.5f ? new Color(.82f, .12f, .16f, 1f) : Color.clear;
            if (distance > 5.6f && distance <= 6.5f) color = new Color(.23f, .05f, .07f, 1f);
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, false);
        Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
        File.WriteAllBytes(IconPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
