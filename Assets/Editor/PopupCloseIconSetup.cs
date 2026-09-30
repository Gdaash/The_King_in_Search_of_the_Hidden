#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GameFoundation.UI;

public static class PopupCloseIconSetup
{
    private const string IconPath = "Assets/Prefabs/UI/Popup Close Icon.prefab";
    private static readonly string[] Paths =
    {
        "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab",
        "Assets/Prefabs/UI/Screens/World Screen HUD.prefab",
        "Assets/Prefabs/UI/Screens/Main Menu Screen.prefab",
        "Assets/Prefabs/UI/CheatResourcePopup.prefab",
        "Assets/Prefabs/UI/SettingsPopup.prefab"
    };

    [MenuItem("Tools/Game Foundation/Setup Popup Close Icons")]
    public static void Run()
    {
        GameObject iconPrefab = CreateIconPrefab();
        foreach (string path in Paths) ApplyToPrefab(path, iconPrefab);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateIconPrefab()
    {
        GameObject sourceRoot = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Screens/Base Screen HUD.prefab");
        try
        {
            Transform source = FindDeep(sourceRoot.transform, "Settings Popup")?.Find("Close");
            UnityEngine.UI.Image sourceImage = source != null ? source.GetComponent<UnityEngine.UI.Image>() : null;
            if (sourceImage == null || sourceImage.sprite == null)
                throw new System.InvalidOperationException("Reference popup close icon is missing.");

            GameObject icon = new GameObject("Popup Close Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            UnityEngine.UI.Image image = icon.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sourceImage.sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            // This is the graphic hit by the Canvas raycaster for every close button.
            image.raycastTarget = true;
            icon.GetComponent<RectTransform>().sizeDelta = sourceImage.rectTransform.sizeDelta;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(icon, IconPath);
            Object.DestroyImmediate(icon);
            return prefab;
        }
        finally { PrefabUtility.UnloadPrefabContents(sourceRoot); }
    }

    private static void ApplyToPrefab(string path, GameObject iconPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (!button.name.ToLowerInvariant().Contains("close")) continue;
                Transform old = button.transform.Find("Popup Close Icon");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                UnityEngine.UI.Image original = button.GetComponent<UnityEngine.UI.Image>();
                if (original != null)
                {
                    Color color = original.color;
                    color.a = 0f;
                    original.color = color;
                    original.raycastTarget = false;
                }
                GameObject icon = (GameObject)PrefabUtility.InstantiatePrefab(iconPrefab, button.transform);
                icon.name = "Popup Close Icon";
                RectTransform rect = icon.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                UnityEngine.UI.Image iconImage = icon.GetComponent<UnityEngine.UI.Image>();
                iconImage.raycastTarget = true;
                button.targetGraphic = iconImage;
                UnifiedButtonFeedback feedback = button.GetComponent<UnifiedButtonFeedback>();
                if (feedback == null) feedback = button.gameObject.AddComponent<UnifiedButtonFeedback>();
                SerializedObject feedbackProperties = new SerializedObject(feedback);
                feedbackProperties.FindProperty("hoverScaleMultiplier").floatValue = 1.3f;
                feedbackProperties.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
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
