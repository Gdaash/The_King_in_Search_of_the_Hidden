#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BasePsdSceneBuilder
{
    [Serializable] public class Layer { public string name, originalName, assetPath; public int left, top, width, height, order; public bool visible; }
    [Serializable] public class Document { public int width, height, pixelsPerUnit; public Layer[] layers; }
    private const string Folder = "Assets/Art/BaseScene";
    private const string PrefabPath = Folder + "/Base Scene Artwork.prefab";
    public static Document Read() => JsonUtility.FromJson<Document>(File.ReadAllText(Folder + "/BaseSceneLayers.json"));

    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Base.unity")
            throw new InvalidOperationException("Open the Base scene first.");
        var doc = Read();
        foreach (var layer in doc.layers)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(layer.assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = doc.pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false; // Preserve the PSD's original RGBA bytes.
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            settings.spritePivot = Vector2.zero;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        string materialPath = Folder + "/Base Pixel Art.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var root = new GameObject("Base Scene Artwork");
        // Photoshop's layer panel is top-to-bottom; sortingOrder is bottom-to-top.
        foreach (var layer in doc.layers.OrderByDescending(l => l.order))
        {
            var go = new GameObject(layer.name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3((layer.left - doc.width / 2f) / doc.pixelsPerUnit,
                (doc.height / 2f - layer.top - layer.height) / doc.pixelsPerUnit, 0);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(layer.assetPath);
            renderer.sortingOrder = layer.order;
            renderer.sharedMaterial = material;
            go.SetActive(layer.visible);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        var old = GameObject.Find("Base Scene Artwork");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place PSD base artwork");

        var camera = Camera.main;
        Undo.RecordObject(camera, "Frame PSD artwork");
        Undo.RecordObject(camera.transform, "Frame PSD artwork");
        camera.transform.position = new Vector3(0, 0, -10);
        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true; camera.orthographicSize = doc.height / (2f * doc.pixelsPerUnit);
        camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var data = camera.GetUniversalAdditionalCameraData();
        Undo.RecordObject(data, "Configure base pixel art renderer");
        data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
        ConfigureRenderer(data);
        var pp = camera.GetComponent<PixelPerfectCamera>() ?? Undo.AddComponent<PixelPerfectCamera>(camera.gameObject);
        Undo.RecordObject(pp, "Configure PSD pixel grid");
        pp.assetsPPU = doc.pixelsPerUnit; pp.refResolutionX = doc.width; pp.refResolutionY = doc.height;
        pp.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        pp.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
        var pixelSettings = new SerializedObject(pp);
        pixelSettings.FindProperty("m_FilterMode").enumValueIndex = (int)PixelPerfectCamera.PixelPerfectFilterMode.Point;
        pixelSettings.ApplyModifiedProperties();

        foreach (var image in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image.gameObject.scene != instance.scene) continue;
            if (image.name == "Castle Grounds" || image.name == "Base Panel")
            {
                Undo.RecordObject(image, "Replace legacy canvas artwork with PSD scene sprites");
                image.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }
        }
        EditorSceneManager.MarkSceneDirty(instance.scene);
        EditorSceneManager.SaveScene(instance.scene);
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static void ConfigureRenderer(UniversalAdditionalCameraData camera)
    {
        // A dedicated renderer keeps the reference artwork free of CRT filtering.
        const string path = Folder + "/Base Pixel Art Renderer.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(path);
        if (renderer == null)
        {
            renderer = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset"));
            renderer.name = "Base Pixel Art Renderer";
            renderer.rendererFeatures.Clear();
            AssetDatabase.CreateAsset(renderer, path);
        }
        var pipeline = new SerializedObject(GraphicsSettings.currentRenderPipeline);
        var list = pipeline.FindProperty("m_RendererDataList");
        int index = -1;
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
        if (index < 0)
        {
            index = list.arraySize; list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            pipeline.ApplyModifiedProperties();
        }
        camera.SetRenderer(index);
    }

    public static void Validate()
    {
        var doc = Read();
        var root = GameObject.Find("Base Scene Artwork");
        if (root == null || root.transform.childCount != doc.layers.Length) throw new Exception("Incorrect layer count.");
        foreach (var layer in doc.layers)
        {
            var child = root.transform.Find(layer.name);
            var renderer = child.GetComponent<SpriteRenderer>();
            Vector3 expected = new Vector3((layer.left - doc.width / 2f) / doc.pixelsPerUnit, (doc.height / 2f - layer.top - layer.height) / doc.pixelsPerUnit, 0);
            if (child.localPosition != expected || child.localScale != Vector3.one || renderer.sortingOrder != layer.order)
                throw new Exception("Incorrect PSD placement: " + layer.name);
            if (renderer.sprite.pixelsPerUnit != 32 || renderer.sprite.rect.size != new Vector2(layer.width, layer.height))
                throw new Exception("Incorrect sprite dimensions: " + layer.name);
            if (renderer.sprite.texture.filterMode != FilterMode.Point || child.GetComponentInParent<Canvas>() != null)
                throw new Exception("Incorrect rendering: " + layer.name);
        }
        Debug.Log("Base PSD validated: 37 original layers, exact pixel bounds and sorting order, 32 PPU, no Canvas.");
    }

    public static void Capture()
    {
        var camera = Camera.main;
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(640, 480, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1, filterMode = FilterMode.Point };
        var pixels = new Texture2D(640, 480, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            pixels.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); pixels.Apply();
            Directory.CreateDirectory("Temp/BaseSceneVerification");
            File.WriteAllBytes("Temp/BaseSceneVerification/Unity.png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous; RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(pixels); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
