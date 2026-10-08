using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProductionFragmentsSetup
{
    [MenuItem("Tools/Production/Configure building fragments")]
    public static void Configure()
    {
        const string folder = "Assets/Sprites/Effects/Production/";
        foreach (var file in Directory.GetFiles(folder, "*.png"))
        {
            AssetDatabase.ImportAsset(file);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        int count = 0;
        foreach (string name in new[] { "Forest 1", "Forest 2", "Forest 3", "Stone 1", "Stone 2", "Stone 3", "Iron Ore Mine", "Magic Ore Mine",
                     "Animals/Chickens 1", "Animals/Chickens 2", "Animals/Chickens 3", "Animals/Boars 1", "Animals/Boars 2", "Animals/Boars 3" })
        {
            string path = "Assets/Prefabs/Buildings/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var timer in root.GetComponentsInChildren<TimerController>(true))
                {
                    ResourceRequester owner = null;
                    for (int i = 0; i < timer.OnTimerEnd.GetPersistentEventCount(); i++)
                        if (timer.OnTimerEnd.GetPersistentMethodName(i) == nameof(ResourceRequester.FinishProcessing))
                            owner = timer.OnTimerEnd.GetPersistentTarget(i) as ResourceRequester;
                    if (owner == null) continue;
                    bool animal = name.StartsWith("Animals/");
                    string set = name.StartsWith("Animals/Chickens") ? "Feather" : name.StartsWith("Animals/Boars") ? "Hide" :
                        name.StartsWith("Forest") ? "Wood" : name.StartsWith("Stone") ? "Stone" :
                        owner.transform.parent == root.transform && owner.GetComponent<SpriteRenderer>() == null ? "ConstructionWood" :
                        name.StartsWith("Iron") ? "Iron" : "MagicOre";
                    var effect = timer.GetComponent<ProductionFragments>();
                    if (effect == null) effect = timer.gameObject.AddComponent<ProductionFragments>();
                    var so = new SerializedObject(effect);
                    so.FindProperty("timer").objectReferenceValue = timer;
                    var sprites = so.FindProperty("fragments"); sprites.arraySize = 4;
                    for (int i = 0; i < 4; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(folder + set + (i + 1) + ".png");
                    var dust = so.FindProperty("dustSprites"); dust.arraySize = 3;
                    for (int i = 0; i < 3; i++) dust.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(folder + "Dust" + (i + 1) + ".png");
                    var renderers = new List<SpriteRenderer>();
                    foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                        if (sr.sprite != null && (animal ? sr.GetComponentInParent<HexAnimalWander>() != null :
                            AssetDatabase.GetAssetPath(sr.sprite).StartsWith("Assets/Sprites/World/Buildings/"))) renderers.Add(sr);
                    var surfaces = so.FindProperty("surfaces"); surfaces.arraySize = renderers.Count;
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        var surface = surfaces.GetArrayElementAtIndex(i);
                        var sr = renderers[i];
                        surface.FindPropertyRelative("renderer").objectReferenceValue = sr;
                        var points = OpaquePoints(sr.sprite);
                        var positions = surface.FindPropertyRelative("points"); positions.arraySize = points.Count;
                        for (int j = 0; j < points.Count; j++) positions.GetArrayElementAtIndex(j).vector2Value = points[j];
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log(path + " / " + owner.name + " -> " + set);
                    count++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Configured production particle emitters: " + count);
    }

    private static List<Vector2> OpaquePoints(Sprite sprite)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            var points = new List<Vector2>();
            var rect = sprite.rect;
            // Store solid source pixels in renderer-local space, excluding the lowest ground strip.
            for (int y = Mathf.RoundToInt(rect.height * 0.22f); y < rect.height; y += 3)
                for (int x = 0; x < rect.width; x += 3)
                    if (texture.GetPixel((int)rect.x + x, (int)rect.y + y).a > 0.9f)
                        points.Add((new Vector2(x + 0.5f, y + 0.5f) - sprite.pivot) / sprite.pixelsPerUnit);
            return points;
        }
        finally { Object.DestroyImmediate(texture); }
    }
}
