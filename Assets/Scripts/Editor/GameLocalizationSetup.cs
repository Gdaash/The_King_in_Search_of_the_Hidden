#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.Localization;

public static class GameLocalizationSetup
{
    [Serializable]class Pair{public string ru,en;}
    [Serializable]class Import{public Pair[] rows;}
    static int bindings;
    static void Bind(GameObject root)
    {
        foreach(var c in root.GetComponentsInChildren<Component>(true))
        {
            if(c is not TMPro.TMP_Text && c is not UnityEngine.UI.Text)continue;
            if(c.GetComponent<LocalizedText>()||c.GetComponent<LocalizedGameText>())continue;
            c.gameObject.AddComponent<LocalizedGameText>();bindings++;
        }
    }
    public static void Apply()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        int ru=table.languages.IndexOf("ru"),en=table.languages.IndexOf("en");
        var data=JsonUtility.FromJson<Import>(File.ReadAllText("Temp/LocalizationImport.json"));int added=0;
        foreach(var pair in data.rows)
        {
            if(table.entries.Any(e=>e.values.Count>Math.Max(ru,en)&&e.values[ru]==pair.ru&&e.values[en]==pair.en))continue;
            string key;using(var sha=SHA256.Create())key="game.text."+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(pair.ru+"\n"+pair.en))).Replace("-","").Substring(0,16).ToLowerInvariant();
            var entry=table.entries.FirstOrDefault(e=>e.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);added++;}
            while(entry.values.Count<table.languages.Count)entry.values.Add("");entry.values[ru]=pair.ru;entry.values[en]=pair.en;
        }
        EditorUtility.SetDirty(table);bindings=0;
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(id);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentInChildren<UnityEngine.UI.Text>(true)&&!asset.GetComponentInChildren<TMPro.TMP_Text>(true))continue;
            var root=PrefabUtility.LoadPrefabContents(path);try{Bind(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var id in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(id);var scene=EditorSceneManager.OpenScene(path);
            foreach(var root in scene.GetRootGameObjects())Bind(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();Debug.Log("Localization: "+added+" new entries; "+bindings+" text bindings");
        Validate();
    }
    public static void Validate()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var gaps=table.entries.Where(e=>e.values.Count<table.languages.Count||e.values.Any(string.IsNullOrWhiteSpace)).Select(e=>e.key).ToArray();
        if(gaps.Length>0)throw new Exception("Untranslated entries: "+string.Join(", ",gaps));
        Debug.Log("Localization COMPLETE: "+table.entries.Count+" keys, "+table.languages.Count+" languages, no empty entries");
    }
}
#endif
