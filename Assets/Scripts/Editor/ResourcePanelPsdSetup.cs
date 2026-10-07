#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ResourcePanelPsdSetup
{
    const string Art = "Assets/Sprites/UI/ResourcePanel/";
    const string Panel = "Assets/Prefabs/UI/ResourcesUI.prefab";
    static readonly string[] Names = { "Crown", "Berry", "Human", "Stone", "Wood", "Cart", "MagicOre", "Sword", "Bow", "IronOre" };

    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var t = (TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 32; t.filterMode = FilterMode.Point; t.mipmapEnabled = false;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.alphaIsTransparency = true; t.npotScale = TextureImporterNPOTScale.None;
            t.spriteBorder = path.Contains("Resource Cell") ? new Vector4(4, 4, 4, 4) : Vector4.zero;
            t.SaveAndReimport();
        }
        var replacements = new Dictionary<Sprite, Sprite>();
        foreach (string name in new[] { "Crown", "Cart", "Sword", "Bow", "IronOre" })
        {
            var resource = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/" + name + ".asset");
            var replacement = resource.resourceIcon;
            if (resource.resourceIcon != replacement) replacements[resource.resourceIcon] = replacement;
            resource.resourceIcon = replacement; EditorUtility.SetDirty(resource);
        }
        // Preserve old world/carry sprites; replace only UI Images that explicitly reference these resource icons.
        var guids = replacements.Keys.Select(s => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s))).ToArray();
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!guids.Any(g => File.ReadAllText(path).Contains(g))) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { if (ReplaceImages(root, replacements)) PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var panel = PrefabUtility.LoadPrefabContents(Panel);
        try
        {
            var root = panel.GetComponent<RectTransform>();
            Place(root, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(1226, 54));
            panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Resource Cell.png");
            float[] iconX = { 17.5f, 16, 17, 15, 14, 16, 17.5f, 15, 15, 14 };
            float[] iconY = { 13, 13, 11.5f, 13, 12.5f, 14.5f, 13, 13, 13, 13.5f };
            for (int i = 0; i < Names.Length; i++)
            {
                var cell = (RectTransform)root.Find(Names[i]);
                Place(cell, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42 + i * 114, 0), new Vector2(116, 54));
                var image = cell.GetComponent<UnityEngine.UI.Image>();
                image.sprite = background; image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.5625f; // 100 / 32 / 2: border pixels have the same 2x scale as icons.
                image.color = Color.white; image.material = null; image.raycastTarget = true;
                var icon = cell.Find("Icon").GetComponent<UnityEngine.UI.Image>();
                var resource = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/" + Names[i] + ".asset");
                ResourceIconSizing.Apply(icon, resource.resourceIcon);
                Place(icon.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(iconX[i] * 2, -iconY[i] * 2), icon.sprite.rect.size * 2);
                icon.color = Color.white; icon.raycastTarget = false;
                var count = cell.Find("Count").GetComponent<TextMeshProUGUI>();
                Place(count.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(86, -27), new Vector2(46, 32));
                count.fontSize = 16; count.enableAutoSizing = false; count.fontStyle = FontStyles.Normal;
                count.characterSpacing = 0; count.alignment = TextAlignmentOptions.Center;
                count.color = new Color32(255, 244, 202, 255); count.raycastTarget = false;
                count.textWrappingMode = TextWrappingModes.NoWrap; count.overflowMode = TextOverflowModes.Ellipsis;
            }
            Cap(root, "Left Cap", 0); Cap(root, "Right Cap", 1184);
            PrefabUtility.SaveAsPrefabAsset(panel, Panel);
        }
        finally { PrefabUtility.UnloadPrefabContents(panel); }
        NormalizeScreenPrefabs();
        AssetDatabase.SaveAssets();
        string originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        foreach (string scenePath in new[] { "Assets/Scenes/Base.unity", "Assets/Scenes/World.unity" })
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            foreach (var root in scene.GetRootGameObjects()) ReplaceImages(root, replacements);
            foreach (var ui in Object.FindObjectsByType<ResourceUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(ui) != Panel) continue;
                // Remove only stale visual overrides of the shared panel, preserving the scene's tooltip reference.
                foreach (var rect in ui.GetComponentsInChildren<RectTransform>(true))
                    if (PrefabUtility.IsPartOfPrefabInstance(rect)) PrefabUtility.RevertObjectOverride(rect, InteractionMode.AutomatedAction);
                foreach (var image in ui.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    if (PrefabUtility.IsPartOfPrefabInstance(image)) PrefabUtility.RevertObjectOverride(image, InteractionMode.AutomatedAction);
                foreach (var text in ui.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RevertObjectOverride(text, InteractionMode.AutomatedAction);
                SetPanelRoot(ui);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene(originalScene);
        Debug.Log("Shared PSD resource panel installed on Base and World; five resource icons updated.");
    }
    public static void NormalizeScreenPrefabs()
    {
        foreach (string path in new[] { "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab", "Assets/Prefabs/UI/Screens/World Screen HUD.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var ui in root.GetComponentsInChildren<ResourceUI>(true))
                {
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(ui) != Panel) continue;
                    foreach (var component in ui.GetComponentsInChildren<Component>(true))
                        if (component is RectTransform || component is UnityEngine.UI.Image || component is TextMeshProUGUI)
                            PrefabUtility.RevertObjectOverride(component, InteractionMode.AutomatedAction);
                    SetPanelRoot(ui);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }
    public static void SetPanelRoot(ResourceUI ui)
    {
        var rect = ui.GetComponent<RectTransform>();
        Place(rect, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(1226, 54));
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }
    static bool ReplaceImages(GameObject root, Dictionary<Sprite, Sprite> replacements)
    {
        bool changed = false;
        foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            if (image.sprite != null && replacements.TryGetValue(image.sprite, out var sprite))
            {
                ResourceIconSizing.Apply(image, sprite);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image.rectTransform);
                changed = true;
            }
        return changed;
    }
    static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
        rect.anchoredPosition = pos; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
    static void Cap(RectTransform root, string name, float x)
    {
        var existing = root.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(root, false);
        var image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
        image.raycastTarget = false; image.color = Color.white;
        Place(image.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, 2), image.sprite.rect.size * 2);
        go.transform.SetAsFirstSibling();
    }
}
#endif
