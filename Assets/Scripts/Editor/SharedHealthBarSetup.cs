#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SharedHealthBarSetup
{
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/"+n+".png");
    static void Place(UnityEngine.UI.Image i,Vector2 size,Vector2 pos){i.rectTransform.anchorMin=i.rectTransform.anchorMax=i.rectTransform.pivot=new Vector2(.5f,.5f);i.rectTransform.sizeDelta=size;i.rectTransform.anchoredPosition=pos;i.color=Color.white;i.raycastTarget=false;}
    static int Configure(GameObject root)
    {
        int count=0;
        foreach(var h in root.GetComponentsInChildren<HealthBar>(true))
        {
            var so=new SerializedObject(h);var main=(UnityEngine.UI.Image)so.FindProperty("mainBar").objectReferenceValue;var buffer=(UnityEngine.UI.Image)so.FindProperty("bufferBar").objectReferenceValue;if(!main)continue;
            var bg=h.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i=>i!=main&&i!=buffer);if(!bg)continue;
            float width=bg.rectTransform.rect.width;float pixel=width/44;
            bg.sprite=S("Health Frame");bg.type=UnityEngine.UI.Image.Type.Simple;Place(bg,new Vector2(width,pixel*7),Vector2.zero);
            foreach(var i in new[]{main,buffer}){if(!i)continue;i.sprite=S(i==main?"Health Fill":"Health Damage Buffer");i.type=UnityEngine.UI.Image.Type.Filled;i.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;i.fillOrigin=0;Place(i,new Vector2(pixel*42,pixel*5),Vector2.zero);}
            count++;
        }
        foreach(var v in root.GetComponentsInChildren<GameFoundation.MetaProgression.WorldMilitaryRosterItemView>(true))
        {
            var so=new SerializedObject(v);var fill=(UnityEngine.UI.Image)so.FindProperty("healthFill").objectReferenceValue;if(!fill)continue;
            var bg=fill.transform.parent.GetComponent<UnityEngine.UI.Image>();if(!bg)continue;var r=bg.rectTransform;r.sizeDelta=new Vector2(88,14);bg.sprite=S("Health Frame");bg.type=UnityEngine.UI.Image.Type.Simple;bg.color=Color.white;fill.sprite=S("Health Fill");fill.type=UnityEngine.UI.Image.Type.Simple;fill.color=Color.white;fill.rectTransform.anchorMin=fill.rectTransform.anchorMax=new Vector2(0,.5f);fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=new Vector2(2,0);fill.rectTransform.sizeDelta=new Vector2(84,10);count++;
        }
        return count;
    }
    public static void Apply()
    {
        foreach(string n in new[]{"Health Frame","Health Fill","Health Damage Buffer"}){var i=(TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/"+n+".png");i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.SaveAndReimport();}
        int count=0;
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p=="Assets/Prefabs/UI/HPBar.prefab"?0:1).ToArray();
        foreach(var p in paths){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!asset.GetComponentInChildren<HealthBar>(true)&&!asset.GetComponentInChildren<GameFoundation.MetaProgression.WorldMilitaryRosterItemView>(true))continue;var g=PrefabUtility.LoadPrefabContents(p);try{count+=Configure(g);PrefabUtility.SaveAsPrefabAsset(g,p);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        var start=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var p in new[]{"Assets/Scenes/Base.unity","Assets/Scenes/World.unity"}){var scene=EditorSceneManager.OpenScene(p);foreach(var g in scene.GetRootGameObjects())count+=Configure(g);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}EditorSceneManager.OpenScene(start);AssetDatabase.SaveAssets();Debug.Log("Health bar appearances updated: "+count);
    }
}
#endif
