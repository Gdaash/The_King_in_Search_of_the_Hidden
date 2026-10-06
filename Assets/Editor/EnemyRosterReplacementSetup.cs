#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Replaces the legacy enemy roster with the four authored enemies from the supplied sprite sheet.</summary>
public static class EnemyRosterReplacementSetup
{
    private const string Units = "Assets/Prefabs/Units/";
    private const string Stats = "Assets/Resources/Units/";
    private const string Sprites = "Assets/Sprites/Units/Enemies/";
    private const string Projectile = "Assets/Prefabs/Projectiles/CursedMageProjectile.prefab";
    private static readonly string[] ReferencingPrefabs =
    {
        "Assets/Prefabs/Buildings/Enemys/Boar tree.prefab",
        "Assets/Prefabs/Buildings/Enemys/Cave.prefab",
        "Assets/Prefabs/Buildings/Enemys/GoblinCave.prefab",
        "Assets/Prefabs/Buildings/Enemys/Pyramid.prefab",
        "Assets/Prefabs/Buildings/Enemys/TreeHouse.prefab",
        "Assets/Prefabs/Managers/AlarmSystem.prefab",
        "Assets/Prefabs/UI/HUD/Alarm Bar.prefab",
        "Assets/Prefabs/UI/Screens/World Screen HUD.prefab"
    };

    [MenuItem("Tools/Game Foundation/World/Install Current Enemy Roster")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Prefabs/Projectiles");
        AssetDatabase.Refresh();
        ConfigureImportedSprites();

        // Moving, rather than recreating, keeps references to the first two enemies valid.
        Move("Assets/Prefabs/Units/Goblin.prefab", Units + "Octopus.prefab");
        Move("Assets/Resources/Units/Goblin.asset", Stats + "Octopus.asset");
        Move("Assets/Prefabs/Units/GreenArcher.prefab", Units + "CursedMage.prefab");
        Move("Assets/Resources/Units/GreenArcher.asset", Stats + "CursedMage.asset");
        Move("Assets/Prefabs/ProjectileMagic.prefab", Projectile);

        GameObject octopus = Prefab("Octopus");
        GameObject mage = Prefab("CursedMage");
        GlobalStats octopusStats = Stat("Octopus");
        GlobalStats mageStats = Stat("CursedMage");
        if (octopus == null || mage == null || octopusStats == null || mageStats == null)
            throw new InvalidOperationException("Base enemy prefabs or GlobalStats assets are missing.");

        ConfigureStats(octopusStats, "Octopus", 40f, 1.5f, 1.5f, .4f, 5f);
        ConfigureStats(mageStats, "CursedMage", 40f, 1.5f, 1.5f, 4f, 15f);
        var knightStats = CreateStats("CursedKnight", octopusStats, 120f, 1.5f, 1.5f, .4f, 22f);
        var warriorStats = CreateStats("OctopusWarrior", octopusStats, 300f, .75f, 3.5f, .6f, 48f);

        ConfigureMeleePrefab(Units + "Octopus.prefab", "Octopus", octopusStats, Sprites + "Octopus.png");
        ConfigureRangedPrefab(Units + "CursedMage.prefab", "CursedMage", mageStats, Sprites + "CursedMage.png", Projectile);
        CloneMeleePrefab(Units + "Octopus.prefab", Units + "CursedKnight.prefab", "CursedKnight", knightStats, Sprites + "CursedKnight.png");
        CloneMeleePrefab(Units + "Octopus.prefab", Units + "OctopusWarrior.prefab", "OctopusWarrior", warriorStats, Sprites + "OctopusWarrior.png");
        ConfigureProjectile(Projectile, Sprites + "CursedMageProjectile.png");

        var replacements = new Dictionary<Object, Object>
        {
            { Prefab("Orc"), Prefab("CursedKnight") },
            { Prefab("Skeleton"), Prefab("OctopusWarrior") },
            { Prefab("Snake"), Prefab("CursedMage") },
            { Prefab("Troll"), Prefab("OctopusWarrior") }
        }.Where(pair => pair.Key != null && pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value);
        ReplaceReferences(replacements);
        ConfigureAlarmTables(Prefab("Octopus"), Prefab("CursedMage"), Prefab("CursedKnight"), Prefab("OctopusWarrior"));

        DeleteLegacyEnemies();
        DeleteLegacyBestiaryAssets();
        EnemyRosterSetup.Run();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Installed current enemy roster: Octopus, Cursed Mage, Cursed Knight and Octopus Warrior.");
    }

    private static void ConfigureImportedSprites()
    {
        string absoluteSprites = Path.GetFullPath(Sprites);
        foreach (string absolutePath in Directory.GetFiles(absoluteSprites, "*.png"))
        {
            string path = "Assets" + absolutePath.Substring(Application.dataPath.Length).Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureMeleePrefab(string path, string name, GlobalStats stats, string spritePath)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { ConfigureMelee(root, name, stats, spritePath); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CloneMeleePrefab(string sourcePath, string destinationPath, string name, GlobalStats stats, string spritePath)
    {
        var root = PrefabUtility.LoadPrefabContents(sourcePath);
        try { ConfigureMelee(root, name, stats, spritePath); PrefabUtility.SaveAsPrefabAsset(root, destinationPath); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureMelee(GameObject root, string name, GlobalStats stats, string spritePath)
    {
        root.name = name;
        root.tag = "Enemy1";
        root.transform.localScale = Vector3.one;
        SetStats(root, stats);
        SetSprite(root, spritePath);
    }

    private static void ConfigureRangedPrefab(string path, string name, GlobalStats stats, string spritePath, string projectilePath)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = name;
            root.tag = "Enemy1";
            root.transform.localScale = Vector3.one;
            SetStats(root, stats);
            SetSprite(root, spritePath);
            var visuals = root.GetComponent<EnemyVisuals_Ranged>();
            Set(visuals, "projectilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureProjectile(string path, string spritePath)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = "CursedMageProjectile";
            root.transform.localScale = Vector3.one;
            SetSprite(root, spritePath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetSprite(GameObject root, string spritePath)
    {
        var renderer = root.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null) throw new InvalidOperationException("SpriteRenderer is missing on " + root.name);
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        renderer.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(renderer);
    }

    private static GlobalStats CreateStats(string name, GlobalStats source, float health, float speed, float cooldown, float range, float damage)
    {
        string path = Stats + name + ".asset";
        var stats = AssetDatabase.LoadAssetAtPath<GlobalStats>(path);
        if (stats == null)
        {
            stats = Object.Instantiate(source);
            stats.name = name;
            AssetDatabase.CreateAsset(stats, path);
        }
        ConfigureStats(stats, name, health, speed, cooldown, range, damage);
        return stats;
    }

    private static void ConfigureStats(GlobalStats stats, string name, float health, float speed, float cooldown, float range, float damage)
    {
        var serialized = new SerializedObject(stats);
        serialized.FindProperty("unitTypeKey").stringValue = name;
        serialized.FindProperty("baseMaxHealth").floatValue = health;
        serialized.FindProperty("baseSpeed").floatValue = speed;
        serialized.FindProperty("baseAttackCooldown").floatValue = cooldown;
        serialized.FindProperty("baseAttackRange").floatValue = range;
        var damageSettings = serialized.FindProperty("damageSettings");
        damageSettings.arraySize = 1;
        var first = damageSettings.GetArrayElementAtIndex(0);
        first.FindPropertyRelative("type").enumValueIndex = (int)DamageType.Physical;
        first.FindPropertyRelative("baseDamage").floatValue = damage;
        first.FindPropertyRelative("bonusDamage").floatValue = 0f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stats);
    }

    private static void SetStats(GameObject root, GlobalStats stats)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty("stats");
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) continue;
            property.objectReferenceValue = stats;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }
    }

    private static void ConfigureAlarmTables(params GameObject[] enemies)
    {
        var entries = enemies.Select((prefab, index) => new AlarmEnemy { prefab = prefab, weight = Mathf.Max(1, 5 - index) }).ToList();
        foreach (var guid in AssetDatabase.FindAssets("t:AlarmDifficultyTable"))
        {
            var table = AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var difficulty in table.difficulties)
                foreach (var threshold in difficulty.thresholds) threshold.enemies = entries.Select(item => new AlarmEnemy { prefab = item.prefab, weight = item.weight }).ToList();
            EditorUtility.SetDirty(table);
        }
        foreach (string path in new[] { "Assets/Prefabs/Managers/AlarmSystem.prefab", "Assets/Prefabs/UI/HUD/Alarm Bar.prefab" })
        {
            if (!File.Exists(path)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var alarm in root.GetComponentsInChildren<AlarmSystem>(true))
                    foreach (var threshold in alarm.ConfiguredThresholds)
                        threshold.enemies = entries.Select(item => new AlarmEnemy { prefab = item.prefab, weight = item.weight }).ToList();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    private static void ReplaceReferences(IReadOnlyDictionary<Object, Object> replacements)
    {
        // Only these content sources can hold legacy enemy prefabs.  Scanning packages and
        // every project asset makes this one-time migration unnecessarily slow and risky.
        foreach (string path in ReferencingPrefabs.Where(File.Exists))
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = ReplaceReferences(root.GetComponentsInChildren<Component>(true), replacements);
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        const string worldScene = "Assets/Scenes/World.unity";
        if (File.Exists(worldScene))
        {
            var scene = EditorSceneManager.OpenScene(worldScene, OpenSceneMode.Single);
            bool changed = ReplaceReferences(Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None), replacements);
            if (changed) EditorSceneManager.SaveScene(scene);
        }
        foreach (string path in new[] { "Assets/Resources/World/AlarmDifficultyTable.asset" }.Where(File.Exists))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || asset is DefaultAsset) continue;
            if (ReplaceReference(asset, replacements)) EditorUtility.SetDirty(asset);
        }
    }

    private static bool ReplaceReferences(IEnumerable<Component> components, IReadOnlyDictionary<Object, Object> replacements) =>
        components.Where(component => component != null).Aggregate(false, (changed, component) => ReplaceReference(component, replacements) || changed);

    private static bool ReplaceReference(Object target, IReadOnlyDictionary<Object, Object> replacements)
    {
        var serialized = new SerializedObject(target); var iterator = serialized.GetIterator(); bool changed = false;
        while (iterator.NextVisible(true))
            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null &&
                replacements.TryGetValue(iterator.objectReferenceValue, out Object replacement))
            { iterator.objectReferenceValue = replacement; changed = true; }
        if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }

    private static void DeleteLegacyEnemies()
    {
        foreach (string name in new[] { "Orc", "Skeleton", "Snake", "Troll" })
        {
            AssetDatabase.DeleteAsset(Units + name + ".prefab");
            AssetDatabase.DeleteAsset(Stats + name + ".asset");
        }
        DeleteUnreferencedLegacySprites();
    }

    private static void DeleteUnreferencedLegacySprites()
    {
        // These are the six sprites used by the retired gameplay prefabs. Do not search by
        // filename: the imported art collection contains unrelated source illustrations with
        // words such as "Goblin" and "Snake" in their names.
        string[] candidates =
        {
            "Assets/Sprites/Units/Enemies/Legacy/Goblin.png",
            "Assets/Sprites/Units/NPCs/Green/GreenHuman3.png",
            "Assets/Sprites/Units/Enemies/Legacy/Orc.png",
            "Assets/Sprites/Units/Enemies/Legacy/Skeleton.png",
            "Assets/Sprites/Units/Enemies/Legacy/Snake.png",
            "Assets/Sprites/Units/Enemies/Legacy/Troll.png"
        };
        var referencedAssets = new HashSet<string>(
            ReferencingPrefabs.Where(File.Exists).SelectMany(path => AssetDatabase.GetDependencies(path, true)));
        if (File.Exists("Assets/Scenes/World.unity"))
            referencedAssets.UnionWith(AssetDatabase.GetDependencies("Assets/Scenes/World.unity", true));
        if (File.Exists("Assets/Resources/World/AlarmDifficultyTable.asset"))
            referencedAssets.UnionWith(AssetDatabase.GetDependencies("Assets/Resources/World/AlarmDifficultyTable.asset", true));
        foreach (string spritePath in candidates)
        {
            if (!referencedAssets.Contains(spritePath)) AssetDatabase.DeleteAsset(spritePath);
        }
    }

    private static void DeleteLegacyBestiaryAssets()
    {
        foreach (string path in AssetDatabase.FindAssets("Enemy t:UnitDescriptionDefinition")
                     .Select(AssetDatabase.GUIDToAssetPath).ToArray()) AssetDatabase.DeleteAsset(path);
        const string portraits = "Assets/Sprites/UI/Portraits/Enemies";
        if (AssetDatabase.IsValidFolder(portraits)) AssetDatabase.DeleteAsset(portraits);
    }

    private static void Move(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(destination) != null) return;
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }
    private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Units + name + ".prefab");
    private static GlobalStats Stat(string name) => AssetDatabase.LoadAssetAtPath<GlobalStats>(Stats + name + ".asset");
    private static void Set(Object target, string field, Object value)
    {
        if (target == null) throw new InvalidOperationException("Missing required component for " + field);
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
}
#endif
