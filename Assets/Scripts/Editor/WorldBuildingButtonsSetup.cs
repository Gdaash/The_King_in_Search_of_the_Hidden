#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WorldBuildingButtonsSetup
{
    private class Mapping
    {
        public string button; public string[] layers; public Vector2 point;
        public Mapping(string button, float x, float y, params string[] layers) { this.button = button; this.layers = layers; point = new Vector2(x, y); }
    }
    private static readonly Mapping[] mappings = {
        new("Castle", 315, 110, "Castle"), new("Portal", 324, 225, "Portal"),
        new("Warehouse", 444, 307, "Warehouse", "Warehouse Barrels and Crates"),
        new("Housing", 150, 298, "House 1", "House 2", "House 3", "House 4", "House 5", "House 6", "House 7"),
        new("Square", 220, 293, "Market"), new("Refugees", 307, 430, "Refugee Camp"),
        new("Laboratory", 161, 120, "Laboratory"), new("Magic Library", 202, 92, "Magic Library", "Magic Library Glow"),
        new("Blacksmith", 185, 193, "Blacksmith"), new("Fort", 477, 100, "Fort"),
        new("Archery Range", 477, 200, "Archery Range", "Archery Range Annex")
    };
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        const string iconPath = "Assets/Sprites/UI/Icons/Construction Icon.png";
        if (!System.IO.File.Exists(iconPath)) throw new System.IO.FileNotFoundException("Construction icon is missing", iconPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.SaveAndReimport();
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        // Pixel alpha picking prevents transparent areas of overlapping buildings from swallowing clicks.
        var document = BasePsdSceneBuilder.Read();
        var names = new HashSet<string>(mappings.SelectMany(m => m.layers));
        foreach (var layer in document.layers.Where(l => names.Contains(l.name)))
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(layer.assetPath);
            texture.isReadable = true; texture.SaveAndReimport();
        }
        const string path = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var mapping in mappings)
            {
                var button = root.GetComponentsInChildren<Button>(true).Single(b => b.name == mapping.button && b.transform.parent.name == "Base Panel");
                var image = button.GetComponent<Image>(); image.enabled = true; image.sprite = null; image.color = Color.clear;
                button.transition = Selectable.Transition.None;
                var feedback = button.GetComponent<UnifiedButtonFeedback>(); if (feedback != null) feedback.enabled = false;
                var outline = button.GetComponent<Outline>(); if (outline != null) outline.enabled = false;
                var label = button.transform.Find("Label"); if (label != null) { label.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(label.gameObject); }
                var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.localScale = Vector3.one;
                var construction = button.GetComponent<BaseBuildingConstruction>();
                var build = button.transform.Find("Build Button") as RectTransform;
                if (build != null)
                {
                    build.anchorMin = build.anchorMax = build.pivot = new Vector2(.5f,.5f); build.sizeDelta = icon.rect.size * 2;
                    build.anchoredPosition = Vector2.zero; build.localScale = Vector3.one;
                    var buildImage = build.GetComponent<Image>(); buildImage.sprite = icon; buildImage.type = Image.Type.Simple; buildImage.color = Color.white;
                    var buildLabel = build.Find("Label"); if (buildLabel != null) { buildLabel.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(buildLabel.gameObject); }
                    // Keep the original icon colors, with a visible hover response.
                    var f = build.GetComponent<UnifiedButtonFeedback>(); if (f != null) f.enabled = false;
                    var b = build.GetComponent<Button>(); b.transition = Selectable.Transition.ColorTint;
                    var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1,.85f,.45f); colors.pressedColor = new Color(.8f,.65f,.3f); colors.disabledColor = new Color(.55f,.55f,.55f); b.colors = colors;
                    Record(build, buildImage, b, f);
                }
                var upgrade = button.GetComponentInChildren<BuildingUpgradeButton>(true);
                var upgradeRect = upgrade != null ? upgrade.GetComponent<RectTransform>() : null;
                if (upgradeRect != null) { upgradeRect.anchorMin = upgradeRect.anchorMax = new Vector2(.5f,.5f); upgradeRect.pivot = new Vector2(.5f,1); PrefabUtility.RecordPrefabInstancePropertyModifications(upgradeRect); }
                var action = button.GetComponent<BuildingActionAvailabilityIndicator>();
                var actionObject = action != null ? new SerializedObject(action).FindProperty("marker").objectReferenceValue as GameObject : null;
                var view = button.GetComponent<WorldBuildingButton>() ?? button.gameObject.AddComponent<WorldBuildingButton>();
                var s = new SerializedObject(view);
                var layers = s.FindProperty("artworkLayers"); layers.arraySize = mapping.layers.Length;
                for (int i=0;i<mapping.layers.Length;i++) layers.GetArrayElementAtIndex(i).stringValue = mapping.layers[i];
                s.FindProperty("constructionPixelPosition").vector2Value = mapping.point;
                s.FindProperty("construction").objectReferenceValue = construction;
                s.FindProperty("constructionButton").objectReferenceValue = build;
                s.FindProperty("upgradeButton").objectReferenceValue = upgradeRect;
                s.FindProperty("actionMarker").objectReferenceValue = actionObject != null ? actionObject.GetComponent<RectTransform>() : null;
                s.ApplyModifiedPropertiesWithoutUndo();
                Record(rect, button, image, feedback, outline, view);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        foreach (var view in Object.FindObjectsByType<WorldBuildingButton>(FindObjectsInactive.Include,FindObjectsSortMode.None)) { view.Resolve(); view.UpdatePosition(); }
        Debug.Log("Mapped 11 building controls to PSD sprites; existing callbacks and tooltips preserved.");
    }
    private static void Record(params Object[] objects) { foreach (var obj in objects) if (obj != null) PrefabUtility.RecordPrefabInstancePropertyModifications(obj); }
}
#endif
