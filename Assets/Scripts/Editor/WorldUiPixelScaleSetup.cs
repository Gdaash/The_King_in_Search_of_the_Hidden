#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class WorldUiPixelScaleSetup
{
    const float PixelSize = 2f;
    static readonly HashSet<string> textures = new();
    static void RepairPosition(RectTransform rect)
    {
        var position = rect.anchoredPosition;
        if (float.IsNaN(position.x) || float.IsNaN(position.y)) rect.anchoredPosition = Vector2.zero;
    }
    static int Normalize(Image image, bool scene)
    {
        if (image.sprite == null) return 0;
        var canvas = image.canvas;
        if (scene && (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace)) return 0;
        string path = AssetDatabase.GetAssetPath(image.sprite);
        if (!path.StartsWith("Assets/")) return 0;
        textures.Add(path);
        // Prefab-stage Canvases can have a zero world scale before their first layout.
        // Multiply local scales instead of dividing two zero lossy scales.
        float relativeScale = 1f;
        Transform boundary = canvas != null ? canvas.rootCanvas.transform : image.transform.root;
        for (Transform t = image.transform; t != null && t != boundary; t = t.parent)
            relativeScale *= Mathf.Abs(t.localScale.x);
        if (relativeScale < .001f) return 0;
        if (image.type == Image.Type.Sliced || image.type == Image.Type.Tiled)
        {
            float reference = canvas != null ? canvas.referencePixelsPerUnit : 100f;
            float multiplier = reference / image.sprite.pixelsPerUnit * relativeScale / PixelSize;
            if (Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier)) return 0;
            image.pixelsPerUnitMultiplier = multiplier;
        }
        else if (image.type == Image.Type.Simple)
        {
            // The resource strip's Image is an obsolete backing image; the cells own its art.
            if (image.name == "ResourcesUI") { image.sprite = null; image.color = Color.clear; return 1; }
            Vector2 size = image.sprite.rect.size * (PixelSize / relativeScale);
            if ((image.rectTransform.rect.size - size).sqrMagnitude < .001f) return 0;
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
        else return 0; // Filled bars retain their authored 2x geometry and fill amount.
        EditorUtility.SetDirty(image);
        if (scene && PrefabUtility.IsPartOfPrefabInstance(image))
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        return 1;
    }

    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "World" || EditorApplication.isPlaying) throw new System.InvalidOperationException("Open World in Edit Mode.");
        textures.Clear();
        var paths = new HashSet<string>(AssetDatabase.GetDependencies(scene.path)
            .Where(p => p.EndsWith(".prefab") && (p.Contains("/UI/") || p.Contains("/UI Kit/"))));
        paths.Add("Assets/Prefabs/UI/HUD/Game Speed Controls.prefab");
        int changed = 0;
        foreach (string path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) RepairPosition(rect);
                int edits = 0;
                foreach (var image in root.GetComponentsInChildren<Image>(true)) edits += Normalize(image, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed += edits;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image.canvas != null && image.canvas.rootCanvas.renderMode != RenderMode.WorldSpace) RepairPosition(image.rectTransform);
            changed += Normalize(image, true);
        }
        int imports = 0;
        foreach (string path in textures)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); imports++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"World UI pixel scale: {changed} image adjustments, {imports} sharp sprite imports; source pixel = {PixelSize} Canvas units.");
    }
}
#endif
