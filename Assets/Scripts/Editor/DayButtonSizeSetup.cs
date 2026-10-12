#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class DayButtonSizeSetup
{
    static void Configure(GameObject root)
    {
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if(b.name!="Next Day"||b.transform.parent.name!="Day Panel")continue;
            var panel=(RectTransform)b.transform.parent;panel.sizeDelta=new Vector2(160.8f,76.8f);
            var pi=panel.GetComponent<UnityEngine.UI.Image>();pi.type=UnityEngine.UI.Image.Type.Sliced;pi.pixelsPerUnitMultiplier=1.5625f;
            var rect=(RectTransform)b.transform;rect.sizeDelta=new Vector2(48,48);rect.anchoredPosition=new Vector2(-40,4);
            b.image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Day Button Background.png");b.image.type=UnityEngine.UI.Image.Type.Sliced;b.image.pixelsPerUnitMultiplier=1.5625f;
            var icon=b.transform.Find("Hourglass Icon");if(!icon)icon=new GameObject("Hourglass Icon",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;
            icon.SetParent(b.transform,false);var im=icon.GetComponent<UnityEngine.UI.Image>();im.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Day Hourglass Icon.png");im.raycastTarget=false;
            var ir=(RectTransform)icon;ir.anchorMin=ir.anchorMax=ir.pivot=new Vector2(.5f,.5f);ir.anchoredPosition=Vector2.zero;ir.sizeDelta=im.sprite.rect.size*2;ir.localScale=Vector3.one;
            var h=b.GetComponent<DayButtonHover>()??b.gameObject.AddComponent<DayButtonHover>();var so=new SerializedObject(h);so.FindProperty("frame").objectReferenceValue=rect;so.ApplyModifiedPropertiesWithoutUndo();
            var dragon=(RectTransform)panel.Find("Dragon");if(dragon)dragon.anchoredPosition=new Vector2(0,-8);
            var day=(RectTransform)panel.Find("Day");if(day){day.sizeDelta=new Vector2(68,30);day.anchoredPosition=new Vector2(32,3);}
        }
    }
    public static void Apply()
    {
        foreach(var n in new[]{"Day Panel","Day Button Background","Day Hourglass Icon"}){var i=(TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/"+n+".png");i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.spriteBorder=n=="Day Panel"?new Vector4(6,6,6,6):n=="Day Button Background"?new Vector4(4,4,4,4):Vector4.zero;i.SaveAndReimport();}
        foreach(var p in new[]{"Assets/Prefabs/Base/Base Panel.prefab","Assets/Prefabs/Base/Variants/Base Islands Backup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab"}){var g=PrefabUtility.LoadPrefabContents(p);try{Configure(g);PrefabUtility.SaveAsPrefabAsset(g,p);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        foreach(var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Configure(g);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
}
#endif
