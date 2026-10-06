#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyAudioCleanup
{
    static readonly string[] AudioFolders = { "Assets/Resources/Sound", "Assets/Resources/LegacyFindIt/Music" };
    static readonly string[] Scripts = {
        "Assets/Scripts/Audio/BackgroundMusicSwitcher.cs",
        "Assets/Scripts/Audio/MusicService.cs",
        "Assets/Scripts/Audio/MusicServiceInstaller.cs",
        "Assets/Scripts/Audio/IMusicService.cs",
        "Assets/Scripts/LegacyFindIt/Core/Main/Utility/Region/MusicRegion.cs",
        "Assets/Scripts/LegacyFindIt/Core/Main/Utility/Tracker/MusicVolumeTracker.cs",
        "Assets/Scripts/LegacyFindIt/Core/Main/Utility/Tracker/SoundVolumeTracker.cs"
    };
    static HashSet<UnityEngine.Object> clips, scripts;
    static int removed, cleared;
    static bool ClearReferences(UnityEngine.Object target, HashSet<UnityEngine.Object> doomed)
    {
        if(target==null)return false;
        var so=new SerializedObject(target);var p=so.GetIterator();bool changed=false;
        while(p.Next(true))
            if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue!=null &&
               (clips.Contains(p.objectReferenceValue) || doomed.Contains(p.objectReferenceValue)))
            {p.objectReferenceValue=null;changed=true;cleared++;}
        // Zenject installer arrays cannot retain a removed installer as a null entry.
        foreach(var field in new[]{"_monoInstallers","_scriptableObjectInstallers","_installerPrefabs"})
        {
            var array=so.FindProperty(field);if(array==null || !array.isArray)continue;
            for(int i=array.arraySize-1;i>=0;i--)
                if(array.GetArrayElementAtIndex(i).propertyType==SerializedPropertyType.ObjectReference &&
                   array.GetArrayElementAtIndex(i).objectReferenceValue==null)
                {array.DeleteArrayElementAtIndex(i);changed=true;}
        }
        if(changed)so.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }
    static bool Clean(GameObject[] roots)
    {
        var components=roots.SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
        var doomed=new HashSet<UnityEngine.Object>();
        foreach(var c in components.OfType<MonoBehaviour>())
            if(scripts.Contains(MonoScript.FromMonoBehaviour(c)))doomed.Add(c);
        foreach(var audio in components.OfType<AudioSource>())
            if(audio.clip!=null && clips.Contains(audio.clip))doomed.Add(audio);
        foreach(var c in components.Where(c=>doomed.Contains(c)))
        {
            var so=new SerializedObject(c);var p=so.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue is AudioSource audio)doomed.Add(audio);
        }
        bool changed=false;
        foreach(var c in components)if(!doomed.Contains(c))changed|=ClearReferences(c,doomed);
        foreach(var c in doomed){UnityEngine.Object.DestroyImmediate(c);removed++;changed=true;}
        return changed;
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Exit Play Mode.");
        clips=AudioFolders.Where(AssetDatabase.IsValidFolder).SelectMany(f=>AssetDatabase.FindAssets("t:AudioClip",new[]{f}))
            .Select(g=>AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g))).ToHashSet();
        scripts=Scripts.Select(AssetDatabase.LoadAssetAtPath<MonoScript>).Where(s=>s!=null).Cast<UnityEngine.Object>().ToHashSet();
        removed=cleared=0;
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && !p.StartsWith("Assets/Plugins/")).ToArray();
        foreach(var path in paths.Where(p=>p.EndsWith(".prefab")))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{if(Clean(new[]{root}))PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in paths.Where(p=>p.EndsWith(".asset")))
        {
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if(asset!=null && ClearReferences(asset,new()))EditorUtility.SetDirty(asset);
        }
        const string temp="Assets/__LegacyAudioCleanup";
        if(!AssetDatabase.IsValidFolder(temp))AssetDatabase.CreateFolder("Assets","__LegacyAudioCleanup");
        try
        {
            foreach(var path in paths.Where(p=>p.EndsWith(".unity")))
            {
                var copy=temp+"/"+Path.GetFileName(path);
                File.Copy(path,copy,true);AssetDatabase.ImportAsset(copy);
                var scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);
                try{if(Clean(scene.GetRootGameObjects())){EditorSceneManager.SaveScene(scene,copy);File.Copy(copy,path,true);}}
                finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.DeleteAsset(copy);}
            }
        }
        finally{AssetDatabase.DeleteAsset(temp);}
        for(int i=0;i<SceneManager.sceneCount;i++)Clean(SceneManager.GetSceneAt(i).GetRootGameObjects());
        AssetDatabase.SaveAssets();
        int count=clips.Count;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var folder in AudioFolders)if(AssetDatabase.IsValidFolder(folder) && !AssetDatabase.DeleteAsset(folder))throw new Exception("Cannot delete "+folder);
            foreach(var script in Scripts)if(File.Exists(script) && !AssetDatabase.DeleteAsset(script))throw new Exception("Cannot delete "+script);
        }
        finally{AssetDatabase.StopAssetEditing();}
        return "Removed "+count+" legacy clips, "+removed+" components, "+Scripts.Length+" obsolete scripts; cleared "+cleared+" references.";
    }
}
#endif
