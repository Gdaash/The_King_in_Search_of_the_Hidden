#if UNITY_EDITOR
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.Quests;
using GameFoundation.UI;

public static class HudAlignmentSetup
{
    static void Artwork()
    {
        var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes("Assets/Sprites/Ui/Alarm 12/Bars Connector.png"));
        if(source.width!=71){Object.DestroyImmediate(source);return;}
        var frame=new Texture2D(2,2);frame.LoadImage(File.ReadAllBytes("Assets/Sprites/Ui/Alarm 12/Frame.png"));
        var palette=frame.GetPixels32().Where(c=>c.a==255).Distinct().ToArray();
        var output=new Texture2D(27,19,TextureFormat.RGBA32,false);
        var pixels=new Color32[27*19];
        for(int y=0;y<19;y++)for(int x=4;x<23;x++)
        {
            var c=source.GetPixel(30+Mathf.FloorToInt((x-4+.5f)*11/19),Mathf.Min(11,Mathf.FloorToInt((y+.5f)*12/19)));
            if(c.a<.5f)continue;
            Color32 best=palette[0];float distance=float.MaxValue;
            foreach(var p in palette){float d=(p.r/255f-c.r)*(p.r/255f-c.r)+(p.g/255f-c.g)*(p.g/255f-c.g)+(p.b/255f-c.b)*(p.b/255f-c.b);if(d<distance){distance=d;best=p;}}
            pixels[y*27+x]=best;
        }
        // Copy the exact three horizontal lines from the frame's pointed end.
        for(int x=0;x<27;x++)if(x<6||x>20)for(int y=8;y<=12;y++)pixels[y*27+x]=frame.GetPixel(215,y+1);
        output.SetPixels32(pixels);output.Apply();File.WriteAllBytes("Assets/Sprites/Ui/Alarm 12/Bars Connector.png",output.EncodeToPNG());
        Object.DestroyImmediate(source);Object.DestroyImmediate(frame);Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset("Assets/Sprites/Ui/Alarm 12/Bars Connector.png",ImportAssetOptions.ForceUpdate);
    }
    static void Configure(GameObject root,bool isBase)
    {
        foreach(var layout in root.GetComponentsInChildren<BottomRunBarsLayout>(true)){var connector=layout.transform.parent.Find("Bars Connector");if(connector)Object.DestroyImmediate(connector.gameObject);Canvas.ForceUpdateCanvases();layout.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);}
        foreach(var panel in root.GetComponentsInChildren<VerticalMilitaryPanel>(true)){if(!panel.GetComponent<CanvasGroup>())panel.gameObject.AddComponent<CanvasGroup>();var s=new SerializedObject(panel);s.FindProperty("cellSpacing").vector2Value=new Vector2(8,8);s.ApplyModifiedPropertiesWithoutUndo();panel.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);}
        foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))if(text.name=="Title"&&text.transform.parent.name=="Base Panel"&&text.text.ToLower().Contains("укрытие"))Object.DestroyImmediate(text.gameObject);
        foreach(var q in root.GetComponentsInChildren<QuestPanel>(true))
        {
            var summary=q.transform.Find("Resource Summary");if(!summary)continue;
            var grid=summary.GetComponent<UnityEngine.UI.GridLayoutGroup>();if(grid)grid.enabled=false;
            var element=summary.GetComponent<UnityEngine.UI.LayoutElement>();if(!element)element=summary.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight=element.preferredHeight=80;element.flexibleHeight=0;
            if(!summary.GetComponent<QuestSummarySpacing>())summary.gameObject.AddComponent<QuestSummarySpacing>();
            summary.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
        }
        if(isBase)foreach(var journal in root.GetComponentsInChildren<JournalHudLayout>(true))((RectTransform)journal.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,400);
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name=="Settings"))
        {
            var parent=b.transform.parent;
            var panel=parent.name=="Settings Panel"?parent as RectTransform:parent.Find("Settings Panel") as RectTransform;
            if(!panel){panel=new GameObject("Settings Panel",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();panel.SetParent(parent,false);}
            var day=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab").transform.Find("Day Panel");
            var image=panel.GetComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(day.GetComponent<UnityEngine.UI.Image>(),image);image.raycastTarget=false;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(64,76.8f);
            var backdrop=panel.Find("Backdrop") as RectTransform;if(!backdrop){backdrop=new GameObject("Backdrop",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();backdrop.SetParent(panel,false);}
            var backImage=backdrop.GetComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(image,backImage);backImage.enabled=true;backImage.raycastTarget=false;image.enabled=false;
            backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;backdrop.localScale=new Vector3(1,-1,1);backdrop.SetAsFirstSibling();
            b.transform.SetParent(panel,false);var rect=(RectTransform)b.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-4);rect.sizeDelta=new Vector2(48,48);rect.localScale=Vector3.one;
            foreach(var icon in b.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(icon.transform!=b.transform&&icon.sprite)icon.rectTransform.sizeDelta=icon.sprite.rect.size*2;
        }
        foreach(var align in root.GetComponentsInChildren<BesideMilitaryPanel>(true))if(align.enabled)align.Align();
        foreach(var c in root.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }
    public static void Apply()
    {
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/UI","Assets/Prefabs/Base"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(id);var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!a.GetComponentInChildren<QuestPanel>(true)&&!a.GetComponentInChildren<BottomRunBarsLayout>(true)&&!a.GetComponentInChildren<VerticalMilitaryPanel>(true)&&!a.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name=="Settings"))continue;
            var root=PrefabUtility.LoadPrefabContents(path);try{Configure(root,path.EndsWith("Base Panel.prefab"));PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(string name in new[]{"Base","World"}){var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");foreach(var root in scene.GetRootGameObjects())Configure(root,name=="Base");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);AssetDatabase.SaveAssets();
    }
}
#endif
