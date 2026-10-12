#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SquareCloseButtonSetup
{
    const string Old="Assets/Sprites/Ui/Evolution/Popups/Sprites/FieldsBuy/IconClose.png";
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Close Square "+n+".png");
    static bool Match(UnityEngine.UI.Button b)=>b.image&&b.image.sprite&&(AssetDatabase.GetAssetPath(b.image.sprite)==Old||b.image.sprite==S("Normal"));
    static int Configure(GameObject root)
    {
        int count=0;
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if(!Match(b))continue;
            var im=b.image;im.sprite=S("Normal");im.type=UnityEngine.UI.Image.Type.Simple;im.color=Color.white;
            var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(-12,-12);r.sizeDelta=new Vector2(40,40);r.localScale=Vector3.one;
            var artwork=b.transform.parent.Find("Artwork Frame") as RectTransform;
            if(artwork&&artwork.anchorMin==artwork.anchorMax)
            {
                r.anchorMin=r.anchorMax=artwork.anchorMin;
                r.anchoredPosition=artwork.anchoredPosition+new Vector2(artwork.rect.xMax*artwork.localScale.x-12,artwork.rect.yMax*artwork.localScale.y-12);
            }
            b.targetGraphic=im;b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
            var states=b.spriteState;states.highlightedSprite=S("Hover");states.selectedSprite=S("Hover");states.pressedSprite=S("Pressed");states.disabledSprite=S("Normal");b.spriteState=states;
            var f=b.GetComponent<GameFoundation.UI.UnifiedButtonFeedback>()??b.gameObject.AddComponent<GameFoundation.UI.UnifiedButtonFeedback>();
            var so=new SerializedObject(f);so.FindProperty("theme").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameFoundation.UI.ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset");so.FindProperty("animateScale").boolValue=true;so.FindProperty("animateGraphic").boolValue=false;so.FindProperty("useHoverGraphicAlpha").boolValue=false;so.FindProperty("hoverScaleOverride").floatValue=1.08f;so.FindProperty("hoverScaleMultiplier").floatValue=1;so.FindProperty("scaleTarget").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();
            var outline=b.GetComponent<UnityEngine.UI.Outline>();if(outline)outline.enabled=false;
            count++;
        }
        return count;
    }
    public static void Apply()
    {
        foreach(var n in new[]{"Normal","Hover","Pressed"}){var i=(TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/Close Square "+n+".png");i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.SaveAndReimport();}
        int count=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(Match))continue;var g=PrefabUtility.LoadPrefabContents(path);try{count+=Configure(g);PrefabUtility.SaveAsPrefabAsset(g,path);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        string start=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath)){var scene=EditorSceneManager.OpenScene(path);int changed=0;foreach(var g in scene.GetRootGameObjects())changed+=Configure(g);if(changed>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}count+=changed;}
        EditorSceneManager.OpenScene(start);AssetDatabase.SaveAssets();Debug.Log("Square close buttons configured: "+count);
    }
}
#endif
