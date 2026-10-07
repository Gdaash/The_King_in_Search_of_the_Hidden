using System.Linq;
using GameFoundation.Quests;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class QuestSetup
{
    public const string PanelPath = "Assets/Prefabs/UI/HUD/Quest Panel.prefab";
    public const string QuestPath = "Assets/Resources/Quests/First Expedition.asset";
    public const string CatalogPath = "Assets/Resources/Quests/Quest Catalog.asset";
    const string RowPath = "Assets/Prefabs/UI/HUD/Quest Objective Row.prefab";
    static Font font;
    static Color cream = new(.95f,.91f,.8f);
    static void Set(Object o, string field, Object value)
    { var s=new SerializedObject(o); s.FindProperty(field).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static GameObject UI(string name, Transform parent)
    { var go=new GameObject(name,typeof(RectTransform)); if(parent!=null)go.transform.SetParent(parent,false); return go; }
    static void Height(GameObject go,float height)
    { var l=go.AddComponent<LayoutElement>();l.minHeight=height;l.preferredHeight=height; }
    static Text Text(string name,Transform parent,string value,int size,float height)
    {
        var go=UI(name,parent);var t=go.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;
        t.color=cream;t.raycastTarget=false;t.lineSpacing=1.15f;t.supportRichText=false;
        t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
        Height(go,height);return t;
    }
    static void Vertical(GameObject go,int padding=0)
    {
        var layout=go.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(padding,padding,padding,padding);
        layout.spacing=6;layout.childControlWidth=true;layout.childControlHeight=true;
        layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        go.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
    }
    public static void Create()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Quests"))AssetDatabase.CreateFolder("Assets/Resources","Quests");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
        var quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.id="first_expedition_supplies";
        quest.title="Запасы для укрытия";
        quest.description="Соберите еду на день и материалы для кузницы, стрельбища и первого лучника.";
        quest.requirements.Add(new QuestDefinition.Requirement{resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset"),amount=3,purpose="Еда на ближайший день"});
        quest.requirements.Add(new QuestDefinition.Requirement{resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset"),amount=3,purpose="Два здания и один лук"});
        quest.requirements.Add(new QuestDefinition.Requirement{resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Stone.asset"),amount=2,purpose="Кузница и стрельбище"});
        AssetDatabase.CreateAsset(quest,QuestPath);
        var catalog=ScriptableObject.CreateInstance<QuestCatalog>();catalog.quests=new[]{quest};AssetDatabase.CreateAsset(catalog,CatalogPath);
        var row=UI("Quest Objective Row",null);((RectTransform)row.transform).sizeDelta=new Vector2(328,64);Height(row,64);
        var bg=row.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.17f,.14f,.2f,.85f);bg.raycastTarget=false;
        var h=row.AddComponent<HorizontalLayoutGroup>();h.padding=new RectOffset(8,8,4,4);h.spacing=8;h.childAlignment=TextAnchor.MiddleLeft;
        h.childControlWidth=true;h.childControlHeight=true;h.childForceExpandWidth=false;h.childForceExpandHeight=false;
        var slot=UI("Icon Slot",row.transform);var sl=slot.AddComponent<LayoutElement>();sl.preferredWidth=56;sl.preferredHeight=56;
        var icon=UI("Resource Icon",slot.transform).AddComponent<UnityEngine.UI.Image>();icon.raycastTarget=false;icon.preserveAspect=true;
        var count=Text("Amount",row.transform,"0 / 3",22,52);count.alignment=TextAnchor.MiddleLeft;count.GetComponent<LayoutElement>().preferredWidth=72;
        var purpose=Text("Purpose",row.transform,"Еда на ближайший день",17,52);purpose.alignment=TextAnchor.MiddleLeft;purpose.GetComponent<LayoutElement>().flexibleWidth=1;
        var view=row.AddComponent<QuestObjectiveRow>();Set(view,"resourceIcon",icon);Set(view,"amount",count);Set(view,"purpose",purpose);
        view.Present(quest.requirements[0],0,false);
        var rowAsset=PrefabUtility.SaveAsPrefabAsset(row,RowPath);Object.DestroyImmediate(row);
        var panel=UI("Quest Panel",null);var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;
        rect.sizeDelta=new Vector2(360,420);rect.anchoredPosition=new Vector2(-132,-84);
        bg=panel.AddComponent<UnityEngine.UI.Image>();bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Evolution/Popups/Sprites/Shared/PopupPanel9Slice.png");bg.type=UnityEngine.UI.Image.Type.Sliced;bg.raycastTarget=false;
        Vertical(panel,16);var group=panel.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var heading=Text("Heading",panel.transform,"ЗАДАНИЕ",16,20);heading.color=new Color(.72f,.68f,.77f);
        var title=Text("Title",panel.transform,quest.title,24,32);title.color=new Color(1,.86f,.51f);
        var desc=Text("Description",panel.transform,quest.description,18,66);
        var separator=UI("Separator",panel.transform);separator.AddComponent<UnityEngine.UI.Image>().color=new Color(.4f,.34f,.45f);separator.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;Height(separator,2);
        var goals=UI("Objectives",panel.transform);Vertical(goals);
        var status=Text("Status",panel.transform,"Доставьте ресурсы в портал",16,26);status.color=new Color(.72f,.68f,.77f);
        var script=panel.AddComponent<QuestPanel>();Set(script,"catalog",catalog);Set(script,"title",title);Set(script,"description",desc);Set(script,"status",status);Set(script,"objectives",goals.transform);Set(script,"rowPrefab",rowAsset.GetComponent<QuestObjectiveRow>());
        var data=new SerializedObject(script);var rows=data.FindProperty("rows");rows.arraySize=quest.requirements.Count;
        for(int i=0;i<quest.requirements.Count;i++){var instance=(GameObject)PrefabUtility.InstantiatePrefab(rowAsset,goals.transform);var v=instance.GetComponent<QuestObjectiveRow>();v.Present(quest.requirements[i],0,false);rows.GetArrayElementAtIndex(i).objectReferenceValue=v;}
        data.ApplyModifiedPropertiesWithoutUndo();var align=panel.AddComponent<BesideMilitaryPanel>();data=new SerializedObject(align);data.FindProperty("gap").floatValue=12;data.FindProperty("preserveVerticalPosition").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);PrefabUtility.SaveAsPrefabAsset(panel,PanelPath);Object.DestroyImmediate(panel);
        // Keep one existing resource manager as the quest owner; it already persists between scenes.
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentInChildren<GlobalResourceManager>(true)==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try { foreach(var manager in root.GetComponentsInChildren<GlobalResourceManager>(true))ConfigureManager(manager,catalog);PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }
    static void ConfigureManager(GlobalResourceManager manager, QuestCatalog catalog)
    {
        var tracker=manager.GetComponent<QuestTracker>();if(tracker==null)tracker=manager.gameObject.AddComponent<QuestTracker>();Set(tracker,"catalog",catalog);
        var data=new SerializedObject(manager);var list=data.FindProperty("initialValues");
        var food=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");
        for(int i=0;i<list.arraySize;i++){var entry=list.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("resourceType").objectReferenceValue==food)entry.FindPropertyRelative("startAmount").intValue=0;}
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Install()
    {
        var active=SceneManager.GetActiveScene();
        foreach(var path in new[]{"Assets/Scenes/Base.unity","Assets/Scenes/World.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var roots=scene.GetRootGameObjects();
            var military=roots.SelectMany(root=>root.GetComponentsInChildren<VerticalMilitaryPanel>(true)).First(p=>p.gameObject.activeInHierarchy);
            var canvas=military.GetComponentInParent<Canvas>().rootCanvas;
            var panel=roots.SelectMany(root=>root.GetComponentsInChildren<QuestPanel>(true)).FirstOrDefault();
            if(panel==null)panel=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath),canvas.transform)).GetComponent<QuestPanel>();
            Set(panel.GetComponent<BesideMilitaryPanel>(),"panel",military);panel.GetComponent<BesideMilitaryPanel>().Align();
            foreach(var manager in roots.SelectMany(root=>root.GetComponentsInChildren<GlobalResourceManager>(true)))ConfigureManager(manager,AssetDatabase.LoadAssetAtPath<QuestCatalog>(CatalogPath));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
    }
    public static void ValidateAndRender(bool expanded = false)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D image=null;Camera camera=null;var previous=RenderTexture.active;
        try
        {
            var go=new GameObject("Quest preview camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=250;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.05f,.075f);
            var canvas=new GameObject("Preview Canvas",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scene);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath),canvas.transform);var rect=(RectTransform)panel.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(rect);Canvas.ForceUpdateCanvases();
            panel.GetComponent<QuestPanelExpansion>()?.SetExpanded(expanded,true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);Canvas.ForceUpdateCanvases();
            foreach(var t in panel.GetComponentsInChildren<Text>())
                if(t.preferredHeight>t.rectTransform.rect.height+.5f)throw new System.Exception("Text clipped: "+t.name+" "+t.preferredHeight+" > "+t.rectTransform.rect.height);
            foreach(var row in panel.GetComponentsInChildren<QuestObjectiveRow>())
            {
                var icon=(UnityEngine.UI.Image)new SerializedObject(row).FindProperty("resourceIcon").objectReferenceValue;
                if(icon.rectTransform.sizeDelta!=icon.sprite.rect.size*2)throw new System.Exception("Icon scale mismatch");
            }
            rt=new RenderTexture(440,500,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(440,500,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,440,500),0,0);image.Apply();System.IO.File.WriteAllBytes(expanded?"Temp/QuestPanelExpanded.png":"Temp/QuestPanelPreview.png",image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;if(camera!=null)camera.targetTexture=null;if(rt!=null)Object.DestroyImmediate(rt);if(image!=null)Object.DestroyImmediate(image);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
