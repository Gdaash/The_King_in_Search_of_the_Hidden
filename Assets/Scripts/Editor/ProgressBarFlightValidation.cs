#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ProgressBarFlightValidation
{
    const string Key="ProgressBarFlightValidation";
    static ProgressBarFlightValidation()=>EditorApplication.playModeStateChanged+=State;
    public static void Run(){SessionState.SetBool(Key,true);BuildingUpgradeValidation.Begin();}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static async void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
        try
        {
            await Task.Delay(500);GameSpeedControls.SetSimulationSpeed(1);
            var p=PortalTowerProgression.Instance;
            var bar=Object.FindFirstObjectByType<PortalTowerExperienceBar>();
            Check(bar!=null,"Experience bar exists");
            p.AwardHexExperience(Vector3.zero);
            Check(p.Experience==0 && p.PendingExperience==3,"XP reserved, not credited before flight");
            var preview=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(i=>i.name=="Incoming XP");
            Check(preview.fillAmount>0 && preview.sprite==bar.fill.sprite && preview.material==bar.incomingFillMaterial,"Original fill sprite with cyan replacement material");
            await Task.Delay(100);
            var images=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.name=="Flying XP Crystal").ToArray();
            Check(images.Length==3,"Three UI crystals from hex");
            Check(images.All(i=>i.rectTransform.sizeDelta==i.sprite.rect.size*2 && !i.raycastTarget),"Crystal UI size and raycast settings");
            var image=images[0];var canvas=bar.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var target=RectTransformUtility.WorldToScreenPoint(camera,bar.fill.rectTransform.TransformPoint(new Vector3(Mathf.Lerp(bar.fill.rectTransform.rect.xMin,bar.fill.rectTransform.rect.xMax,1f/p.RequiredExperience),bar.fill.rectTransform.rect.center.y,0)));
            float initial=Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera,image.transform.position),target);
            await Task.Delay(550);
            Check(image!=null && Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera,image.transform.position),target)<initial*.6f,"Crystal approaches its XP fill position");
            Time.timeScale=0;await Task.Delay(600);
            Check(p.Experience==3 && p.PendingExperience==0,"Only arrived crystals credit XP, including during pause");
            Check(!Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Any(i=>i.name=="Flying XP Crystal"),"Crystal finishes during pause");
            GameSpeedControls.SetSimulationSpeed(1);
            var alarm=AlarmSystem.Instance;
            typeof(AlarmSystem).GetField("thresholds",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(alarm,new List<AlarmThreshold>{new AlarmThreshold{alarmValue=1,minSpawnInterval=1000,maxSpawnInterval=1000}});
            double waveAt=0,alarmAt=0;
            alarm.OnThresholdReached.AddListener(_=>waveAt=Time.realtimeSinceStartupAsDouble);
            alarm.OnAlarmChanged.AddListener(value=>{if(value>=1 && alarmAt==0)alarmAt=Time.realtimeSinceStartupAsDouble;});
            alarm.AddAlarmFromWorldPosition(8,Vector3.zero);
            p.AwardHexExperience(Vector3.zero);
            Check(alarmAt==0 && waveAt==0,"Alarm and wave wait for arrival");
            await Task.Delay(450);
            ScreenCapture.CaptureScreenshot("Temp/TintedIncomingBars.png");
            var skull=Object.FindObjectsByType<AlarmOrbSettings>(FindObjectsSortMode.None).FirstOrDefault();
            Check(skull!=null && skull.Image.sprite==alarm.SkullSprite,"Flying skull matches active bar sprite");
            Check(skull.GetComponent<RectTransform>().sizeDelta==alarm.SkullSprite.rect.size*2,"Flying skull standard UI size");
            Check(skull.transform.localScale==Vector3.one,"Flying skull keeps full size");
            await Task.Delay(500);Check(skull!=null && skull.transform.localScale==Vector3.one,"Skull does not shrink during flight");
            await Task.Delay(1700);
            Check(alarmAt>0 && waveAt-alarmAt>=.48,"Wave event follows credited alarm by 0.5 seconds");
            p.AddExperience(p.RequiredExperience-p.Experience-1);
            int oldLevel=p.Level;double experienceAt=0,levelAt=0;
            p.Changed+=()=>{if(p.Experience>=p.RequiredExperience && experienceAt==0)experienceAt=Time.realtimeSinceStartupAsDouble;if(p.Level>oldLevel && levelAt==0)levelAt=Time.realtimeSinceStartupAsDouble;};
            p.AwardEnemyExperience(Vector3.zero);
            Check(p.Level==oldLevel && p.PendingExperience==1,"Level waits for crystal arrival");
            await Task.Delay(1000);
            Check(p.Experience>=p.RequiredExperience && p.Level==oldLevel && !PortalTowerProgression.IsChoosingUpgrade,"XP fills before level popup");
            await Task.Delay(700);
            Check(levelAt-experienceAt>=.48 && PortalTowerProgression.IsChoosingUpgrade && Time.timeScale==0,"Level popup pauses after 0.5 seconds");
            SessionState.SetString(Key+".result","PASS: 17 progress bar arrival and delayed event checks");
        }
        catch(Exception e){SessionState.SetString(Key+".result","FAILED: "+e.Message);}
        finally{SessionState.SetBool(Key,false);Debug.Log(SessionState.GetString(Key+".result",""));EditorApplication.isPlaying=false;}
    }
}
#endif
