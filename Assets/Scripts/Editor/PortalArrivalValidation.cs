#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortalArrivalValidation
{
    const string Key="PortalArrivalValidation";
    static PortalArrivalValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Portal Arrival")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");BuildingUpgradeValidation.Begin();
    }
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static async void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false) || state!=PlayModeStateChange.EnteredPlayMode)return;
        try
        {
            await Task.Delay(100);
            Check(PortalArrivalOverlay.AlphaAt(0)==1,"Starts opaque");
            Check(Mathf.Approximately(PortalArrivalOverlay.AlphaAt(1),.5f),"Gradual fade");
            Check(PortalArrivalOverlay.AlphaAt(1.45f)==.7f,"First flash");
            Check(Mathf.Approximately(PortalArrivalOverlay.AlphaAt(1.7f),.25f),"Fade resumes after flash");
            Check(PortalArrivalOverlay.AlphaAt(1.95f)==.7f,"Second flash");
            Check(PortalArrivalOverlay.AlphaAt(2.2f)<.11f,"Fade resumes after second flash");
            Check(PortalArrivalOverlay.AlphaAt(2.4f)==0,"Finishes at 2.4 seconds");
            var effect=UnityEngine.Object.FindFirstObjectByType<PortalArrivalOverlay>();
            Check(effect!=null,"Portal prefab has arrival effect");
            var overlay=new SerializedObject(effect).FindProperty("overlay").objectReferenceValue as SpriteRenderer;
            Check(overlay!=null && overlay.sprite.name=="Castle 4" && overlay.sortingOrder>effect.GetComponent<SpriteRenderer>().sortingOrder,"Castle 4 overlays base sprite");
            Check(overlay.enabled && effect.Elapsed<.6f && overlay.color.a>.7f,"Natural scene startup: elapsed="+effect.Elapsed+", enabled="+overlay.enabled+", alpha="+overlay.color.a);
            ScreenCapture.CaptureScreenshot("Temp/PortalArrivalStart.png");
            GameSpeedControls.SetSimulationSpeed(0);
            while(effect.Elapsed<.9f)await Task.Delay(10);
            Check(overlay.color.a<.6f && overlay.color.a>.4f,"Live overlay fades during gameplay pause");
            ScreenCapture.CaptureScreenshot("Temp/PortalArrival.png");
            while(effect.Elapsed<1.45f)await Task.Delay(10);
            Check(Mathf.Approximately(overlay.color.a,.7f),"Live first flash");
            while(effect.Elapsed<1.95f)await Task.Delay(10);
            Check(Mathf.Approximately(overlay.color.a,.7f),"Live second flash");
            while(effect.Elapsed<2.4f)await Task.Delay(10);
            Check(!overlay.enabled && overlay.color.a==0,"Overlay disappears after 2.4 seconds");
            SessionState.SetString(Key+".result","PASS: 14 portal arrival checks");Debug.Log(SessionState.GetString(Key+".result",""));
        }
        catch(Exception e){SessionState.SetString(Key+".result","FAILED: "+e.Message);Debug.LogException(e);}
        finally{SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
    }
}
#endif
