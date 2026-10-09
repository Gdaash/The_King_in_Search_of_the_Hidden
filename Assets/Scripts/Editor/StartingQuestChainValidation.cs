#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Quests;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StartingQuestChainValidation
{
    const string Key="StartingQuestChainValidation";
    static IEnumerator routine;
    static readonly System.Collections.Generic.Stack<IEnumerator> stack=new();
    static readonly System.Collections.Generic.List<string> checks=new();
    static StartingQuestChainValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");
        SessionState.SetString(Key+".scene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");SessionState.SetBool(Key,true);
        BuildingUpgradeValidation.Begin();
        SaveSlotPrefs.SetInt("Global_Resource_Wood",6);SaveSlotPrefs.SetInt("Global_Resource_Stone",3);SaveSlotPrefs.SetInt("Global_Resource_Berry",3);SaveSlotPrefs.SetInt("Global_Resource_Bow",0);SaveSlotPrefs.SetInt("Global_Resource_Crown",0);SaveSlotPrefs.Save();
    }
    static void Changed(PlayModeStateChange s)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){checks.Clear();routine=Test();stack.Clear();stack.Push(routine);EditorApplication.update+=Tick;}
        if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+".scene",""));BuildingUpgradeValidation.Restore();SessionState.SetBool(Key,false);}
    }
    static void Tick(){try{while(stack.Count>0){var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}if(top.Current is IEnumerator nested){stack.Push(nested);continue;}return;}checks.Add("COMPLETE");}catch(Exception e){checks.Add("FAIL "+e);}Directory.CreateDirectory("Temp");File.WriteAllLines("Temp/StartingQuestChainValidation.txt",checks);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
    static void Check(bool b,string message){if(!b)throw new Exception(message);checks.Add("PASS "+message);}
    static UnityEngine.Object Field(UnityEngine.Object o,string n)=>new SerializedObject(o).FindProperty(n).objectReferenceValue;
    static IEnumerator Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until || GameObject.Find("Accepted Quest Presentation")!=null){var ok=GameObject.Find("Quest Presentation OK");if(ok!=null)ok.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;}yield return null;}
    static void ReadAndAdvance(QuestDialogueView view){if(view.IsTyping)view.Advance();view.Advance();}
    static IEnumerator Test()
    {
        yield return null;yield return null;
        var catalog=AssetDatabase.LoadAssetAtPath<QuestCatalog>(QuestSetup.CatalogPath);var first=catalog.quests[0];var craft=catalog.quests[1];var archers=catalog.quests[2];
        var view=UnityEngine.Object.FindFirstObjectByType<QuestDialogueView>();var panel=UnityEngine.Object.FindObjectsByType<QuestPanel>(FindObjectsSortMode.None).First(p=>!RoyalTributeSetup.IsRoyal(p));var manager=GlobalResourceManager.Instance;
        var sounds=new System.Collections.Generic.Dictionary<GameFoundation.Audio.GameAudioCue,int>();
        void Record(GameFoundation.Audio.GameAudioCue cue){sounds.TryGetValue(cue,out int count);sounds[cue]=count+1;}
        GameFoundation.Audio.GameAudioController.Instance.CuePlayed+=Record;
        var buildings=UnityEngine.Object.FindObjectsByType<BaseBuildingConstruction>(FindObjectsSortMode.None);
        var smith=buildings.First(b=>b.BuildingId=="blacksmith");var range=buildings.First(b=>b.BuildingId=="archery_range");var camp=buildings.First(b=>b.BuildingId=="refugees");var housing=buildings.First(b=>b.BuildingId=="housing");
        Check(!smith.ConstructionUnlocked && !range.ConstructionUnlocked && !housing.ConstructionUnlocked && !camp.ConstructionUnlocked,"smithy, range, housing and camp locked at start");
        Check(!QuestProgress.IsAccepted(craft) && !QuestProgress.IsAccepted(archers),"later quests not accepted automatically");
        smith.ConfirmBuild();Check(!smith.IsBuilt,"locked smithy cannot be built");
        var royal=catalog.quests.First(q=>q.parallelQuest);ReadAndAdvance(view);ReadAndAdvance(view);yield return Wait(2.5f);
        Check(QuestProgress.IsAccepted(royal) && !QuestProgress.IsClaimed(royal),"royal quest accepted first and stays active alongside tutorial");
        ReadAndAdvance(view);ReadAndAdvance(view);yield return Wait(2.5f);
        Check(QuestProgress.IsComplete(first) && !smith.ConstructionUnlocked,"first quest completed but smithy locked until reward claim");
        ((Button)Field(panel,"claimButton")).onClick.Invoke();
        Check(smith.ConstructionUnlocked && !range.ConstructionUnlocked && manager.GetResourceAmount(AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Crown.asset"))==0,"first reward unlocks smithy without influence");
        ReadAndAdvance(view);yield return Wait(2.5f);
        Check(QuestProgress.Current(catalog)==craft && !QuestProgress.IsComplete(craft),"second quest is build smithy and craft two bows; actual="+(QuestProgress.Current(catalog)?.id??"none")+" accepted="+QuestProgress.IsAccepted(craft));
        smith.ConfirmBuild();Check(smith.IsBuilt && !QuestProgress.IsComplete(craft),"smithy construction alone does not complete second quest");
        var ui=UnityEngine.Object.FindFirstObjectByType<BaseUIController>();ui.OpenBlacksmith();yield return null;
        var production=UnityEngine.Object.FindFirstObjectByType<BlacksmithProductionView>();
        var recipe=new SerializedObject(production).FindProperty("bowRecipe");var button=(Button)recipe.FindPropertyRelative("produceButton").objectReferenceValue;var bow=(ResourceType)recipe.FindPropertyRelative("output").objectReferenceValue;
        button.onClick.Invoke();Check(manager.GetResourceAmount(bow)==1 && !QuestProgress.IsComplete(craft),"one crafted bow does not complete quest");
        button.onClick.Invoke();Check(manager.GetResourceAmount(bow)==2 && QuestProgress.IsComplete(craft) && !range.ConstructionUnlocked,"two crafted bows complete quest, range still locked");ui.CloseBlacksmith();
        ((Button)Field(panel,"claimButton")).onClick.Invoke();Check(range.ConstructionUnlocked && !camp.ConstructionUnlocked,"second reward unlocks range only");
        ReadAndAdvance(view);yield return Wait(2.5f);Check(QuestProgress.Current(catalog)==archers,"third quest is existing two archers quest");
        range.ConfirmBuild();Check(range.IsBuilt,"initial supplies suffice to build range after crafting");
        var archer=archers.requirements[0].resource;var training=UnityEngine.Object.FindObjectsByType<MilitaryTrainingView>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>Field(t,"warrior")==archer);
        training.Arm();Check(manager.GetResourceAmount(archer)==1 && !QuestProgress.IsComplete(archers),"first archer insufficient");
        training.Arm();Check(manager.GetResourceAmount(archer)==2 && QuestProgress.IsComplete(archers) && !camp.ConstructionUnlocked,"two archers complete third quest without automatic camp unlock");
        ((Button)Field(panel,"claimButton")).onClick.Invoke();Check(camp.ConstructionUnlocked && !housing.ConstructionUnlocked,"third reward unlocks camp while housing remains locked");
        int people=manager.GetResourceAmount(archer);training.Disarm();Check(manager.GetResourceAmount(archer)==people-1 && manager.GetResourceAmount(bow)==1,"disarming returns bow and resident");
        yield return null;training.Arm();Check(manager.GetResourceAmount(archer)==people && manager.GetResourceAmount(bow)==0,"rearming does not duplicate people or weapons");
        var day=DayCycleService.Instance;day.NextDay();camp.ConfirmBuild();Check(camp.IsBuilt && day.RefugeesAvailable==2,"first camp arrival exactly two even after next day");
        ui.AdmitRefugee();ui.AdmitRefugee();Check(day.RefugeesAvailable==0,"exactly two refugees can be admitted");day.EnsureFirstRefugees();Check(day.RefugeesAvailable==0,"first arrival cannot refill twice");
        Check(QuestProgress.Current(catalog)==null && QuestProgress.IsClaimed(first)&&QuestProgress.IsClaimed(craft)&&QuestProgress.IsClaimed(archers),"all three quests passed sequentially and claimed");
        Check(!QuestProgress.TryClaim(first,manager)&&!QuestProgress.TryClaim(craft,manager)&&!QuestProgress.TryClaim(archers,manager),"rewards cannot be claimed twice");
        yield return Wait(1.8f);
        foreach(var cue in new[]{GameFoundation.Audio.GameAudioCue.DialogueWord,GameFoundation.Audio.GameAudioCue.DialogueNext,GameFoundation.Audio.GameAudioCue.DialogueOpen,GameFoundation.Audio.GameAudioCue.QuestAccept,GameFoundation.Audio.GameAudioCue.QuestComplete,GameFoundation.Audio.GameAudioCue.QuestReward,GameFoundation.Audio.GameAudioCue.QuestFly,GameFoundation.Audio.GameAudioCue.ContentUnlock,GameFoundation.Audio.GameAudioCue.BuildingComplete,GameFoundation.Audio.GameAudioCue.ProductionComplete,GameFoundation.Audio.GameAudioCue.RecruitWarrior,GameFoundation.Audio.GameAudioCue.DisarmWarrior,GameFoundation.Audio.GameAudioCue.RefugeesArrival,GameFoundation.Audio.GameAudioCue.RefugeeAdmit})
            Check(sounds.TryGetValue(cue,out int count)&&count>0,"audio playback reached AudioSource: "+cue);
        GameFoundation.Audio.GameAudioController.Instance.CuePlayed-=Record;
    }
}
#endif
