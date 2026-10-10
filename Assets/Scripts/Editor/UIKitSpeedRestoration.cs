#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public static class UIKitSpeedRestoration
{
    const string Art = "Assets/Sprites/Ui/";
    static Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + n + ".png");
    static void Panel(GameSpeedControls c)
    {
        var root = (RectTransform)c.transform;
        var layout = c.GetComponent<HorizontalLayoutGroup>();
        if (layout) Object.DestroyImmediate(layout);
        root.sizeDelta = new Vector2(190,50);
        var frame = c.GetComponent<UnityEngine.UI.Image>();
        frame.sprite=S("Speed Panel"); frame.type=UnityEngine.UI.Image.Type.Simple; frame.color=Color.white;
        var so = new SerializedObject(c);
        so.FindProperty("playSprite").objectReferenceValue=S("Speed Play Button");
        so.FindProperty("pauseSprite").objectReferenceValue=S("Speed Pause Button");
        so.FindProperty("pauseGlyph").objectReferenceValue=null;
        so.ApplyModifiedPropertiesWithoutUndo();
        string[] names={"Pause","Speed 1","Speed 2","Speed 4"};
        for(int i=0;i<4;i++)
        {
            var t=(RectTransform)root.Find(names[i]);
            t.anchorMin=t.anchorMax=new Vector2(0,.5f); t.pivot=new Vector2(.5f,.5f);
            t.anchoredPosition=new Vector2(33+42*i,0); t.sizeDelta=new Vector2(38,38); t.localScale=Vector3.one;
            var im=t.GetComponent<UnityEngine.UI.Image>(); im.sprite=S(i==0?"Speed Pause Button":"Speed Button"); im.type=UnityEngine.UI.Image.Type.Simple; im.color=Color.white;
            foreach(string child in new[]{"Pause Glyph","Icon"}) {var old=t.Find(child);if(old)Object.DestroyImmediate(old.gameObject);}
            var label=t.Find("Label");if(label)label.gameObject.SetActive(false);
            if(i>0)
            {
                var glyph=t.Find("Pixel Label");if(!glyph)glyph=new GameObject("Pixel Label",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;
                glyph.SetParent(t,false);var image=glyph.GetComponent<UnityEngine.UI.Image>();image.sprite=S("Speed Label "+i);image.raycastTarget=false;
                var rect=(RectTransform)glyph;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(i==1?1:0,0);rect.sizeDelta=image.sprite.rect.size*2;rect.localScale=Vector3.one;
            }
        }
        EditorUtility.SetDirty(c);
    }
    static void Crystal(GameObject root)
    {
        foreach(var t in root.GetComponentsInChildren<RectTransform>(true))
            if(t.name=="Experience Crystal") {t.anchorMin=t.anchorMax=new Vector2(.5f,.5f);t.anchoredPosition=new Vector2(-153,11);EditorUtility.SetDirty(t);}
    }
    public static void Apply()
    {
        foreach(string n in new[]{"Speed Panel","Speed Button","Speed Play Button","Speed Pause Button","Speed Label 1","Speed Label 2","Speed Label 3"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+n+".png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.spriteBorder=Vector4.zero;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        foreach(string p in new[]{"Assets/Prefabs/UI/HUD/Game Speed Controls.prefab","Assets/Prefabs/UI/Portal Tower Progression.prefab"})
        {var g=PrefabUtility.LoadPrefabContents(p);try{var c=g.GetComponent<GameSpeedControls>();if(c)Panel(c);Crystal(g);PrefabUtility.SaveAsPrefabAsset(g,p);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        foreach(var c in Object.FindObjectsByType<GameSpeedControls>(FindObjectsInactive.Include,FindObjectsSortMode.None))Panel(c);
        foreach(var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Crystal(g);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
}
#endif
