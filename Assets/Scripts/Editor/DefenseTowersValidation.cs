#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DefenseTowersValidation
{
    const string Key = "DefenseTowersValidation";
    const string ScenePath = "Assets/Scenes/Defense Towers Validation.unity";
    static IEnumerator test;
    static readonly List<string> checks = new();
    static DefenseTowersValidation() => EditorApplication.playModeStateChanged += Changed;
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        SessionState.SetString(Key+".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SaveScene(scene, ScenePath); EditorSceneManager.CloseScene(scene, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { checks.Clear(); test = Test(); EditorApplication.update += Tick; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+".scene", ""));
            SessionState.SetBool(Key, false); AssetDatabase.DeleteAsset(ScenePath);
        }
    }
    static void Tick()
    {
        try { if (test.MoveNext()) return; checks.Add("COMPLETE"); }
        catch (Exception e) { checks.Add("FAIL "+e); }
        File.WriteAllLines("Temp/DefenseTowersValidation.txt", checks); EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
    }
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks.Add("PASS "+label); }
    static GameObject Target(Vector3 position)
    {
        var go = new GameObject("Enemy target"); go.tag = "Enemy1"; go.transform.position = position;
        go.AddComponent<BoxCollider2D>().size = Vector2.one * .4f;
        var stats = ScriptableObject.CreateInstance<GlobalStats>(); stats.baseMaxHealth = 5000; stats.baseRegenAmount = 0;
        // Configure before Awake so the target starts with its authored health.
        go.SetActive(false); var health = go.AddComponent<Health>();
        var so = new SerializedObject(health); so.FindProperty("stats").objectReferenceValue = stats; so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(true); return go;
    }
    static IEnumerator Test()
    {
        var names = new[] { "Magic Tower", "Arrow Tower", "Stone Tower" };
        var towers = new ArcherTower[3]; var targets = new GameObject[3]; var shots = new int[3];
        for (int i=0;i<3;i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefenseTowersSetup.Folder+names[i]+".prefab");
            var instance = UnityEngine.Object.Instantiate(prefab, new Vector3(i*30,0,0), Quaternion.identity);
            towers[i] = instance.GetComponent<ArcherTower>(); int index = i; towers[i].OnAttack.AddListener(()=>shots[index]++);
            Check(towers[i].GetStats().CanAttack, names[i]+" attacks without portal research");
            var sprite = instance.GetComponentInChildren<SpriteRenderer>().sprite;
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Check(importer.spritePixelsPerUnit==32 && importer.filterMode==FilterMode.Point && importer.textureCompression==TextureImporterCompression.Uncompressed, names[i]+" sharp 32 PPU sprite");
            targets[i] = Target(new Vector3(i*30+12,0,0));
        }
        float until = Time.time+1;
        while(Time.time<until) yield return null;
        Check(shots[0]+shots[1]+shots[2]==0, "no shooting beyond range");
        for(int i=0;i<3;i++) targets[i].transform.position = new Vector3(i*30+2,0,0);
        until = Time.time+5.2f; while(Time.time<until) yield return null;
        Check(shots[1]>=15 && shots[1]>shots[0]*3, "arrow tower fires many weak arrows");
        Check(shots[0]>=3 && shots[2]>=1 && shots[0]>shots[2], "magic shoots regularly, stone rarely");
        for(int i=0;i<3;i++) Check(targets[i].GetComponent<Health>().CurrentHealth<5000, names[i]+" projectile actually damages enemy");
        Check(towers[2].CurrentRange<towers[0].CurrentRange && towers[2].GetComponent<GameFoundation.Combat.Combatant>().weapon.damage>=towers[0].GetComponent<GameFoundation.Combat.Combatant>().weapon.damage*2,"stone short range and powerful damage");
        checks.Add("Shots magic/arrow/stone: "+shots[0]+"/"+shots[1]+"/"+shots[2]);
        // Dead targets must stop attracting fire even while death animation is playing.
        for(int i=0;i<3;i++) targets[i].SetActive(false);
        yield return null; yield return null;
        for(int i=0;i<3;i++) Check(towers[i].GetTarget()==null,names[i]+" clears unavailable target");
        var one = Target(new Vector3(201,0,0)); var two = Target(new Vector3(201,0,0));
        var projectile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/MagicTowerProjectile.prefab"),new Vector3(200,0,0),Quaternion.identity);
        projectile.GetComponent<EnemyProjectile>().Setup(Vector3.right,"Enemy1",new List<GlobalStats.DamageInfo>{new GlobalStats.DamageInfo{type=DamageType.Magic,baseDamage=7}});
        until=Time.time+.4f;while(Time.time<until)yield return null;
        float damage=10000-one.GetComponent<Health>().CurrentHealth-two.GetComponent<Health>().CurrentHealth;
        Check(Mathf.Approximately(damage,7),"magic projectile hits exactly one enemy even with overlapping colliders");
    }
}
#endif
