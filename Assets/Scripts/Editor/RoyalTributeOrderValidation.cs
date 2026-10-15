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
public static class RoyalTributeOrderValidation
{
    const string Key="RoyalTributeOrderValidation";
    static IEnumerator routine;static int checks;
    static readonly Stack<IEnumerator> stack=new();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static RoyalTributeOrderValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Royal Tribute Order")]
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
            Finish("PASS: "+checks+" royal order checks");
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
        yield return Wait(.3f);
        var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameFoundation.Quests.QuestCatalog>("Assets/Resources/Quests/Quest Catalog.asset");
        var royal=catalog.quests.Last();var last=catalog.quests[catalog.quests.Length-2];
        var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quests/00 Royal Tribute.prefab").GetComponent<GameFoundation.Quests.QuestDialogueDefinition>();
        var first=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quests/01 First Supplies.prefab").GetComponent<GameFoundation.Quests.QuestDialogueDefinition>();
        var view=Object.FindFirstObjectByType<GameFoundation.Quests.QuestDialogueView>();
        var available=typeof(GameFoundation.Quests.QuestDialogueView).GetMethod("Available",Private);
        bool Available()=>(bool)available.Invoke(view,new object[]{definition});
        Check(royal.id=="royal_magic_tribute"&&last==UnityEditor.AssetDatabase.LoadAssetAtPath<GameFoundation.Quests.QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset"),"Royal tribute is last after archer quest");
        Check(definition.prerequisite==last&&definition.prerequisiteNextDay&&!definition.prerequisiteAccepted,"Royal dialogue requires claimed final quest and next day");
        Check(first.prerequisite==null,"First supplies no longer depend on royal tribute");
        Check(!Available()&&!GameFoundation.Quests.QuestProgress.IsAccepted(royal),"Scribe not available at game start");
        Check(royal.requirements[0].amount==30,"Existing thirty ore requirement retained");
        foreach(var q in catalog.quests.Where(q=>q!=royal&&q!=last)){
            GameFoundation.Saves.SaveSlotPrefs.SetInt("foundation.quest."+q.id+".completed",1);
            GameFoundation.Saves.SaveSlotPrefs.SetInt("foundation.quest."+q.id+".claimed",1);
        }
        GameFoundation.Saves.SaveSlotPrefs.SetInt("foundation.quest."+last.id+".accepted",1);
        GameFoundation.Saves.SaveSlotPrefs.SetInt("foundation.quest."+last.id+".completed",1);
        int day=DayCycleService.Instance.Day;
        Check(GameFoundation.Quests.QuestProgress.TryClaim(last,GlobalResourceManager.Instance),"Final quest reward really claimed");
        Check(GameFoundation.Saves.SaveSlotPrefs.GetInt("foundation.quest."+last.id+".claimedDay",-1)==day,"Completion day persists in save");
        Check(!Available()&&!definition.PrerequisiteNightPassed,"Scribe blocked for remainder of completion day");
        typeof(GameFoundation.Quests.QuestDialogueView).GetField("current",Private).SetValue(view,null);
        DayCycleService.Instance.NextDay();
        Check(DayCycleService.Instance.Day==day+1&&definition.PrerequisiteNightPassed,"Night unlocks prerequisite");
        if(GameFoundation.Base.DayReportPopup.BlocksQuestDialogues)Check(!Available(),"Day statistics still block dialogue");
        foreach(var popup in Object.FindObjectsByType<GameFoundation.Base.DayReportPopup>(FindObjectsInactive.Include,FindObjectsSortMode.None))popup.Close();
        Check(Available(),"Scribe becomes available after next-day report is closed");
        view.Evaluate();
        Check(typeof(GameFoundation.Quests.QuestDialogueView).GetField("current",Private).GetValue(view) is GameFoundation.Quests.QuestDialogueDefinition d&&d.quest==royal,"Actual next conversation is royal scribe");
        Check(!GameFoundation.Quests.QuestProgress.IsAccepted(royal),"Tribute not silently accepted before conversation");
    }
}
#endif
