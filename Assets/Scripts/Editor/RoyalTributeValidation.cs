#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RoyalTributeValidation
{
    const string Key="RoyalTributeValidation";
    static readonly System.Collections.Generic.Stack<IEnumerator> stack=new();
    static readonly System.Collections.Generic.List<string> checks=new();
    static RoyalTributeValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");
        SessionState.SetString(Key+".scene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");SessionState.SetBool(Key,true);BuildingUpgradeValidation.Begin();
        SaveSlotPrefs.SetString("foundation.daycycle","{\"day\":1,\"totalPortalEntries\":0,\"refugees\":2}");SaveSlotPrefs.SetInt("Global_Resource_MagicOre",0);SaveSlotPrefs.SetInt("Global_Resource_Crown",5);SaveSlotPrefs.Save();
    }
    static void Changed(PlayModeStateChange s)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){checks.Clear();stack.Clear();stack.Push(Test());EditorApplication.update+=Tick;}
        if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+".scene",""));BuildingUpgradeValidation.Restore();SessionState.SetBool(Key,false);}
    }
    static void Tick(){try{while(stack.Count>0){var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}if(top.Current is IEnumerator nested){stack.Push(nested);continue;}return;}checks.Add("COMPLETE");}catch(Exception e){checks.Add("FAIL "+e);}Directory.CreateDirectory("Temp");File.WriteAllLines("Temp/RoyalTributeValidation.txt",checks);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
    static void Check(bool b,string text){if(!b)throw new Exception(text);checks.Add("PASS "+text);}
    static IEnumerator Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until || GameObject.Find("Accepted Quest Presentation")!=null){var ok=GameObject.Find("Quest Presentation OK");if(ok!=null)ok.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;}yield return null;}
    static void Read(QuestDialogueView v){if(v.IsTyping)v.Advance();v.Advance();}
    static UnityEngine.Object Field(UnityEngine.Object o,string n)=>new SerializedObject(o).FindProperty(n).objectReferenceValue;
    static IEnumerator Test()
    {
        yield return null;yield return null;
        var catalog=AssetDatabase.LoadAssetAtPath<QuestCatalog>(QuestSetup.CatalogPath);var q=AssetDatabase.LoadAssetAtPath<QuestDefinition>(RoyalTributeSetup.QuestPath);var resources=GlobalResourceManager.Instance;var day=DayCycleService.Instance;var ore=q.requirements[0].resource;var crown=q.penaltyResource;
        var view=UnityEngine.Object.FindFirstObjectByType<QuestDialogueView>();Check(((TMP_Text)Field(view,"speaker")).text=="Королевский писарь","royal scribe gives first conversation");
        Check(!QuestProgress.IsAccepted(q)&&!QuestProgress.IsAccepted(catalog.quests[0]),"neither royal nor tutorial quest appears before acceptance");Read(view);Read(view);yield return Wait(2.5f);
        Check(QuestProgress.IsAccepted(q)&&QuestProgress.DaysRemaining(q)==30&&QuestProgress.DeadlineDay(q)==31,"royal acceptance starts 30 full days");Check(((TMP_Text)Field(view,"speaker")).text=="Старик Пью","Pew conversation follows royal acceptance without waiting 30 days");Read(view);Read(view);yield return Wait(2.5f);
        Check(QuestProgress.Current(catalog)==catalog.quests[0]&&QuestProgress.Current(catalog,true)==q,"royal and tutorial quests active in parallel");
        var panel=UnityEngine.Object.FindObjectsByType<QuestPanel>(FindObjectsSortMode.None).First(RoyalTributeSetup.IsRoyal);var button=(Button)Field(panel,"claimButton");var label=(Text)Field(panel,"deadlineLabel");
        Check(label.text.Contains("30 / 30"),"remaining days visible in compact HUD");
        var expansion=panel.GetComponent<QuestPanelExpansion>();expansion.SetExpanded(true,true);Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.transform);Canvas.ForceUpdateCanvases();
        foreach(var text in panel.GetComponentsInChildren<Text>())Check(text.preferredHeight<=text.rectTransform.rect.height+.5f,"royal panel text fits: "+text.name);
        expansion.SetExpanded(false,true);
        resources.SetResourceAmount(ore,29);Check(!button.gameObject.activeSelf&&!QuestProgress.TryClaim(q,resources),"29 ore cannot be surrendered");
        resources.SetResourceAmount(ore,34);Check(button.gameObject.activeSelf&&!QuestProgress.IsComplete(q),"ore stock enables surrender but does not complete quest automatically");
        int crowns=resources.GetResourceAmount(crown);button.onClick.Invoke();Check(QuestProgress.IsClaimed(q)&&resources.GetResourceAmount(ore)==4&&resources.GetResourceAmount(crown)==crowns+10,"surrender spends exactly 30 ore and grants exactly 10 influence");
        Check(!QuestProgress.TryClaim(q,resources)&&resources.GetResourceAmount(ore)==4,"duplicate surrender does not spend or reward twice");Check(panel.GetComponent<CanvasGroup>().alpha==0,"claimed royal quest disappears");
        foreach(var suffix in new[]{"accepted","claimed","completed","failed","deadline"})SaveSlotPrefs.SetInt("foundation.quest."+q.id+"."+suffix,0);
        Check(QuestProgress.Accept(q),"independent failure scenario accepts fresh tribute");
        foreach(var resource in resources.AvailableResources)resources.SetResourceAmount(resource,resource.Id=="Human"?3:resource.Id=="Archer"?2:resource.Id=="Swordsman"?1:resource.Id=="Cart"?2:10);
        var food=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");resources.SetResourceAmount(food,200);resources.SetResourceAmount(ore,29);
        var iron=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Iron.asset");resources.AddResource(iron,17);
        for(int i=0;i<29;i++)day.NextDay();Check(day.Day==30&&!QuestProgress.IsFailed(q)&&QuestProgress.DaysRemaining(q)==1,"deadline does not fail early; final day remains usable");
        Check(label.text.Contains("1 / 30"),"HUD countdown updates after day changes");
        resources.SetResourceAmount(ore,30);Check(QuestProgress.IsReadyToClaim(q,resources),"delivery remains available on last day");resources.SetResourceAmount(ore,29);resources.SetResourceAmount(crown,0);day.NextDay();
        Check(day.Day==31&&QuestProgress.IsFailed(q)&&resources.GetResourceAmount(crown)==-29,"expiry subtracts 30 influence even below zero after daily +1");
        foreach(var resource in resources.AvailableResources)if(resource!=crown&&!q.protectedResources.Contains(resource))Check(resources.GetResourceAmount(resource)==0,"confiscated "+resource.Id);
        Check(resources.GetResourceAmount(iron)==0,"confiscation includes resources added outside initial manager list");
        foreach(var pair in new[]{("Human",3),("Archer",2),("Swordsman",1),("Cart",2)})Check(resources.GetResourceAmount(AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+pair.Item1+".asset"))==pair.Item2,"preserved "+pair.Item1);
        int after=resources.GetResourceAmount(crown);Check(!QuestProgress.TryFail(q,resources)&&resources.GetResourceAmount(crown)==after,"penalty cannot apply twice");
        Check(!QuestProgress.TryClaim(q,resources)&&!QuestProgress.Accept(q),"failed quest cannot pay reward or restart");
        Check(SaveSlotPrefs.GetInt("foundation.quest."+q.id+".failed",0)==1&&SaveSlotPrefs.GetInt("Global_Resource_Crown")==after,"failure and negative influence saved in current slot");
    }
}
#endif
