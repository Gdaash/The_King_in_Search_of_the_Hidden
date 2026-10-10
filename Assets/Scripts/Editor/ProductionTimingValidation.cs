#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ProductionTimingValidation
{
    const string Key="ProductionTimingValidation";
    static int checks;
    static ProductionTimingValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Production Timing")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");
        BuildingUpgradeValidation.Begin();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static async void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false) || state!=PlayModeStateChange.EnteredPlayMode)return;
        GameObject go=null;GlobalStats stats=null;checks=0;
        try
        {
            await Task.Delay(400);
            foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Buildings"}))
            {
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));
                foreach(var t in p.GetComponentsInChildren<TimerController>(true))
                {
                    var so=new SerializedObject(t);var source=so.FindProperty("stats").objectReferenceValue as GlobalStats;
                    if(source==null)continue;
                    Check(so.FindProperty("duration").floatValue==15,"Migrated base time: "+p.name);
                    Check(Mathf.Approximately(t.ConfiguredDuration,source.ApplyProductionTimeModifiers(15)),"Modifier reference retained: "+p.name);
                }
            }
            stats=ScriptableObject.CreateInstance<GlobalStats>();stats.bonusProductionSpeed=2;
            go=new GameObject("Production timing test");var timer=go.AddComponent<TimerController>();timer.OnTimerEnd=new UnityEvent();
            var settings=new SerializedObject(timer);settings.FindProperty("runOnStart").boolValue=false;
            settings.FindProperty("duration").floatValue=9;settings.ApplyModifiedPropertiesWithoutUndo();
            Check(timer.ConfiguredDuration==9,"Without stats the prefab owns time");
            settings.FindProperty("stats").objectReferenceValue=stats;settings.ApplyModifiedPropertiesWithoutUndo();
            Check(timer.ConfiguredDuration==7,"Stats modify the prefab base instead of replacing it");
            await Task.Delay(100);
            GameSpeedControls.SetSimulationSpeed(1);timer.ResetTimer();
            Check(timer.ActiveCycleDuration==7,"New cycle starts with modified prefab time");
            settings.FindProperty("duration").floatValue=12;settings.ApplyModifiedPropertiesWithoutUndo();
            Check(timer.ConfiguredDuration==10 && timer.ActiveCycleDuration==7,"Base edits apply to the next cycle");
            timer.ResetTimer();Check(timer.ActiveCycleDuration==10,"Restart picks up new base");
            await Task.Delay(200);Check(timer.TimeRemaining<10 && timer.TimeRemaining>8,"Production countdown runs");
            GameSpeedControls.SetSimulationSpeed(0);float remaining=timer.TimeRemaining;await Task.Delay(150);
            Check(Mathf.Approximately(timer.TimeRemaining,remaining),"Pause freezes production");
            stats.bonusProductionSpeed=100;Check(Mathf.Approximately(timer.ConfiguredDuration,.2f),"Modifiers cannot produce negative duration");
            Check(new SerializedObject(stats).FindProperty("baseProductionTime")==null,"GlobalStats no longer stores production base");
            SessionState.SetString(Key+".result","PASS: "+checks+" production timing checks");
            Debug.Log(SessionState.GetString(Key+".result",""));
        }
        catch(Exception e){SessionState.SetString(Key+".result","FAILED: "+e.Message);Debug.LogException(e);}
        finally
        {
            if(go!=null)Object.Destroy(go);if(stats!=null)Object.Destroy(stats);
            SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
        }
    }
}
#endif
