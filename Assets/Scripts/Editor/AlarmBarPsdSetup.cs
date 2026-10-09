#if UNITY_EDITOR
using System;
using System.IO;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Installs the source PSD pixels and stores direct sprite references in the HUD prefabs.</summary>
public static class AlarmBarPsdSetup
{
    public const string Art = "Assets/Sprites/Ui/Alarm/";
    public const string Bar = "Assets/Prefabs/UI/HUD/Alarm Bar.prefab";
    const string Orb = "Assets/Prefabs/UI/AlarmOrb.prefab";

    [MenuItem("Tools/UI/Install PSD Danger Bar")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = Vector4.zero;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        Edit(Orb, root =>
        {
            var image = root.GetComponent<Image>();
            SetImage(image, Sprite("Alarm Skull"));
            image.rectTransform.sizeDelta = image.sprite.rect.size * 2;
        });
        Edit(Bar, root =>
        {
            var alarm = root.GetComponent<AlarmSystem>();
            Configure(alarm);
            SetRoot((RectTransform)root.transform);
            SetImage(root.GetComponent<Image>(), Sprite("Alarm Frame"));
            var config = new SerializedObject(alarm);
            foreach (string field in new[] { "previewImage", "fillImage" })
            {
                var image = (Image)config.FindProperty(field).objectReferenceValue;
                SetImage(image, Sprite("Alarm Fill"));
                Place(image.rectTransform, new Vector2(1, 5), image.sprite.rect.size * 2);
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = 0;
                image.fillAmount = 0;
                image.color = new Color(1, 1, 1, field == "previewImage" ? .4f : 1f);
            }
            var ticks = (RectTransform)config.FindProperty("tickContainer").objectReferenceValue;
            Place(ticks, Vector2.zero, new Vector2(360, 50));
            var existing = root.transform.Find("Start Divider");
            var start = existing != null ? existing.GetComponent<Image>() :
                new GameObject("Start Divider", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            start.transform.SetParent(root.transform, false);
            SetImage(start, Sprite("Start Divider"));
            Place(start.rectTransform, new Vector2(-180, 4), start.sprite.rect.size * 2);
            ticks.SetAsLastSibling();
            alarm.RefreshThresholdMarkers();
        });
        Edit("Assets/Prefabs/Managers/AlarmSystem.prefab", root => Configure(root.GetComponent<AlarmSystem>()));
        Edit("Assets/Prefabs/UI/World/Crystal Hex Hover.prefab", HexAlarmPreviewSetup.Configure);
        Edit("Assets/Prefabs/UI/Screens/World Screen HUD.prefab", root => Normalize(root));
        Edit("Assets/Prefabs/UI/HUD/Danger Level Notification.prefab", root =>
        {
            var view = new SerializedObject(root.GetComponent<DangerLevelNotificationView>());
            view.FindProperty("activeSkullSprite").objectReferenceValue = Sprite("Threshold Skull Active");
            view.FindProperty("inactiveSkullSprite").objectReferenceValue = Sprite("Threshold Skull Inactive");
            var template = (Image)view.FindProperty("skullTemplate").objectReferenceValue;
            SetImage(template, Sprite("Threshold Skull Active"));
            template.rectTransform.sizeDelta = template.sprite.rect.size * 2;
            view.ApplyModifiedPropertiesWithoutUndo();
        });
        AssetDatabase.SaveAssets();

        // Only World is changed. Keep the open Base scene and all its unsaved edits intact.
        var original = SceneManager.GetActiveScene();
        var world = SceneManager.GetSceneByPath("Assets/Scenes/World.unity");
        bool opened = !world.isLoaded;
        if (opened) world = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Additive);
        try
        {
            foreach (var root in world.GetRootGameObjects()) Normalize(root);
            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(world, true);
            if (original.isLoaded) SceneManager.SetActiveScene(original);
        }
        Debug.Log("PSD danger bar installed: centered below resources; two skull states; all sprites at 2x pixels.");
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    static void Edit(string path, Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void Configure(AlarmSystem alarm)
    {
        var config = new SerializedObject(alarm);
        config.FindProperty("thresholdSkullSprite").objectReferenceValue = Sprite("Threshold Skull Active");
        config.FindProperty("inactiveThresholdSkullSprite").objectReferenceValue = Sprite("Threshold Skull Inactive");
        config.FindProperty("alarmOrbSprite").objectReferenceValue = Sprite("Alarm Skull");
        config.FindProperty("orbSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Orb).GetComponent<AlarmOrbSettings>();
        config.FindProperty("thresholdSkullYOffset").floatValue = -13;
        config.FindProperty("thresholdDividerYOffset").floatValue = 4;
        var dividers = config.FindProperty("thresholdDividerSprites");
        dividers.arraySize = 4;
        for (int i = 0; i < 4; i++) dividers.GetArrayElementAtIndex(i).objectReferenceValue = Sprite("Threshold Divider " + (i + 1));
        config.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Normalize(GameObject root)
    {
        foreach (var alarm in root.GetComponentsInChildren<AlarmSystem>(true))
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(alarm) != Bar) continue;
            var rect = (RectTransform)alarm.transform;
            SetRoot(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
    }
    static void SetRoot(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -58);
        rect.sizeDelta = new Vector2(454, 50);
        rect.localScale = Vector3.one;
    }
    static void SetImage(Image image, Sprite sprite)
    {
        image.sprite = sprite; image.color = Color.white; image.type = Image.Type.Simple;
        image.material = null; image.raycastTarget = false; image.preserveAspect = false;
    }
    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
}
#endif
