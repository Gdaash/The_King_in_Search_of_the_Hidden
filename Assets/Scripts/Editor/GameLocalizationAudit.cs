#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GameFoundation.Localization;
using UnityEditor.SceneManagement;
public static class GameLocalizationAudit
{
    [System.Serializable]public class Row{public string path,property,value;}
    [System.Serializable]public class Rows{public List<Row> rows=new();}
    static bool Russian(string s)=>!string.IsNullOrEmpty(s)&&s.Any(c=>c>='А'&&c<='я'||c=='Ё'||c=='ё');
    static void Scan(Object o,string path,Rows rows)
    {
        var s=new SerializedObject(o);var p=s.GetIterator();
        while(p.Next(true))if(p.propertyType==SerializedPropertyType.String&&(Russian(p.stringValue)||((p.name=="m_Text"||p.name=="m_text")&&!string.IsNullOrEmpty(p.stringValue)&&p.stringValue.Any(char.IsLetter)))&&p.name!="m_Name"&&p.name!="m_EditorClassIdentifier"&&!p.name.EndsWith("ID")&&p.name!="id")rows.rows.Add(new Row{path=path,property=p.propertyPath,value=p.stringValue});
    }
    public static void Export()
    {
        var rows=new Rows();
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {string path=AssetDatabase.GUIDToAssetPath(id);var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var c in root.GetComponentsInChildren<Component>(true))if(c&&c is not Transform)Scan(c,path+" :: "+c.name+" :: "+c.GetType().Name,rows);}
        foreach(var id in AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets"}))
        {string path=AssetDatabase.GUIDToAssetPath(id);var o=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);if(o&&o is not LocalizationTable&&!path.Contains("TextMesh Pro"))Scan(o,path,rows);}
        string original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var id in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {string path=AssetDatabase.GUIDToAssetPath(id);var scene=EditorSceneManager.OpenScene(path);foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Component>(true))if(c&&c is not Transform)Scan(c,path+" :: "+c.name+" :: "+c.GetType().Name,rows);}
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        File.WriteAllText("Temp/LocalizationAssetAudit.json",JsonUtility.ToJson(rows,true));
        File.WriteAllText("Temp/LocalizationExisting.json",JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset"),true));
        Debug.Log("Localization audit: "+rows.rows.Count+" text fields");
    }
}
#endif
