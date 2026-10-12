#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.UI;
using GameFoundation.Quests;

public static class HudEdgeLayoutSetup
{
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/"+n+".png");
    static void Align(RectTransform rect, VerticalMilitaryPanel panel)
    {
        var a=rect.GetComponent<BesideMilitaryPanel>()??rect.gameObject.AddComponent<BesideMilitaryPanel>();
        var so=new SerializedObject(a);so.FindProperty("panel").objectReferenceValue=panel;so.FindProperty("gap").floatValue=0;so.FindProperty("preserveVerticalPosition").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        rect.anchoredPosition=new Vector2(-(panel?panel.Width:120),rect.anchoredPosition.y);
    }
    static void Configure(GameObject root)
    {
        var panels=root.GetComponentsInChildren<VerticalMilitaryPanel>(true);
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="ResourcesUI"))
        {
            rect.Find("Right Cap")?.gameObject.SetActive(false);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.sizeDelta=new Vector2(1350,54);rect.anchoredPosition=new Vector2(-120,0);
            var panel=panels.FirstOrDefault(p=>p.GetComponentInParent<Canvas>()==rect.GetComponentInParent<Canvas>());
            if(panel)Align(rect,panel);
        }
        foreach(var q in root.GetComponentsInChildren<QuestPanel>(true))
        {
            var align=q.GetComponent<BesideMilitaryPanel>();if(align)Object.DestroyImmediate(align);
            var rect=(RectTransform)q.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(0,-120);
        }
        foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name=="Next Day"))
        {
            var rect=(RectTransform)button.transform;var parent=rect.parent;
            RectTransform block;
            if(parent.name=="Day Panel")block=(RectTransform)parent;
            else {var existing=parent.Find("Day Panel");block=existing?(RectTransform)existing:(RectTransform)new GameObject("Day Panel",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;block.SetParent(parent,false);}
            block.anchorMin=block.anchorMax=block.pivot=new Vector2(1,0);block.sizeDelta=S("Day Panel").rect.size*2;block.anchoredPosition=new Vector2(-120,0);
            var bg=block.GetComponent<UnityEngine.UI.Image>();bg.sprite=S("Day Panel");bg.color=Color.white;bg.raycastTarget=false;
            var panel=panels.FirstOrDefault(p=>p.GetComponentInParent<Canvas>()==block.GetComponentInParent<Canvas>());if(panel)Align(block,panel);
            var oldAlign=rect.GetComponent<BesideMilitaryPanel>();if(oldAlign)Object.DestroyImmediate(oldAlign);
            rect.SetParent(block,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(-43,4);rect.sizeDelta=S("Day Hourglass").rect.size*2;rect.localScale=Vector3.one;
            button.image.sprite=S("Day Hourglass");button.image.type=UnityEngine.UI.Image.Type.Simple;button.image.color=Color.white;
            var feedback=button.GetComponent<UnifiedButtonFeedback>();if(feedback){var so=new SerializedObject(feedback);so.FindProperty("useHoverGraphicAlpha").boolValue=false;so.FindProperty("preserveGraphicAlpha").boolValue=true;so.FindProperty("animateScale").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
            var outline=button.GetComponent<UnityEngine.UI.Outline>();if(outline)outline.enabled=false;
            foreach(var label in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))label.gameObject.SetActive(false);
            foreach(var label in button.GetComponentsInChildren<TMPro.TMP_Text>(true))label.gameObject.SetActive(false);
            var day=root.GetComponentsInChildren<UnityEngine.UI.Text>(true).FirstOrDefault(t=>t.name=="Day"&&t.GetComponentInParent<Canvas>()==block.GetComponentInParent<Canvas>());
            if(day){day.transform.SetParent(block,false);var d=day.rectTransform;d.anchorMin=d.anchorMax=d.pivot=new Vector2(.5f,.5f);d.anchoredPosition=new Vector2(23,3);d.sizeDelta=new Vector2(74,30);d.localScale=Vector3.one;day.fontSize=16;day.alignment=TextAnchor.MiddleCenter;day.raycastTarget=false;day.gameObject.SetActive(true);}
            var tooltip=button.GetComponent<ResourceTooltipTrigger>()??button.gameObject.AddComponent<ResourceTooltipTrigger>();tooltip.Initialize("Следующий день",rect);
            var dragon=block.Find("Dragon");if(!dragon)dragon=new GameObject("Dragon",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;
            dragon.SetParent(block,false);var dr=(RectTransform)dragon;dr.anchorMin=dr.anchorMax=new Vector2(1,1);dr.pivot=new Vector2(1,0);dr.anchoredPosition=Vector2.zero;dr.sizeDelta=S("Day Dragon").rect.size*2;
            var di=dragon.GetComponent<UnityEngine.UI.Image>();di.sprite=S("Day Dragon");di.raycastTarget=false;
        }
    }
    public static void Apply()
    {
        foreach(string n in new[]{"Day Panel","Day Hourglass","Day Dragon"}){var i=(TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/"+n+".png");i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.SaveAndReimport();}
        foreach(string p in new[]{"Assets/Prefabs/UI/ResourcesUI.prefab","Assets/Prefabs/UI/HUD/Quest Panel.prefab","Assets/Prefabs/UI/HUD/Royal Quest Panel.prefab","Assets/Prefabs/Base/Base Panel.prefab","Assets/Prefabs/Base/Variants/Base Islands Backup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab"})
        {if(!AssetDatabase.LoadAssetAtPath<GameObject>(p))continue;var g=PrefabUtility.LoadPrefabContents(p);try{Configure(g);PrefabUtility.SaveAsPrefabAsset(g,p);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        var start=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        foreach(string p in new[]{"Assets/Scenes/Base.unity","Assets/Scenes/World.unity"}){var scene=EditorSceneManager.OpenScene(p);foreach(var root in scene.GetRootGameObjects())Configure(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        EditorSceneManager.OpenScene(start);AssetDatabase.SaveAssets();
    }
}
#endif
