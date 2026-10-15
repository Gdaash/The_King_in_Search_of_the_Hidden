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
public static class PortalRoguelikeValidation
{
    const string Key="PortalRoguelikeValidation";
    static IEnumerator routine;static int checks;
    static readonly Stack<IEnumerator> stack=new();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static PortalRoguelikeValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Portal Tower Roguelike")]
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
            Finish("PASS: "+checks+" roguelike checks");
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
        var p=PortalTowerProgression.Instance;var controller=Object.FindFirstObjectByType<PortalWeaponController>();
        var tower=Object.FindObjectsByType<ArcherTower>(FindObjectsSortMode.None).First(t=>t.GetStats()!=null && t.GetStats().IsPortalTower);
        var view=p.GetComponentInChildren<PortalUpgradeCards>(true);
        Check(p.WeaponCount==0 && p.Level==1,"Run starts without a weapon");
        Check(p.IsInitialWeaponChoice && p.LevelPoints == 1, "Free startup weapon choice at level one");
        var lights=Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        Check(!lights.CanActivateLights && !lights.LightsActivated, "Light is blocked until a weapon is chosen");
        lights.ActivateAvailableLights();
        Check(!lights.LightsActivated, "Light activation cannot bypass starting choice");
        Check(Time.timeScale==0 && p.Offers.Count==3,"Three choices and pause");
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==null,"First card is not auto-selected");
        var modalCanvas=view.GetComponent<Canvas>();
        Check(modalCanvas.overrideSorting && modalCanvas.sortingOrder>=32760,"Upgrade modal above other UI");
        Check(p.Offers.All(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon) && p.Offers.Select(u=>u.weapon).Distinct().Count()==3,"First choice is three distinct weapons");
        Check(!p.Purchase("lights"),"Unshown upgrades cannot be purchased");
        foreach (var card in view.cards)
        {
            Check(card.description.text.Contains("Способ:") && card.description.text.Contains("Урон:") ||
                card.description.text.Contains("Delivery:") && card.description.text.Contains("Damage:"), "Weapon card includes delivery and damage types");
            Check(card.description.preferredHeight <= card.description.rectTransform.rect.height + 1, "Weapon description fits its card");
        }
        var authoredOffers = (List<PortalTowerBalance.Upgrade>)typeof(PortalTowerProgression).GetField("offers", Private).GetValue(p);
        var originalOffers = authoredOffers.ToArray();
        foreach(var definition in p.Balance.weapons.Where(w => !w.disabled))
        {
            authoredOffers[0] = p.Balance.Find("weapon_" + definition.weapon);
            view.SendMessage("Refresh"); Canvas.ForceUpdateCanvases();
            var card = view.cards[0];
            Check(card.icon.sprite != null && card.title.text.Length > 0 && (card.description.text.Contains("Способ:") || card.description.text.Contains("Delivery:")), definition.weapon + " card has art and damage-delivery labels");
            Check(card.description.preferredHeight <= card.description.rectTransform.rect.height + 1, definition.weapon + " full description fits the card");
        }
        authoredOffers.Clear(); authoredOffers.AddRange(originalOffers); view.SendMessage("Refresh");
        yield return Wait(.3f);ScreenCapture.CaptureScreenshot("UserSettings/CodexValidation/PortalRoguelikeCards.png");yield return Wait(.2f);
        Check(view.cards.Count(c=>c.button.gameObject.activeInHierarchy && c.button.IsInteractable())==3,"All three UI cards are actionable");
        view.cards[0].button.onClick.Invoke();
        Check(p.WeaponCount==1 && p.LevelPoints==0 && p.Level==1 && !p.IsInitialWeaponChoice && Time.timeScale>0,"Free choice grants one weapon without gaining a level and resumes");
        Check(lights.CanActivateLights, "Light is available after weapon choice");
        while(p.Level<10)
        {
            p.AddExperience(p.RequiredExperience);
            Check(p.Offers.Count==3,"Three cards at level "+p.Level);
            Check(p.Offers.Where(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon).All(p.IsUnlocked),"Offered upgrades match owned categories at level "+p.Level);
            if(p.Level%5==0)
            {
                Check(p.Offers.Count(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon)==1,"Exactly one optional weapon on fifth level");
                var selection=p.Level==10?p.Offers.First(u=>u.effect==PortalTowerBalance.Effect.UnlockWeapon):p.Offers.First(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon);
                p.Purchase(selection.id);
                Check(p.WeaponCount==(p.Level==10?2:1),"Optional weapon accepted or declined");
            }
            else{Check(p.Offers.All(u=>u.effect!=PortalTowerBalance.Effect.UnlockWeapon),"No extra weapons on normal level");p.Purchase(p.Offers[0].id);}
        }
        int required=p.RequiredExperience;
        p.AddExperience(required+p.Balance.RequiredExperience(p.Level+1));
        Check(p.LevelPoints==2 && p.ChoiceLevel==11,"Queued choices retain their level");
        p.Purchase(p.Offers[0].id);Check(p.LevelPoints==1 && p.ChoiceLevel==12 && Time.timeScale==0,"New cards while queued level remains paused");
        p.Purchase(p.Offers[0].id);Check(p.LevelPoints==0 && Time.timeScale>0,"Queue resumes after final choice");
        var ranks=(Dictionary<string,int>)typeof(PortalTowerProgression).GetField("ranks",Private).GetValue(p);
        void Equip(PortalTowerBalance.Weapon w)
        {ranks.Clear();ranks["weapon_"+w]=1;controller.StopAllCoroutines();}
        tower.enabled=false;
        Equip(PortalTowerBalance.Weapon.Beam);yield return Wait(.2f);
        bool BeamVisible()=>Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None).Any(l=>l.transform.root.name=="Portal Burning Beam");
        Check(!BeamVisible(),"Beam hidden without enemies");
        Vector2 origin=tower.transform.position;
        var beamEnemy=Enemy(origin+Vector2.right*2);var outside=Enemy(origin+Vector2.up*4);
        var smallRenderer=beamEnemy.gameObject.AddComponent<SpriteRenderer>();smallRenderer.sprite=p.Balance.FindWeapon(PortalTowerBalance.Weapon.Beam).icon;
        var tallRenderer=outside.gameObject.AddComponent<SpriteRenderer>();tallRenderer.sprite=smallRenderer.sprite;outside.transform.localScale=Vector3.one*2;
        float before=beamEnemy.CurrentHealth;yield return Wait(.7f);
        Check(BeamVisible() && Vector2.Distance(controller.BeamAim,beamEnemy.transform.position)<.01f,"Beam automatically selects nearest enemy");
        Check(beamEnemy.CurrentHealth<before && outside.CurrentHealth==100,"Beam damages only inside spot");
        Check(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Any(l=>l.name=="Burning light trail"),"Burning beam emits thin upward light trails");
        var spotLight=Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None).First(l=>l.name=="Burning Spot");
        Check(Mathf.Approximately(spotLight.intensity,2.25f),"Burning spot brightness increased by 50 percent");
        Check(Mathf.Approximately((float)typeof(PortalWeaponController).GetField("minimumEnemyHeight",Private).GetValue(controller),smallRenderer.bounds.size.y) && Mathf.Approximately((float)typeof(PortalWeaponController).GetField("maximumEnemyHeight",Private).GetValue(controller),tallRenderer.bounds.size.y),"Trail heights follow smallest and largest living enemy sprites");
        ScreenCapture.CaptureScreenshot("UserSettings/CodexValidation/PortalBurningBeamTrails.png");yield return Wait(.1f);
        beamEnemy.transform.position=origin+Vector2.left*2;yield return Wait(.1f);
        Check(Vector2.Distance(controller.BeamAim,beamEnemy.transform.position)<.01f,"Beam follows moving enemy");
        before=beamEnemy.CurrentHealth;Time.timeScale=0;yield return Wait(.6f);Check(beamEnemy.CurrentHealth==before,"Weapons stop during pause");Time.timeScale=1;
        Object.Destroy(beamEnemy.gameObject);yield return Wait(.2f);
        Check(Vector2.Distance(controller.BeamAim,outside.transform.position)<.01f,"Beam switches target after enemy disappears");
        outside.transform.position=origin+Vector2.up*20;yield return Wait(.2f);
        Check(!BeamVisible(),"Beam hidden when all enemies are outside range");
        outside.transform.position=origin+Vector2.up*3;yield return Wait(.2f);
        Check(BeamVisible(),"Beam resumes when enemy enters range");
        Object.Destroy(outside.gameObject);yield return Wait(.2f);
        Check(!BeamVisible(),"Beam hides after last enemy disappears");
        Check(!Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Any(l=>l.name=="Burning light trail"),"Light trails hide with beam");
        int cheatLevel=p.Level,cheatExperience=p.Experience;
        p.GrantEditorLevel();
        Check(p.Level==cheatLevel+1 && p.Experience==0 && p.LevelPoints==1 && Time.timeScale==0,"Editor cheat grants one level and opens mandatory choice");
        p.GrantEditorLevel();Check(p.Level==cheatLevel+1,"Editor cheat cannot stack levels while choosing");
        p.Purchase(p.Offers[0].id);
        if(p.Balance.IsWeaponEnabled(PortalTowerBalance.Weapon.Rings))
        {
        Equip(PortalTowerBalance.Weapon.Rings);
        typeof(PortalWeaponController).GetField("ringTime",Private).SetValue(controller,Time.time+.2f);
        var ringA=Enemy((Vector2)tower.transform.position+Vector2.right*1.5f);var ringB=Enemy((Vector2)tower.transform.position+Vector2.up*1.5f);var ringFar=Enemy((Vector2)tower.transform.position+Vector2.right*8);
        yield return Wait(1.5f);
        Check(ringA.CurrentHealth==70 && ringB.CurrentHealth==70 && ringFar.CurrentHealth==100,"Ring hits every touched enemy once and respects radius");
        Object.Destroy(ringA.gameObject);Object.Destroy(ringB.gameObject);Object.Destroy(ringFar.gameObject);
        }
        else Check(!p.HasWeapon(PortalTowerBalance.Weapon.Rings) && !p.Offers.Any(u=>u.weapon==PortalTowerBalance.Weapon.Rings),"Disabled rings excluded from weapons and offers");
        Equip(PortalTowerBalance.Weapon.Lightning);
        typeof(PortalWeaponController).GetField("lightningTime",Private).SetValue(controller,Time.time+.2f);
        var la=Enemy((Vector2)tower.transform.position+Vector2.right*2);var lb=Enemy((Vector2)tower.transform.position+Vector2.up*2);var lf=Enemy((Vector2)tower.transform.position+Vector2.left*3);
        yield return Wait(.25f);
        ScreenCapture.CaptureScreenshot("UserSettings/CodexValidation/PortalPixelLightning.png");
        yield return Wait(.35f);
        Check(controller.lightningSprite!=null && p.Balance.FindWeapon(PortalTowerBalance.Weapon.Lightning).range<2.9f,"Pixel lightning uses short first-ring range");
        Check(Mathf.Approximately(la.CurrentHealth+lb.CurrentHealth,160) && lf.CurrentHealth==100,"Lightning guarantees one random in-range hit");
        ranks["instant_targets"]=1;
        typeof(PortalWeaponController).GetField("lightningTime",Private).SetValue(controller,Time.time+.1f);
        yield return Wait(.4f);Check(la.CurrentHealth+lb.CurrentHealth==80,"Additional lightning hits another target");
        Object.Destroy(la.gameObject);Object.Destroy(lb.gameObject);Object.Destroy(lf.gameObject);
        Equip(PortalTowerBalance.Weapon.Bolts);tower.enabled=true;
        var boltEnemy=Enemy((Vector2)tower.transform.position+Vector2.right*3);
        yield return Wait(2.5f);Check(boltEnemy.CurrentHealth<100,"Original projectile weapon still fires and damages");
        ranks["all_damage"]=1;ranks["projectile_count"]=1;
        Check(p.ProjectileCount==2 && Mathf.Approximately(p.DamageMultiplier,1.1f),"Projectile count and global damage apply to bolts");
        ranks["weapon_Beam"]=1;ranks["damage_type_1"]=1;
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.Beam,PortalTowerBalance.Effect.Damage),1.3f) && Mathf.Approximately(p.DamageMultiplier,1.1f),"New beam inherits global damage and adds fire damage without changing magic bolts");
        ranks["area_size"]=1;
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.Beam,PortalTowerBalance.Effect.Area),1.2f) && p.Multiplier(PortalTowerBalance.Weapon.Bolts,PortalTowerBalance.Effect.Area)==1,"Area upgrade affects area weapons only");
        Check(p.Count(PortalTowerBalance.Weapon.Lightning)==1 && p.ProjectileCount==2,"Projectile upgrade does not add lightning targets");
        ranks["weapon_Lightning"]=1;ranks["instant_targets"]=1;
        Check(p.Count(PortalTowerBalance.Weapon.Lightning)==2 && p.ProjectileCount==2,"Instant targets and projectile counts are independent");
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.Lightning,PortalTowerBalance.Effect.Damage),1.1f),"Later lightning inherits global damage");
        ranks["all_attack_speed"]=1;ranks["all_range"]=1;
        foreach (var w in p.Balance.weapons.Where(w=>!w.disabled))
            Check(Mathf.Approximately(p.Multiplier(w.weapon,PortalTowerBalance.Effect.AttackSpeed),1.1f) && Mathf.Approximately(p.Multiplier(w.weapon,PortalTowerBalance.Effect.Range),1.15f),"Shared attack speed and range reach "+w.weapon);
        foreach(var weapon in p.Balance.weapons)ranks["weapon_"+weapon.weapon]=1;
        var mixedEnemy=Enemy((Vector2)tower.transform.position+Vector2.left);
        yield return Wait(.8f);
        Check(p.WeaponCount==p.Balance.weapons.Count(w=>p.Balance.IsWeaponEnabled(w.weapon)) && mixedEnemy.CurrentHealth<100,"Multiple owned weapons operate together");
        tower.enabled=false;yield return Wait(.5f);
        p.EndExpedition();float stopped=mixedEnemy.CurrentHealth;yield return Wait(.7f);
        Check(mixedEnemy.CurrentHealth==stopped,"Escape stops the new weapons safely");
        Check(!Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None).Any(l=>l.transform.root.name=="Portal Burning Beam"),"Escape hides both the cone and the burning spot");
    }
}
#endif
