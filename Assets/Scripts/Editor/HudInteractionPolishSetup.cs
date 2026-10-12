#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.UI;
using GameFoundation.Quests;
using GameFoundation.MetaProgression;

public static class HudInteractionPolishSetup
{
    static void Relay(GameObject caption, UnityEngine.UI.Button button)
    {
        var relay=caption.GetComponent<ButtonPointerRelay>()??caption.AddComponent<ButtonPointerRelay>(); relay.target=button.gameObject;
        foreach(var graphic in caption.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget=true;
    }
    static void Configure(GameObject root)
    {
        foreach(var view in root.GetComponentsInChildren<WorldMilitaryRosterItemView>(true))
        {
            var im=view.GetComponent<UnityEngine.UI.Image>(); if(!im)continue;
            im.sprite=null; im.type=UnityEngine.UI.Image.Type.Simple; im.color=new Color(.17f,.14f,.20f,.85f);
        }
        foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if(button.name=="Next Day" && button.transform.parent.name=="Day Panel")
            { var day=button.transform.parent.Find("Day");if(day)Relay(day.gameObject,button); }
        }
        foreach(var q in root.GetComponentsInChildren<QuestPanel>(true))
        {
            var title=q.transform.Find("Title");if(!title)continue;
            var expansion=q.GetComponent<QuestPanelExpansion>();if(!expansion)continue;
            var arrow=title.Find("Toggle Quest");
            if(!arrow){arrow=new GameObject("Toggle Quest",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button)).transform;arrow.SetParent(title,false);}
            var rect=(RectTransform)arrow;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(32,32);
            var image=arrow.GetComponent<UnityEngine.UI.Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Day Button Background.png");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=1.5625f;
            var button=arrow.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var feedback=arrow.GetComponent<UnifiedButtonFeedback>()??arrow.gameObject.AddComponent<UnifiedButtonFeedback>();
            var fs=new SerializedObject(feedback);fs.FindProperty("hoverScaleOverride").floatValue=1.08f;fs.ApplyModifiedPropertiesWithoutUndo();
            var glyph=arrow.Find("Arrow");if(!glyph){glyph=new GameObject("Arrow",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;glyph.SetParent(arrow,false);}
            var gi=glyph.GetComponent<UnityEngine.UI.Image>();gi.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Journal Arrow.png");gi.raycastTarget=false;
            var gr=(RectTransform)glyph;gr.anchorMin=gr.anchorMax=gr.pivot=new Vector2(.5f,.5f);gr.sizeDelta=gi.sprite.rect.size*2;gr.anchoredPosition=Vector2.zero;
            var text=title.GetComponent<UnityEngine.UI.Text>();text.raycastTarget=true;
            var outline=title.GetComponent<UnityEngine.UI.Shadow>();
            var padding=title.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()??title.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();padding.enabled=false;
            // Text uses a left inset while the arrow stays in the title's original slot.
            text.alignment=TextAnchor.MiddleLeft;
            var tr=(RectTransform)title;tr.sizeDelta=new Vector2(408,32);
            var inset=title.Find("Title Label");if(!inset){inset=Object.Instantiate(title.gameObject,title,false).transform;inset.name="Title Label";foreach(Transform child in inset)Object.DestroyImmediate(child.gameObject);foreach(var component in inset.GetComponents<Component>())if(component is not RectTransform && component is not UnityEngine.UI.Text && component is not CanvasRenderer)Object.DestroyImmediate(component);}
            var it=(RectTransform)inset;it.anchorMin=Vector2.zero;it.anchorMax=Vector2.one;it.offsetMin=new Vector2(40,0);it.offsetMax=Vector2.zero;
            var inner=inset.GetComponent<UnityEngine.UI.Text>();inner.text=text.text;inner.raycastTarget=false;
            text.color=new Color(text.color.r,text.color.g,text.color.b,0);
            var binding=title.GetComponent<QuestTitleMirror>()??title.gameObject.AddComponent<QuestTitleMirror>();binding.source=text;binding.target=inner;
            var so=new SerializedObject(expansion);so.FindProperty("toggleButton").objectReferenceValue=button;so.FindProperty("toggleArrow").objectReferenceValue=gr;so.FindProperty("showObjectives").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        }
        if(root.name.StartsWith("Red"))foreach(var hp in root.GetComponentsInChildren<HealthBar>(true))
        {
            hp.transform.localScale=new Vector3(.13f,.20f,1);
            var p=hp.transform.localPosition;p.y=.28f;p.x=0;hp.transform.localPosition=p;
        }
        foreach(var c in root.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }
    static void Scene()
    {
        foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())Configure(root);
        var journal=Object.FindAnyObjectByType<JournalHudLayout>();if(journal)
        {
            var s=new SerializedObject(journal);s.FindProperty("expanded").boolValue=false;s.FindProperty("enemies").objectReferenceValue=Object.FindAnyObjectByType<EnemyRosterView>();s.ApplyModifiedPropertiesWithoutUndo();
            var button=(UnityEngine.UI.Button)s.FindProperty("toggleButton").objectReferenceValue;
            var title=journal.transform.Find("Title");if(title)Relay(title.gameObject,button);
        }
        var enemy=Object.FindAnyObjectByType<EnemyRosterView>();if(enemy)
        {var beside=enemy.GetComponent<BesideMilitaryPanel>();if(beside)beside.enabled=false;}
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
    }
    public static void Apply()
    {
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(id);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentInChildren<QuestPanel>(true)&&!asset.GetComponentInChildren<WorldMilitaryRosterItemView>(true)&&!asset.name.StartsWith("Red")&&!asset.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name=="Next Day"))continue;
            var root=PrefabUtility.LoadPrefabContents(path);try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var name in new[]{"Base","World"}){EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Scene();}
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);AssetDatabase.SaveAssets();
    }
}
#endif
