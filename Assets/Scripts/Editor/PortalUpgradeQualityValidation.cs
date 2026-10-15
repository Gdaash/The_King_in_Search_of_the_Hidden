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
public static class PortalUpgradeQualityValidation
{
    const string Key="PortalUpgradeQualityValidation";
    static IEnumerator routine;static int checks;
    static readonly Stack<IEnumerator> stack=new();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static PortalUpgradeQualityValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Portal Upgrade Qualities")]
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
            Finish("PASS: "+checks+" upgrade quality checks");
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
        var b=p.Balance;
        var view=p.GetComponentInChildren<PortalUpgradeCards>(true);
        var ranks=(Dictionary<string,int>)typeof(PortalTowerProgression).GetField("ranks",Private).GetValue(p);
        var values=(Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p);
        var offers=(List<PortalTowerBalance.Upgrade>)typeof(PortalTowerProgression).GetField("offers",Private).GetValue(p);
        Check(b.upgrades.Count(u=>u.hasQuality)==8,"Eight weapon upgrade families");
        Check(b.upgrades.Where(u=>u.hasQuality).All(u=>u.maximumRank==10),"All weapon families share ten-rank caps");
        var counts=new int[3]; for(int i=0;i<10000;i++) counts[(int)PortalTowerBalance.RollQuality(i/10000f)]++;
        Check(counts.SequenceEqual(new[]{6000,3000,1000}),"Quality distribution is 60/30/10");
        Check(p.IsInitialWeaponChoice && p.Offers.Count==3 && p.Offers.All(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon),"Startup offers only three weapons");
        foreach(var w in b.weapons.Where(w=>!w.disabled))
        {
            foreach(var u in b.upgrades.Where(u=>u.hasQuality))
            {
                bool expected=u.effect switch {
                    PortalTowerBalance.Effect.Projectiles or PortalTowerBalance.Effect.ExpansionSpeed=>w.deliveryMethod==PortalTowerBalance.DeliveryMethod.Projectile,
                    PortalTowerBalance.Effect.Area=>w.deliveryMethod==PortalTowerBalance.DeliveryMethod.Area,
                    PortalTowerBalance.Effect.Duration=>w.weapon==PortalTowerBalance.Weapon.FrostSeal,
                    PortalTowerBalance.Effect.TickInterval=>w.weapon==PortalTowerBalance.Weapon.Beam||w.weapon==PortalTowerBalance.Weapon.FrostSeal,
                    PortalTowerBalance.Effect.AttackInterval=>w.weapon!=PortalTowerBalance.Weapon.Beam,
                    _=>true};
                Check(PortalUpgradeModifiers.Applies(u,w)==expected,w.weapon+" eligibility: "+u.id);
            }
            offers[0]=b.Find("weapon_"+w.weapon).CreateOffer(PortalTowerBalance.Quality.Common);
            view.SendMessage("Refresh");Canvas.ForceUpdateCanvases();
            Check(view.cards[0].description.preferredHeight<=view.cards[0].description.rectTransform.rect.height+1,w.weapon+" weapon card fits");
        }
        foreach(var u in b.upgrades.Where(u=>u.hasQuality))
        foreach(PortalTowerBalance.Quality q in Enum.GetValues(typeof(PortalTowerBalance.Quality)))
        {
            offers[0]=u.CreateOffer(q);view.SendMessage("Refresh");Canvas.ForceUpdateCanvases();
            Check(view.cards[0].description.preferredHeight<=view.cards[0].description.rectTransform.rect.height+1,u.id+" "+q+" description fits");
            Check(view.cards[0].category.preferredWidth<=view.cards[0].category.rectTransform.rect.width+1,u.id+" "+q+" category fits");
        }
        offers[0]=b.Find("weapon_Bolts").CreateOffer(PortalTowerBalance.Quality.Common);
        Check(p.Purchase("weapon_Bolts")&&p.WeaponCount==1&&p.LevelPoints==0,"Startup purchase resumes gameplay");
        for(int i=0;i<10;i++)
        {
            p.AddExperience(p.RequiredExperience);
            if(p.Level==2)Check(p.Offers.All(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon),"Level two never offers weapons");
            if(p.Level==5||p.Level==10)Check(p.Offers.Count(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon)==1,"Fifth level optional weapon");
            var q=(PortalTowerBalance.Quality)(i%3);
            offers.Clear();offers.Add(b.Find("all_damage").CreateOffer(q));
            view.SendMessage("Refresh");Canvas.ForceUpdateCanvases();
            Check(view.cards[0].description.preferredHeight<=view.cards[0].description.rectTransform.rect.height+1,"Quality description fits");
            float old=p.DamageMultiplier;
            Check(p.Purchase("all_damage"),"Mixed quality damage purchased");
            Check(Mathf.Abs(p.DamageMultiplier-old-(q==PortalTowerBalance.Quality.Common?.1f:q==PortalTowerBalance.Quality.Rare?.2f:.3f))<.0001f,"Purchased quality applied exactly");
        }
        Check(p.Rank("all_damage")==10&&p.IsComplete(b.Find("all_damage")),"Mixed qualities reach one shared cap");
        p.AddExperience(p.RequiredExperience);
        Check(p.Offers.All(u=>u.id!="all_damage"),"Capped family excluded at every quality");
        var snapshot=p.Offers.Select(u=>(u.id,u.quality,u.OfferedValue)).ToArray();view.SendMessage("Refresh");
        Check(snapshot.SequenceEqual(p.Offers.Select(u=>(u.id,u.quality,u.OfferedValue))),"Refreshing does not reroll quality");
        offers.Clear();offers.Add(b.Find("projectile_count").CreateOffer(PortalTowerBalance.Quality.Legendary));
        Check(p.Purchase("projectile_count")&&p.ProjectileCount==4,"Legendary adds three projectiles");
        Check(p.Count(PortalTowerBalance.Weapon.FireShot)==6&&p.Count(PortalTowerBalance.Weapon.ElectricOrb)==4,"Future projectile weapons inherit count");
        Check(p.Count(PortalTowerBalance.Weapon.Lightning)==1,"Projectile upgrade does not add lightning hits");
        ranks["tick_interval"]=10; values["tick_interval"]=2;
        ranks["attack_interval"]=10; values["attack_interval"]=2;
        foreach(var w in b.weapons.Where(w=>!w.disabled))
        {
            Check(p.AttackInterval(w.weapon)>=.1f,"Attack interval floor: "+w.weapon);
            if(w.HasTicks)Check(Mathf.Abs(p.TickInterval(w.weapon)-.1f)<.0001f,"Tick floor: "+w.weapon);
        }
        Check(p.AttackInterval(PortalTowerBalance.Weapon.Beam)==1,"Attack upgrades leave continuous beam unchanged");
        values["tick_interval"]=0;Check(p.TickInterval(PortalTowerBalance.Weapon.Beam)==1,"Beam base tick is one second");
        Check(b.FindWeapon(PortalTowerBalance.Weapon.Beam).damage==10,"Beam preserves ten baseline DPS");
        ranks["all_range"]=1;values["all_range"]=2;Check(p.AddedRange(PortalTowerBalance.Weapon.Bolts)==2,"Range is flat world units");
        ranks["area_size"]=1;values["area_size"]=.35f;
        var controller=Object.FindFirstObjectByType<PortalWeaponController>();
        Check(Mathf.Abs(controller.FrostRadius-b.FindWeapon(PortalTowerBalance.Weapon.FrostSeal).radius*1.35f)<.0001f,"Radius is linear, not square root");
        ranks["area_duration"]=1;values["area_duration"]=.2f;
        Check(p.Multiplier(PortalTowerBalance.Weapon.FrostSeal,PortalTowerBalance.Effect.Duration)==1.2f&&p.Multiplier(PortalTowerBalance.Weapon.Beam,PortalTowerBalance.Effect.Duration)==1,"Duration only affects temporary area");
        foreach(var u in b.upgrades)ranks[u.id]=u.maximumRank;
        p.AddExperience(p.RequiredExperience);
        Check(p.LevelPoints==0&&!view.gameObject.activeInHierarchy&&Time.timeScale>0,"Exhausted upgrade pool cannot trap player in empty modal");
    }
}
#endif
