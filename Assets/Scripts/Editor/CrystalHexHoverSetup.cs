#if UNITY_EDITOR
using System.IO;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CrystalHexHoverSetup
{
    public const string PrefabPath = "Assets/Prefabs/UI/World/Crystal Hex Hover.prefab";
    private const string SpriteFolder = "Assets/Sprites/Resources/";
    private static Sprite Recolour(string name, float hue, bool gray = false)
    {
        // Palette-only edit: identical dimensions, pixel positions and alpha to the original Flag D.
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(SpriteFolder + "Flag D.png"));
        var pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            var original = pixels[i];
            Color.RGBToHSV(original, out _, out _, out float value);
            var colour = gray ? Color.Lerp(new Color(.06f, .065f, .08f), new Color(.34f, .35f, .38f), value) :
                Color.HSVToRGB(hue, Mathf.Lerp(.85f, .5f, value), value);
            colour.a = original.a;
            pixels[i] = colour;
        }
        texture.SetPixels(pixels); texture.Apply();
        string path = SpriteFolder + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void Run()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play first");
        var red = Recolour("Flag D Red", .005f);
        var purple = Recolour("Flag D Purple", .765f);
        var gray = Recolour("Flag D Gray", 0, true);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/World")) AssetDatabase.CreateFolder("Assets/Prefabs/UI", "World");
        var root = new GameObject("Crystal Hex Hover", typeof(CrystalHexHover));
        var so = new SerializedObject(root.GetComponent<CrystalHexHover>());
        so.FindProperty("availableSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Flag D.png");
        so.FindProperty("workingSprite").objectReferenceValue = red;
        so.FindProperty("cancelSprite").objectReferenceValue = purple;
        so.FindProperty("noEnergySprite").objectReferenceValue = gray;
        var views = so.FindProperty("views"); views.arraySize = 2;
        for (int i = 0; i < 2; i++)
        {
            var visual = new GameObject("Hover View " + (i + 1)); visual.transform.SetParent(root.transform, false);
            var frame = new GameObject("Flag D", typeof(SpriteRenderer)); frame.transform.SetParent(visual.transform, false);
            var sr = frame.GetComponent<SpriteRenderer>(); sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Flag D.png");
            sr.sharedMaterial = Object.FindFirstObjectByType<CrystalLightBeam>().Flag.GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;
            sr.sortingLayerName = "OverLight"; sr.sortingOrder = 120; sr.color = new Color(1, 1, 1, 0);
            var cg = new GameObject("Caption", typeof(RectTransform), typeof(Canvas)); cg.transform.SetParent(visual.transform, false);
            cg.transform.localPosition = new Vector3(0, -2.05f, 0); cg.transform.localScale = Vector3.one / 64;
            var cr = (RectTransform)cg.transform; cr.sizeDelta = new Vector2(288, 40);
            var canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingLayerName = "OverLight"; canvas.sortingOrder = 121;
            var group = cg.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            var bg = cg.AddComponent<Image>(); bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Popups/Sprites/Shared/PopupPanel9Slice.png");
            bg.type = Image.Type.Sliced; bg.color = new Color(1, 1, 1, .94f); bg.raycastTarget = false;
            var tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); tg.transform.SetParent(cg.transform, false);
            var tr = (RectTransform)tg.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var text = tg.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixellari Cyrillic/Pixellari Cyrillic UI.asset");
            text.fontSize = 18; text.alignment = TextAlignmentOptions.Center; text.color = new Color32(242, 233, 209, 255);
            text.raycastTarget = false; text.text = ""; text.enableWordWrapping = false;
            var shadow = tg.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .9f); shadow.effectDistance = new Vector2(1, -1);
            var v = views.GetArrayElementAtIndex(i); v.FindPropertyRelative("root").objectReferenceValue = visual.transform;
            v.FindPropertyRelative("frame").objectReferenceValue = sr; v.FindPropertyRelative("caption").objectReferenceValue = text;
            v.FindPropertyRelative("captionGroup").objectReferenceValue = group;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root); CrystalRecallSetup.InstallHover();
        var existing = Object.FindFirstObjectByType<CrystalHexHover>(FindObjectsInactive.Include);
        if (existing == null) existing = ((GameObject)PrefabUtility.InstantiatePrefab(asset)).GetComponent<CrystalHexHover>();
        var live = new SerializedObject(existing);
        live.FindProperty("crystal").objectReferenceValue = Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        live.FindProperty("worldCamera").objectReferenceValue = Camera.main;
        live.ApplyModifiedProperties(); PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Translate(table, "world.crystal.cancel", "отменить действие", "cancel action");
        Translate(table, "world.crystal.no_energy", "нет энергии", "no energy");
        Translate(table, "world.crystal.busy", "кристалл занят", "crystal is busy");
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table);
        EditorSceneManager.MarkSceneDirty(existing.gameObject.scene); EditorSceneManager.SaveScene(existing.gameObject.scene);
        Selection.activeGameObject = existing.gameObject;
        Debug.Log("Crystal hex hover prefab, palette variants and World instance saved.");
    }
    private static void Translate(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.Find(e => e.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new() { ru, en };
    }
}
#endif
