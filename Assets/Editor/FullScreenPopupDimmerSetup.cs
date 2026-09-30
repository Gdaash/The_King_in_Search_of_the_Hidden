#if UNITY_EDITOR
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;

public static class FullScreenPopupDimmerSetup
{
    private const string DimmerPath = "Assets/Prefabs/UI/Full Screen Popup Dimmer.prefab";
    private static readonly (string prefab, string[] popups)[] Targets =
    {
        ("Assets/Prefabs/UI/Screens/Base Screen HUD.prefab", new[] { "Global Map", "Laboratory Popup", "Settings Popup", "Warehouse Popup", "Housing Popup", "Refugees Popup", "Square Popup", "Day Report Popup", "Next Day Confirmation", "Fort Popup", "Archery Range Popup", "Blacksmith Popup", "Castle Popup" }),
        ("Assets/Prefabs/UI/Screens/World Screen HUD.prefab", new[] { "RunStatistics", "Settings Popup" }),
        ("Assets/Prefabs/UI/Screens/Main Menu Screen.prefab", new[] { "Save Slots Popup", "Settings Popup" })
    };

    [MenuItem("Tools/Game Foundation/Setup Full Screen Popup Dimmers")]
    public static void Run()
    {
        GameObject dimmerPrefab = CreateDimmerPrefab();
        foreach ((string prefab, string[] popups) in Targets)
            SetupPrefab(prefab, popups, dimmerPrefab);
        SetupStandalone("Assets/Prefabs/UI/HUD/Escape Statistics Popup.prefab", dimmerPrefab);
        SetupStandalone("Assets/Prefabs/UI/SettingsPopup.prefab", dimmerPrefab);
        SetupCheatPopup(dimmerPrefab);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateDimmerPrefab()
    {
        GameObject root = new GameObject("Full Screen Popup Dimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-4096f, -4096f);
        rect.offsetMax = new Vector2(4096f, 4096f);
        UnityEngine.UI.Image image = root.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, .74f);
        image.raycastTarget = true;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DimmerPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void SetupPrefab(string path, string[] popupNames, GameObject dimmerPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (string name in popupNames)
            {
                Transform popup = FindDeep(root.transform, name);
                if (popup == null) throw new System.InvalidOperationException("Popup not found: " + name + " in " + path);
                SetupPopup(popup, dimmerPrefab);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetupPopup(Transform popup, GameObject dimmerPrefab)
    {
        Transform parent = popup.parent;
        Transform old = parent.Find(popup.name + " Dimmer");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        PopupDimmerLink existing = popup.GetComponent<PopupDimmerLink>();
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject dimmer = (GameObject)PrefabUtility.InstantiatePrefab(dimmerPrefab, parent);
        dimmer.name = popup.name + " Dimmer";
        dimmer.transform.SetSiblingIndex(popup.GetSiblingIndex());
        dimmer.SetActive(popup.gameObject.activeSelf);

        // Popup roots used to act as partial dimmers. Keep their layout and hit area, but make
        // the graphic transparent: the shared sibling now covers the complete display.
        UnityEngine.UI.Image rootImage = popup.GetComponent<UnityEngine.UI.Image>();
        if (rootImage != null)
        {
            Color color = rootImage.color;
            color.a = 0f;
            rootImage.color = color;
            rootImage.raycastTarget = false;
        }

        PopupDimmerLink link = popup.gameObject.AddComponent<PopupDimmerLink>();
        SerializedObject serialized = new SerializedObject(link);
        serialized.FindProperty("dimmer").objectReferenceValue = dimmer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupStandalone(string path, GameObject dimmerPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform old = root.transform.Find("Screen Dimmer");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject dimmer = (GameObject)PrefabUtility.InstantiatePrefab(dimmerPrefab, root.transform);
            dimmer.name = "Screen Dimmer";
            dimmer.transform.SetAsFirstSibling();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetupCheatPopup(GameObject dimmerPrefab)
    {
        const string path = "Assets/Prefabs/UI/CheatResourcePopup.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform old = root.transform.Find("Screen Dimmer");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject dimmer = (GameObject)PrefabUtility.InstantiatePrefab(dimmerPrefab, root.transform);
            dimmer.name = "Screen Dimmer";
            dimmer.transform.SetAsFirstSibling();
            dimmer.SetActive(false);
            CheatResourcePopup popup = root.GetComponent<CheatResourcePopup>();
            if (popup == null) throw new System.InvalidOperationException("CheatResourcePopup component is missing.");
            SerializedObject serialized = new SerializedObject(popup);
            serialized.FindProperty("dimmer").objectReferenceValue = dimmer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
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
}
#endif
