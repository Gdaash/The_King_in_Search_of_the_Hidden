#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DialogueHoldAppearanceValidation
{
    const string Key="DialogueHoldAppearanceValidation";
    static IEnumerator routine;static int checks;
    static readonly Stack<IEnumerator> stack=new();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static DialogueHoldAppearanceValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Dialogue Hold Appearance")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        SessionState.SetString(Key+".scene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");
        SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");BuildingUpgradeValidation.Begin();
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){checks=0;routine=Audit();stack.Clear();stack.Push(routine);EditorApplication.update+=Tick;}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;routine=null;SessionState.SetBool(Key,false);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+".scene",""));
        }
    }
    static void Tick()
    {
        try
        {
            while(stack.Count>0)
            {
                var next=stack.Peek();
                if(!next.MoveNext()){stack.Pop();continue;}
                if(next.Current is IEnumerator nested){stack.Push(nested);continue;}
                return;
            }
            Finish("PASS: "+checks+" dialogue hold checks");
        }
        catch(Exception e){Debug.LogException(e);Finish("FAILED: "+e.Message);}
    }
    static void Finish(string result){EditorApplication.update-=Tick;routine=null;SessionState.SetString(Key+".result",result);Debug.Log(result);EditorApplication.isPlaying=false;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("Portal weapon audit PASS: "+message);}
    static IEnumerator Wait(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static Health Enemy(Vector2 position)
    {
        var g=new GameObject("Roguelike test enemy");g.tag="Enemy1";g.transform.position=position;
        var c=g.AddComponent<CircleCollider2D>();c.radius=.2f;c.isTrigger=true;
        var body=g.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
        var h=g.AddComponent<Health>();h.OnDeath=new UnityEvent();h.OnHealthChanged=new UnityEvent<float>();return h;
    }
    static IEnumerator Audit()
    {
        yield return Wait(.2f);
        var canvas=Object.FindFirstObjectByType<Canvas>();
        foreach(string path in new[]{"Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab","Assets/Prefabs/UI/Dialogue/First Run Light Tutorial.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Component view=root.GetComponent<GameFoundation.Quests.QuestDialogueView>();
            if(view==null)view=root.GetComponent<FirstRunLightTutorial>();
            var template=(UnityEngine.UI.Button)new SerializedObject(view).FindProperty("nextButton").objectReferenceValue;
            var clone=Object.Instantiate(template.gameObject,canvas.transform,false);clone.SetActive(true);
            var button=clone.GetComponent<UnityEngine.UI.Button>();button.interactable=true;
            var hold=clone.GetComponent<GameFoundation.Quests.DialogueHoldButton>();
            bool ready=false;int complete=0;
            hold.Configure(()=>ready,()=>{complete++;ready=false;},.7f,new Color(1,.85f,.05f,1));
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            hold.OnPointerEnter(pointer);
            var tip=clone.transform.Find("Hold LMB Tooltip");
            Check(tip!=null&&!tip.gameObject.activeSelf,path+" hides hint while not ready");
            ready=true;yield return Wait(.05f);
            Check(tip.gameObject.activeSelf,"Hover updates hint when typing ends");
            var label=tip.GetComponentInChildren<TMPro.TMP_Text>();Canvas.ForceUpdateCanvases();label.ForceMeshUpdate();
            var icon=tip.Find("LMB Icon").GetComponent<UnityEngine.UI.Image>();
            Check(label.text=="Зажми"&&icon.sprite!=null&&icon.rectTransform.sizeDelta==icon.sprite.rect.size*2,"Tooltip contains native-size LMB sprite beside hold text");
            Check(label.preferredWidth<=label.rectTransform.rect.width+1&&label.preferredHeight<=label.rectTransform.rect.height+1,"Tooltip text and icon fit");
            var image=clone.transform.Find("Hold Progress/Button Fill").GetComponent<UnityEngine.UI.Image>();
            var color=image.material.GetColor("_Color");
            Check(color.r==1&&color.g>=.8f&&color.b<.1f&&image.color==Color.white,"Bright yellow replaces source color instead of dark multiply");
            Check(image.sprite==((UnityEngine.UI.Image)button.targetGraphic).sprite,"Hold fill preserves button sprite");
            hold.OnPointerDown(pointer);yield return Wait(.25f);
            Check(hold.Progress>.1f&&hold.Progress<1&&complete==0&&image.gameObject.activeInHierarchy,"Holding progressively reveals yellow fill");
            hold.OnPointerExit(pointer);Check(hold.Progress==0&&!tip.gameObject.activeSelf,"Pointer exit cancels fill and hides hint");
            pointer.button=UnityEngine.EventSystems.PointerEventData.InputButton.Right;hold.OnPointerDown(pointer);yield return Wait(.1f);
            Check(hold.Progress==0&&complete==0,"Right click does not hold");
            pointer.button=UnityEngine.EventSystems.PointerEventData.InputButton.Left;hold.OnPointerEnter(pointer);hold.OnPointerDown(pointer);yield return Wait(.8f);
            Check(complete==1&&hold.Progress==0&&!tip.gameObject.activeSelf,"Complete hold fires once and hides hint when next line starts");
            Check(hold.ConsumeClick()&&!hold.ConsumeClick(),"Completion consumes exactly one subsequent click");
            ready=true;hold.OnPointerEnter(pointer);clone.SetActive(false);
            Check(!tip.gameObject.activeSelf,"Disabling dialogue hides tooltip");
            Object.Destroy(clone);yield return null;
        }
    }
}
#endif
