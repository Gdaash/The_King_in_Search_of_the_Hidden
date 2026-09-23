using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class PixellariFontPilotSetup
{
    private const string FontPath = "Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf";
    private const string TmpPath = "Assets/Fonts/Pixellari Cyrillic/Pixellari Cyrillic UI.asset";
    private const string FallbackFontPath = "Assets/Fonts/Pixel UI/Unifont-17.0.04.otf";
    private const string FallbackTmpPath = "Assets/Fonts/Pixel UI/Unifont Last Resort.asset";
    private const string BasePanelPath = "Assets/Prefabs/Base/Base Panel.prefab";
    private const string BaseScenePath = "Assets/Scenes/Base.unity";

    [MenuItem("Tools/Game Foundation/Fonts/Apply Pixellari Pilot To Base")]
    public static void Run()
    {
        ConfigureImporter();
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset fallback = CreateOrReplaceFallbackAsset();
        TMP_FontAsset tmp = CreateOrReplaceTmpAsset(font, fallback);
        ApplyToBasePanelPrefab(font, tmp);
        ApplyToVisibleBaseScene(font, tmp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Game Foundation/Fonts/Apply Pixellari To Entire Game")]
    public static void ApplyToEntireGame()
    {
        ConfigureImporter();
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset fallback = CreateOrReplaceFallbackAsset();
        TMP_FontAsset tmp = CreateOrReplaceTmpAsset(font, fallback);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Apply(root, font, tmp, false);
            if (path == "Assets/Prefabs/UI/ResourcesUI.prefab") ConfigureResourceCounts(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path);
            foreach (GameObject root in scene.GetRootGameObjects()) Apply(root, font, tmp, false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.OpenScene(BaseScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureResourceCounts(GameObject root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "Count") continue;
            text.fontSize = 32f;
            text.enableAutoSizing = false;
            text.characterSpacing = 1f;
            text.alignment = TextAlignmentOptions.Center;
            RectTransform rect = text.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Round(size.x * 0.5f) * 2f;
            size.y = Mathf.Round(size.y * 0.5f) * 2f;
            rect.sizeDelta = size;
            Vector2 position = rect.anchoredPosition;
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);
            rect.anchoredPosition = position;
            EditorUtility.SetDirty(text);
        }
    }

    private static void ConfigureImporter()
    {
        TrueTypeFontImporter importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
        if (importer == null) return;
        importer.fontSize = 16;
        importer.fontRenderingMode = FontRenderingMode.HintedRaster;
        importer.includeFontData = true;
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset CreateOrReplaceTmpAsset(Font font, TMP_FontAsset fallback)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpPath);
        if (existing != null) AssetDatabase.DeleteAsset(TmpPath);

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 32, 2,
            GlyphRenderMode.RASTER_HINTED, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        asset.name = "Pixellari Cyrillic UI";
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(asset, TmpPath);
        if (asset.material != null && !AssetDatabase.Contains(asset.material))
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D texture in asset.atlasTextures)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, asset);
        }

        asset.TryAddCharacters(BuildCharacterSet(), out _);
        if (asset.fallbackFontAssetTable == null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (fallback != null && !asset.fallbackFontAssetTable.Contains(fallback))
            asset.fallbackFontAssetTable.Add(fallback);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static TMP_FontAsset CreateOrReplaceFallbackAsset()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FallbackFontPath);
        if (font == null) return null;

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackTmpPath);
        if (existing != null) AssetDatabase.DeleteAsset(FallbackTmpPath);

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 32, 2,
            GlyphRenderMode.RASTER_HINTED, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        asset.name = "Unifont Last Resort";
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(asset, FallbackTmpPath);
        if (asset.material != null && !AssetDatabase.Contains(asset.material))
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D texture in asset.atlasTextures)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, asset);
        }

        asset.TryAddCharacters(BuildCharacterSet(), out _);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static string BuildCharacterSet()
    {
        StringBuilder value = new StringBuilder();
        AddRange(value, 0x20, 0x024F);
        AddRange(value, 0x0400, 0x052F);
        AddRange(value, 0x2000, 0x206F);
        value.Append("₽€£¥№");
        return value.ToString();
    }

    private static void AddRange(StringBuilder value, int first, int last)
    {
        for (int code = first; code <= last; code++) value.Append((char)code);
    }

    private static void ApplyToBasePanelPrefab(Font font, TMP_FontAsset tmp)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BasePanelPath);
        Apply(root, font, tmp, false);
        PrefabUtility.SaveAsPrefabAsset(root, BasePanelPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ApplyToVisibleBaseScene(Font font, TMP_FontAsset tmp)
    {
        var scene = EditorSceneManager.OpenScene(BaseScenePath);
        foreach (GameObject root in scene.GetRootGameObjects()) Apply(root, font, tmp, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Apply(GameObject root, Font font, TMP_FontAsset tmp, bool visibleOnly)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (visibleOnly && !text.gameObject.activeInHierarchy) continue;
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.lineSpacing = 1f;
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text))
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (visibleOnly && !text.gameObject.activeInHierarchy) continue;
            text.font = tmp;
            text.fontStyle = FontStyles.Normal;
            text.enableAutoSizing = false;
            text.characterSpacing = Mathf.Max(text.characterSpacing, 1f);
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text))
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }
}
