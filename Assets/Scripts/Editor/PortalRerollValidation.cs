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
public static class PortalRerollValidation
{
    const string Key="PortalRerollValidation";
    static IEnumerator routine;static int checks;
    static readonly Stack<IEnumerator> stack=new();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static PortalRerollValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Portal Reroll")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        SessionState.SetString(Key+".scene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/World.unity");
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
            Finish("PASS: "+checks+" reroll checks");
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
        yield return Wait(.4f);
        var p=PortalTowerProgression.Instance;
        var view=p.GetComponentInChildren<PortalUpgradeCards>(true);
        var bar=Object.FindFirstObjectByType<PortalTowerExperienceBar>();
        Check(p.CrystalDebt==0&&p.RerollCost==1&&p.CanReroll,"Run starts with one-crystal reroll and no debt");
        Check(view.rerollButton!=null&&view.rerollButton.IsInteractable(),"Authored reroll button is actionable");
        var window=(RectTransform)view.transform.Find("Window");var br=(RectTransform)view.rerollButton.transform;
        Check(br.anchoredPosition.y+br.rect.height/2 < ((RectTransform)view.cards[0].button.transform.parent).anchoredPosition.y-221,"Reroll below cards without overlap");
        Check(br.anchoredPosition.y-br.rect.height/2 > -window.rect.height/2,"Reroll fits panel");
        view.rerollButton.onClick.Invoke();
        Check(p.CrystalDebt==1&&p.RerollCost==2&&p.LevelPoints==1&&p.WeaponCount==0&&Time.timeScale==0,"Button rerolls free starting choice without granting weapon or unpausing");
        Check(p.Offers.Count==3&&p.Offers.All(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon),"Startup reroll preserves weapon-choice rules");
        Check(p.Reroll()&&p.CrystalDebt==3&&p.RerollCost==4,"Second reroll costs two");
        Check(p.Reroll()&&p.CrystalDebt==7&&p.RerollCost==8,"Third reroll costs four");
        var snapshot=p.Offers.ToArray();Check(!p.CanReroll&&!p.Reroll()&&p.CrystalDebt==7&&p.RerollCost==8&&snapshot.SequenceEqual(p.Offers),"Over-cap reroll rejected without changing price, debt or cards");
        Check(!view.rerollButton.interactable,"Unaffordable reroll disabled");
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        UnityEngine.EventSystems.ExecuteEvents.Execute(view.rerollButton.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        Canvas.ForceUpdateCanvases();
        Check(view.rerollTooltip.activeInHierarchy&&view.rerollTooltipText.text.Contains("долг"),"Hover tooltip explains borrowed crystals even when unaffordable");
        Check(view.rerollTooltipText.preferredHeight<=view.rerollTooltipText.rectTransform.rect.height+1,"Tooltip text fits");
        UnityEngine.EventSystems.ExecuteEvents.Execute(view.rerollButton.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
        Check(!view.rerollTooltip.activeSelf,"Tooltip hides on pointer exit");
        var debt=bar.fill.transform.parent.Find("Crystal Debt").GetComponent<UnityEngine.UI.Image>();
        Check(debt.enabled&&debt.sprite==bar.fill.sprite&&Mathf.Approximately(debt.fillAmount,7f/p.RequiredExperience),"Grey debt uses the same fill sprite and debt fraction");
        var gray=debt.material.GetColor("_Color");Check(gray.r==gray.g&&gray.g==gray.b,"Debt replacement color is neutral grey");
        Check(p.PreviewExperience(5)==0&&p.PreviewExperience(9)==2,"Incoming preview excludes debt repayment");
        Check(p.Purchase(p.Offers[0].id)&&p.LevelPoints==0&&!view.rerollTooltip.activeSelf,"Choosing weapon closes modal and tooltip");
        Check(!p.Reroll(),"Cannot reroll outside level choice");
        typeof(PortalTowerProgression).GetProperty("PendingExperience").GetSetMethod(true).Invoke(p,new object[]{7});
        int level=p.Level;
        typeof(PortalTowerProgression).GetMethod("CommitIncomingExperience",Private).Invoke(p,new object[]{3});
        Check(p.CrystalDebt==4&&p.Experience==0&&p.Level==level,"Arriving crystals repay debt before earning XP");
        Check(Mathf.Approximately(debt.fillAmount,4f/p.RequiredExperience),"Grey fill shrinks after crystal arrival");
        typeof(PortalTowerProgression).GetMethod("CommitIncomingExperience",Private).Invoke(p,new object[]{4});
        Check(p.CrystalDebt==0&&p.Experience==0&&p.Level==level&&!debt.enabled,"Last debt crystal does not become XP");
        p.AddExperience(2);Check(p.Experience==2,"Experience resumes after debt is fully repaid");
        p.AddExperience(p.RequiredExperience-p.Experience);
        Check(p.Level==2&&p.RerollCost==1&&p.Offers.All(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon),"New level reduces price by up to three steps and offers upgrades");
        Check(p.Reroll()&&p.Reroll()&&p.Reroll()&&p.Reroll(),"Four rerolls affordable on level two");
        Check(p.CrystalDebt==15&&p.RerollCost==16,"Four rerolls cost 1+2+4+8");
        Check(p.Offers.All(p.IsUnlocked)&&p.Offers.All(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon),"Upgrade rerolls retain weapon matching and level schedule");
        p.Purchase(p.Offers[0].id);
        p.AddExperience(16);Check(p.CrystalDebt==0&&p.Experience==1,"Excess crystals after debt repayment go into XP");
        p.AddExperience(p.RequiredExperience-1);
        Check(p.Level==3&&p.RerollCost==2,"Next level lowers price from sixteen to two, not one");
        p.Reroll();Check(p.CrystalDebt==2&&p.RerollCost==4,"Discounted reroll charges correct cost");
        p.Purchase(p.Offers[0].id);
        p.AddExperience(2);p.AddExperience(p.RequiredExperience);
        Check(p.Level==4&&p.RerollCost==1,"Price never falls below one");
        p.Purchase(p.Offers[0].id);p.AddExperience(p.RequiredExperience);
        Check(p.Level==5&&p.Offers.Count(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon)==1,"Fifth-level choice still includes one weapon");
        p.Reroll();Check(p.Offers.Count(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon)==1,"Fifth-level reroll retains one weapon and two upgrades");
        p.Purchase(p.Offers[0].id);
        p.AddExperience(p.CrystalDebt+p.RequiredExperience+p.Balance.RequiredExperience(p.Level+1));
        Check(p.LevelPoints==2&&p.ChoiceLevel==6,"Multiple gained levels queue separate choices");
        Check(p.Reroll()&&p.Reroll()&&p.Reroll()&&p.Reroll()&&p.RerollCost==16,"Queued first choice can increase price four steps");
        p.Purchase(p.Offers[0].id);
        Check(p.ChoiceLevel==7&&p.RerollCost==2,"Queued next choice also reduces price by three steps");
        p.EndExpedition();Check(!p.CanReroll&&!p.Reroll(),"Expedition end disables reroll");
    }
}
#endif
