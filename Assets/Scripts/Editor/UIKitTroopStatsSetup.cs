#if UNITY_EDITOR
using System.Collections.Generic;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UIKitTroopStatsSetup
{
    const string Folder = "Assets/Sprites/Ui/UIKit Stats/";
    static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + ".png");
    static readonly Dictionary<UnitStat, string> Icons = new()
    {
        { UnitStat.Health, "Heart" }, { UnitStat.PhysicalDamage, "Sword" },
        { UnitStat.FireDamage, "Fire" }, { UnitStat.IceDamage, "Ice" },
        { UnitStat.MagicDamage, "Magic" }, { UnitStat.ElectricDamage, "Lightning" },
        { UnitStat.AttackRate, "Lightning" }, { UnitStat.AttackInterval, "Clock" },
        { UnitStat.AttackRange, "Range" }, { UnitStat.MovementSpeed, "Boot" },
        { UnitStat.DetectionRange, "Eye" }, { UnitStat.Defense, "Shield" },
        { UnitStat.Armor, "Shield" }, { UnitStat.PhysicalResistance, "Shield" },
        { UnitStat.FireResistance, "Fire" }, { UnitStat.IceResistance, "Ice" },
        { UnitStat.MagicResistance, "MagicPlus" }, { UnitStat.ElectricResistance, "Lightning" },
        { UnitStat.Regeneration, "Healing" }, { UnitStat.RegenerationDelay, "Clock" },
        { UnitStat.PortalHealing, "Healing" }, { UnitStat.Retreat, "Withdrawal" },
        { UnitStat.TargetPriority, "Target" }, { UnitStat.Dps, "Sword" }
    };

    static void Configure(GameObject root)
    {
        foreach (var view in root.GetComponentsInChildren<UnitDescriptionView>(true))
        {
            var serialized = new SerializedObject(view);
            var entries = serialized.FindProperty("stats");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var stat = (UnitStat)entry.FindPropertyRelative("stat").enumValueIndex;
                // Food remains the actual ResourceType icon, assigned by the view.
                if (Icons.TryGetValue(stat, out string name))
                    entry.FindPropertyRelative("icon").objectReferenceValue = Icon(name);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }
        foreach (var row in root.GetComponentsInChildren<UnitStatRowView>(true))
        {
            var serialized = new SerializedObject(row);
            serialized.FindProperty("statIconColor").colorValue = Color.white;
            var cells = serialized.FindProperty("defenseCells");
            for (int i = 0; i < cells.arraySize; i++)
            {
                var cell = cells.GetArrayElementAtIndex(i);
                int type = cell.FindPropertyRelative("type").enumValueIndex;
                var image = cell.FindPropertyRelative("icon").objectReferenceValue as UnityEngine.UI.Image;
                if (!image) continue;
                string[] names = { "Sword", "Fire", "Ice", "Magic", "Lightning" };
                if (type < names.Length) ResourceIconSizing.Apply(image, Icon(names[type]));
                image.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image.rectTransform);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(row);
        }
    }

    [MenuItem("Tools/UI/Apply tiny UIKit troop stat icons")]
    public static void Apply()
    {
        AssetDatabase.Refresh();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
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
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentInChildren<UnitDescriptionView>(true) && !asset.GetComponentInChildren<UnitStatRowView>(true)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); count++; }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        // Existing scene overrides must receive the same shared artwork.
        string original = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var root in scene.GetRootGameObjects()) Configure(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();
        Debug.Log("Tiny UIKit troop icons applied to " + count + " prefabs and scenes.");
    }
}
#endif
