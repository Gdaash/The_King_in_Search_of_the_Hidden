#if UNITY_EDITOR
using System.IO;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CrystalRecallSetup
{
    public const string CellsPath = "Assets/Prefabs/UI/HUD/Crystal Cells.prefab";
    const string HudPath = "Assets/Prefabs/UI/HUD/Crystal Charge Panel.prefab";
    const string MousePath = "Assets/Sprites/Resources/RBM.png";

    public static void Run()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        CreateMouseIcon();
        InstallHud();
        InstallHover();
        ConfigureMine();
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var entry = table.entries.Find(e => e.key == "world.crystal.recall_humans");
        if (entry == null) { entry = new LocalizationTable.Entry { key = "world.crystal.recall_humans" }; table.entries.Add(entry); }
        entry.values = new() { "Вернуть людей в портал", "Return people to the portal" };
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log("Resident mine worker, RBM icon and shared crystal cell views saved.");
    }

    static void CreateMouseIcon()
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes("Assets/Sprites/Resources/LBM.png"));
        var original = source.GetPixels32();
        var mirrored = new Color32[original.Length];
        for (int y = 0; y < source.height; y++)
            for (int x = 0; x < source.width; x++)
                mirrored[y * source.width + x] = original[y * source.width + source.width - 1 - x];
        source.SetPixels32(mirrored); source.Apply();
        File.WriteAllBytes(MousePath, source.EncodeToPNG());
        Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(MousePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(MousePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    public static void InstallHud()
    {
        var hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var view = hud.GetComponent<CrystalChargeHud>();
            var row = hud.transform.Find("Cells/Cell Row");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CellsPath);
            if (asset == null)
            {
                var copy = Object.Instantiate(row.gameObject);
                copy.name = "Crystal Cells";
                var rt = (RectTransform)copy.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(282, 44); rt.anchoredPosition = Vector2.zero;
                var bar = copy.GetComponent<CrystalCellBar>() ?? copy.AddComponent<CrystalCellBar>();
                var so = new SerializedObject(bar); var cells = so.FindProperty("cells"); cells.arraySize = 6;
                for (int i = 0; i < 6; i++)
                {
                    var cell = copy.transform.Find("Cell " + (i + 1));
                    var binding = cells.GetArrayElementAtIndex(i);
                    binding.FindPropertyRelative("root").objectReferenceValue = cell.gameObject;
                    binding.FindPropertyRelative("frame").objectReferenceValue = cell.GetComponent<Image>();
                    binding.FindPropertyRelative("fill").objectReferenceValue = cell.Find("Charge Area/Charge").GetComponent<Image>();
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                asset = PrefabUtility.SaveAsPrefabAsset(copy, CellsPath);
                Object.DestroyImmediate(copy);
            }
            var nested = hud.transform.Find("Cells/Crystal Cells");
            if (nested == null) nested = ((GameObject)PrefabUtility.InstantiatePrefab(asset, hud.transform.Find("Cells"))).transform;
            var rect = (RectTransform)nested;
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, 44);
            if (row != null) Object.DestroyImmediate(row.gameObject);
            var bindingSo = new SerializedObject(view);
            bindingSo.FindProperty("cellBar").objectReferenceValue = nested.GetComponent<CrystalCellBar>();
            bindingSo.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(hud); }
    }

    public static void InstallHover()
    {
        var barAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CellsPath);
        var mouse = AssetDatabase.LoadAssetAtPath<Sprite>(MousePath);
        if (barAsset == null || mouse == null) return;
        var root = PrefabUtility.LoadPrefabContents(CrystalHexHoverSetup.PrefabPath);
        try
        {
            var so = new SerializedObject(root.GetComponent<CrystalHexHover>());
            var views = so.FindProperty("views");
            for (int i = 0; i < views.arraySize; i++)
            {
                var view = views.GetArrayElementAtIndex(i);
                var visual = (Transform)view.FindPropertyRelative("root").objectReferenceValue;
                var group = (CanvasGroup)view.FindPropertyRelative("captionGroup").objectReferenceValue;
                var caption = (TMP_Text)view.FindPropertyRelative("caption").objectReferenceValue;
                var rect = (RectTransform)group.transform;
                rect.pivot = new Vector2(.5f, 1); rect.localPosition = new Vector3(0, -1.82f, 0);
                rect.sizeDelta = new Vector2(336, 98);
                var layout = group.GetComponent<VerticalLayoutGroup>() ?? group.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 8, 8); layout.spacing = 2;
                var fitter = group.GetComponent<ContentSizeFitter>() ?? group.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlHeight = layout.childControlWidth = true;
                layout.childForceExpandHeight = layout.childForceExpandWidth = false;
                var labelLayout = caption.GetComponent<LayoutElement>() ?? caption.gameObject.AddComponent<LayoutElement>();
                labelLayout.minHeight = labelLayout.preferredHeight = 30; labelLayout.flexibleWidth = 1;
                var oldRecall = visual.Find("Caption/Recall People");
                if (oldRecall != null) Object.DestroyImmediate(oldRecall.gameObject);
                var row = new GameObject("Recall People", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(rect, false);
                var rowSize = row.GetComponent<LayoutElement>(); rowSize.minHeight = rowSize.preferredHeight = 48; rowSize.flexibleWidth = 1;
                var horizontal = row.GetComponent<HorizontalLayoutGroup>(); horizontal.spacing = 8;
                horizontal.childAlignment = TextAnchor.MiddleCenter;
                horizontal.childControlHeight = horizontal.childControlWidth = true;
                horizontal.childForceExpandHeight = horizontal.childForceExpandWidth = false;
                var icon = new GameObject("RBM", typeof(RectTransform), typeof(Image), typeof(LayoutElement)); icon.transform.SetParent(row.transform, false);
                icon.GetComponent<Image>().sprite = mouse; icon.GetComponent<Image>().raycastTarget = false;
                icon.GetComponent<Image>().preserveAspect = true;
                var iconSize = icon.GetComponent<LayoutElement>();
                iconSize.minWidth = iconSize.preferredWidth = mouse.rect.width * 2;
                iconSize.minHeight = iconSize.preferredHeight = mouse.rect.height * 2;
                ((RectTransform)icon.transform).sizeDelta = mouse.rect.size * 2;
                var label = Object.Instantiate(caption.gameObject, row.transform); label.name = "Recall Label";
                var text = label.GetComponent<TMP_Text>(); text.text = "Вернуть людей в портал";
                text.fontSize = 18; text.alignment = TextAlignmentOptions.Left; text.enableWordWrapping = true;
                var sizing = label.GetComponent<LayoutElement>(); sizing.minHeight = sizing.preferredHeight = 48; sizing.flexibleWidth = 1;
                view.FindPropertyRelative("recallRow").objectReferenceValue = row;
                view.FindPropertyRelative("recallCaption").objectReferenceValue = text;

                var oldBar = visual.Find("Crystal Energy"); if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);
                var energy = new GameObject("Crystal Energy", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                energy.transform.SetParent(visual, false); energy.transform.localScale = Vector3.one / 64;
                energy.transform.localPosition = new Vector3(0, 1.95f, 0);
                ((RectTransform)energy.transform).sizeDelta = new Vector2(282, 44);
                var canvas = energy.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingLayerName = "OverLight"; canvas.sortingOrder = 122;
                var energyGroup = energy.GetComponent<CanvasGroup>(); energyGroup.alpha = 0;
                energyGroup.blocksRaycasts = energyGroup.interactable = false;
                var bar = ((GameObject)PrefabUtility.InstantiatePrefab(barAsset, energy.transform)).GetComponent<CrystalCellBar>();
                view.FindPropertyRelative("energyGroup").objectReferenceValue = energyGroup;
                view.FindPropertyRelative("energyBar").objectReferenceValue = bar;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            HexAlarmPreviewSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, CrystalHexHoverSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void ConfigureMine()
    {
        const string path = "Assets/Prefabs/Buildings/Magic Ore Mine.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var builder = root.transform.Find("Controller").GetComponent<ResourceRequester>();
            var mine = root.transform.Find("Mine").GetComponent<ResourceRequester>();
            var human = builder.requirements.First(r => r.resourceType.isHumanResource).resourceType;
            if (!mine.requirements.Any(r => r.resourceType == human))
                mine.requirements.Add(new ResourceRequirement { resourceType = human, requiredAmount = 1 });
            var mineSo = new SerializedObject(mine); mineSo.FindProperty("retainHumansBetweenCycles").boolValue = true; mineSo.ApplyModifiedPropertiesWithoutUndo();
            var builderSo = new SerializedObject(builder); builderSo.FindProperty("humanSuccessor").objectReferenceValue = mine;
            var outputs = builderSo.FindProperty("outputResources");
            for (int i = outputs.arraySize - 1; i >= 0; i--)
            {
                var prefab = (GameObject)outputs.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
                if (prefab != null && prefab.GetComponent<HumanUnit>() != null) outputs.DeleteArrayElementAtIndex(i);
            }
            builderSo.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
