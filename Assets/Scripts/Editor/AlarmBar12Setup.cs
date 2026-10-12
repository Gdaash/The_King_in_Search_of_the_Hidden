#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AlarmBar12Setup
{
    const string Art="Assets/Sprites/Ui/Alarm 12/";
    static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
    static void Place(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
    static void Configure(GameObject root)
    {
        foreach(var alarm in root.GetComponentsInChildren<AlarmSystem>(true))
        {
            var frame=alarm.GetComponent<UnityEngine.UI.Image>();if(!frame)continue;
            frame.sprite=S("Frame");frame.type=UnityEngine.UI.Image.Type.Simple;frame.color=Color.white;
            var rect=frame.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.sizeDelta=frame.sprite.rect.size*2;rect.anchoredPosition=new Vector2(0,-56);rect.localScale=Vector3.one;
            var s=new SerializedObject(alarm);
            foreach(string field in new[]{"fillImage","previewImage"})
            {
                var image=(UnityEngine.UI.Image)s.FindProperty(field).objectReferenceValue;
                image.sprite=S("Fill");Place(image.rectTransform,new Vector2(-2,3),image.sprite.rect.size*2);
                image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillOrigin=0;
            }
            var ticks=(RectTransform)s.FindProperty("tickContainer").objectReferenceValue;Place(ticks,new Vector2(0,2),new Vector2(360,42));
            s.FindProperty("thresholdSkullSprite").objectReferenceValue=S("Skull Active");s.FindProperty("inactiveThresholdSkullSprite").objectReferenceValue=S("Skull Inactive");
            s.FindProperty("thresholdSkullYOffset").floatValue=-17;s.FindProperty("thresholdDividerYOffset").floatValue=0;
            var div=s.FindProperty("thresholdDividerSprites");div.arraySize=1;div.GetArrayElementAtIndex(0).objectReferenceValue=S("Divider");s.ApplyModifiedPropertiesWithoutUndo();
            var start=alarm.transform.Find("Start Divider");if(!start){start=new GameObject("Start Divider",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;start.SetParent(alarm.transform,false);}
            start.gameObject.SetActive(true);var startImage=start.GetComponent<UnityEngine.UI.Image>();startImage.sprite=S("Start Divider");startImage.color=Color.white;startImage.raycastTarget=false;
            Place(startImage.rectTransform,new Vector2(-180,2),startImage.sprite.rect.size*2);start.SetAsLastSibling();ticks.SetAsLastSibling();
            alarm.RefreshThresholdMarkers();
            foreach(var c in alarm.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
    }
    public static void Apply()
    {
        foreach(var file in Directory.GetFiles(Art,"*.png"))
        {
            AssetDatabase.ImportAsset(file);var i=(TextureImporter)AssetImporter.GetAtPath(file);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.SaveAndReimport();
        }
        foreach(var path in new[]{AlarmBarPsdSetup.Bar,"Assets/Prefabs/UI/Screens/World Screen HUD.prefab"})
        {var root=PrefabUtility.LoadPrefabContents(path);try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        var original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        if(original!="Assets/Scenes/World.unity")EditorSceneManager.OpenScene("Assets/Scenes/World.unity");
        foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())Configure(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        if(original!="Assets/Scenes/World.unity"&&!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
    }
}
#endif
