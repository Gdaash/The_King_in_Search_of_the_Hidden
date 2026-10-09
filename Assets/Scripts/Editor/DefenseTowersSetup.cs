#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class DefenseTowersSetup
{
    public const string Folder = "Assets/Prefabs/Buildings/Defense Towers/";
    static void Set(Object obj, string field, Object value)
    {
        var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Number(Object obj, string field, float value)
    {
        var so = new SerializedObject(obj); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite Import(string name, Vector2 pivot)
    {
        string path = "Assets/Sprites/World/Defense Towers/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static GameObject Projectile(string name, Sprite sprite, float speed)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/PlayerTowerProjectile.prefab");
        var go = Object.Instantiate(source); go.name = name;
        go.transform.localScale = Vector3.one;
        var renderer = go.GetComponentInChildren<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = Color.white;
        renderer.transform.localScale = Vector3.one;
        var collider = go.GetComponent<Collider2D>();
        if (collider is BoxCollider2D box) { box.size = sprite.rect.size / 32f; box.offset = Vector2.zero; }
        Number(go.GetComponent<EnemyProjectile>(), "speed", speed);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Projectiles/" + name + ".prefab");
        Object.DestroyImmediate(go); return prefab;
    }
    public static void Install()
    {
        Directory.CreateDirectory(Folder); Directory.CreateDirectory("Assets/Resources/Stats/Defense Towers"); AssetDatabase.Refresh();
        var magic = Import("Magic Tower", new Vector2(35f/65,23f/75));
        var arrow = Import("Arrow Tower", new Vector2(41f/79,25f/78));
        var stone = Import("Stone Tower", new Vector2(42f/79,25f/76));
        var magicProjectile = Projectile("MagicTowerProjectile", Import("Magic Projectile", new Vector2(.5f,.5f)), 12);
        var stoneProjectile = Projectile("StoneTowerProjectile", Import("Stone Projectile", new Vector2(.5f,.5f)), 9);
        var arrowProjectile = Projectile("ArrowTowerProjectile", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ProjectileArrow.prefab").GetComponentInChildren<SpriteRenderer>().sprite, 18);
        Create("Magic Tower", magic, magicProjectile, 12, 8, 1.2f, DamageType.Magic, .12f, 18);
        Create("Arrow Tower", arrow, arrowProjectile, 3, 9, .22f, DamageType.Physical, .02f, 60);
        Create("Stone Tower", stone, stoneProjectile, 45, 4, 3.5f, DamageType.Physical, .25f, 12);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Magic Tower.prefab");
    }
    static void Create(string name, Sprite sprite, GameObject projectile, float damage, float range, float cooldown, DamageType type, float windup, float recoilSpeed)
    {
        string statsPath = "Assets/Resources/Stats/Defense Towers/" + name + ".asset";
        var stats = AssetDatabase.LoadAssetAtPath<GlobalStats>(statsPath);
        if (stats == null) { stats = ScriptableObject.CreateInstance<GlobalStats>(); AssetDatabase.CreateAsset(stats, statsPath); }
        stats.unitTypeKey = name.Replace(" ", ""); stats.baseMaxHealth = 200; stats.baseRegenAmount = 0;
        stats.baseAttackRange = range; stats.baseAttackCooldown = cooldown;
        stats.damageSettings = new List<GlobalStats.DamageInfo> { new GlobalStats.DamageInfo { type = type, baseDamage = damage } };
        EditorUtility.SetDirty(stats);
        var root = new GameObject(name); root.tag = "Player";
        root.layer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PortalTower.prefab").layer;
        var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
        var originalRenderer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/PortalTower.prefab").GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = originalRenderer.sharedMaterial; renderer.sortingLayerID = originalRenderer.sortingLayerID; renderer.sortingOrder = originalRenderer.sortingOrder;
        var body = root.AddComponent<BoxCollider2D>(); body.size = new Vector2(.5f,.7f); body.offset = new Vector2(0,.35f);
        var tower = root.AddComponent<ArcherTower>(); Set(tower, "stats", stats); Number(tower, "cooldownVariation", cooldown * .05f);
        var health = root.AddComponent<Health>(); Set(health, "stats", stats); Set(health, "targetSprite", renderer);
        var point = new GameObject("ShootPoint"); point.transform.SetParent(visual.transform, false); point.transform.localPosition = new Vector3(0,1.45f,0);
        var shooting = root.AddComponent<TowerVisuals>(); Set(shooting, "tower", tower); Set(shooting, "projectilePrefab", projectile);
        Set(shooting, "shootPoint", point.transform); Set(shooting, "spriteTransform", visual.transform);
        Number(shooting, "windupDuration", windup); Number(shooting, "shootSpeed", recoilSpeed); Number(shooting, "kickbackDist", .035f);
        tower.OnAttack = new UnityEngine.Events.UnityEvent(); UnityEventTools.AddPersistentListener(tower.OnAttack, shooting.StartShoot);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + name + ".prefab"); Object.DestroyImmediate(root);
    }
}
#endif
