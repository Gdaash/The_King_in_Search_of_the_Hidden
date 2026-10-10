#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameFoundation.Combat;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CombatBalanceValidation
{
    const string Key="CombatBalanceValidation";
    static readonly List<Object> spawned=new();static int checks;
    static CombatBalanceValidation(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Validation/Combat Balance")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");BuildingUpgradeValidation.Begin();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static GameObject Prefab(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/"+name+".prefab");
    static Health Target(string name,int level=1)
    {
        var source=Prefab(name).GetComponent<Combatant>();var g=new GameObject("Combat test "+name);spawned.Add(g);g.SetActive(false);g.tag="Enemy1";
        var c=g.AddComponent<Combatant>();EditorUtility.CopySerialized(source,c);
        var modifier=Object.Instantiate(c.modifiers);spawned.Add(modifier);modifier.bonusHealth=modifier.bonusArmor=0;
        foreach(var d in modifier.damageSettings)d.bonusDamage=0;foreach(var r in modifier.resistances)r.bonusResist=0;c.modifiers=modifier;
        var h=g.AddComponent<Health>();h.OnDeath=new UnityEvent();h.OnHealthChanged=new UnityEvent<float>();
        var s=new SerializedObject(h);s.FindProperty("stats").objectReferenceValue=modifier;s.ApplyModifiedPropertiesWithoutUndo();
        if(level>1)g.AddComponent<EnemyLevel>().Initialize(level);
        g.SetActive(true);h.SetNormalizedHealth(1);return h;
    }
    static void Hit(Health target,float raw,DamageType type,float expected)
    {
        target.SetNormalizedHealth(1);float before=target.CurrentHealth;target.TakeDamage(raw,type);
        Check(Mathf.Approximately(before-target.CurrentHealth,expected),target.name+" "+type+" "+raw+" => "+expected);
    }
    static async void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false) || state!=PlayModeStateChange.EnteredPlayMode)return;
        try
        {
            checks=0;await Task.Delay(250);GameSpeedControls.SetSimulationSpeed(1);
            var knight=Target("CursedKnight");var guardian=Target("OctopusGuardian");var mage=Target("CursedMage");var giant=Target("OctopusWarrior");
            Check(knight.MaxHealth==100 && guardian.MaxHealth==80 && giant.MaxHealth==300 && mage.MaxHealth==60,"Enemy health archetypes");
            Hit(knight,10,DamageType.Physical,7);Hit(knight,3,DamageType.Physical,0);Hit(knight,40,DamageType.Magic,37);
            Hit(knight,5,DamageType.Fire,2);Hit(knight,40,DamageType.Electric,60);
            Hit(guardian,10,DamageType.Physical,7);Hit(guardian,40,DamageType.Electric,60);
            foreach(var type in new[]{DamageType.Magic,DamageType.Fire,DamageType.Ice,DamageType.Electric})Hit(mage,40,type,20);
            Hit(mage,40,DamageType.Physical,50);Hit(giant,40,DamageType.Physical,40);Hit(giant,5,DamageType.Fire,5);
            Check(Prefab("Octopus").GetComponent<Combatant>().baseHealth==20 && Prefab("OctopusRaider").GetComponent<Combatant>().baseSpeed>Prefab("Octopus").GetComponent<Combatant>().baseSpeed,"Fast low-health enemies");
            var high=Target("CursedKnight",50);
            Check(Mathf.Approximately(high.MaxHealth,590) && high.Armor>3 && high.Armor<7,"Level 50 scales health faster than armor");
            var c=knight.GetComponent<Combatant>();c.modifiers.bonusArmor=2;Hit(knight,10,DamageType.Physical,5);c.modifiers.bonusArmor=0;
            c.modifiers.resistances.Find(r=>r.type==DamageType.Electric).bonusResist=.25f;Hit(knight,40,DamageType.Electric,50);
            c.modifiers.resistances.Find(r=>r.type==DamageType.Electric).bonusResist=0;
            var projectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/ArrowTowerProjectile.prefab");
            var projectile=Object.Instantiate(projectilePrefab);spawned.Add(projectile);
            var p=projectile.GetComponent<EnemyProjectile>();knight.SetNormalizedHealth(1);p.SetupHit(Vector3.right,"Enemy1",10,DamageType.Physical,null);
            typeof(EnemyProjectile).GetMethod("ProcessHit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{knight.gameObject});
            Check(knight.CurrentHealth==93,"Projectile enters shared armor calculation");
            typeof(EnemyProjectile).GetMethod("ProcessHit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{knight.gameObject});
            Check(knight.CurrentHealth==93,"Same projectile cannot hit twice");
            foreach(var name in new[]{"Archer Bow","Swordsman Sword","Sniper Bow","Arrow Tower","Magic Tower","Stone Tower","Portal Bolts","Portal Beam","Portal Rings","Portal Lightning","Barbarian Attack"})
            {
                var w=AssetDatabase.LoadAssetAtPath<CombatWeapon>(CombatBalanceSetup.Folder+"/Weapons/"+name+".asset");Check(w!=null && Mathf.Approximately(w.SingleTargetDps,20),name+" base DPS = 20");
            }
            var balance=PortalTowerProgression.Instance.Balance;
            Check(balance.FindWeapon(PortalTowerBalance.Weapon.Lightning).damageType==DamageType.Electric,"Portal lightning uses electric damage");
            Check(balance.weapons.All(w=>w.combat!=null && w.damage==w.combat.damage),"Portal weapon stats have one source");
            var swordsman=Target("RedSwordsman");var sword=swordsman.GetComponent<Combatant>();
            Check(sword.Damage==40 && sword.Interval==2,"Melee weapon uses central attack data");
            sword.modifiers.damageSettings.Find(d=>d.type==DamageType.Physical).bonusDamage=5;
            sword.modifiers.bonusAttackSpeed=.5f;
            Check(sword.Damage==45 && sword.Interval==1.5f,"Damage and attack upgrades remain effective");
            sword.modifiers.bonusHealth=20;sword.modifiers.bonusSpeed=.3f;
            Check(swordsman.MaxHealth==120 && Mathf.Approximately(sword.SpeedBeforeLevel,1.8f),"Prefab bases combine with global modifiers");
            var canvas=new GameObject("Combat info validation",typeof(Canvas));spawned.Add(canvas);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=3000;
            var card=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UnitDescriptionSetup.CardPath),canvas.transform);spawned.Add(card);
            var rect=card.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(640,Mathf.Min(880,Screen.height-60));
            var view=card.GetComponent<UnitDescriptionView>();view.Show(AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>("Assets/Resources/UI/Unit Details/Enemy CursedMage Description.asset"));
            var array=new SerializedObject(view).FindProperty("stats");
            Check(Enumerable.Range(0,array.arraySize).Any(i=>(UnitStat)array.GetArrayElementAtIndex(i).FindPropertyRelative("stat").enumValueIndex==UnitStat.Armor),"Info card includes armor");
            Check(Enumerable.Range(0,array.arraySize).Any(i=>(UnitStat)array.GetArrayElementAtIndex(i).FindPropertyRelative("stat").enumValueIndex==UnitStat.ElectricResistance),"Info card includes electric resistance");
            await Task.Delay(150);ScreenCapture.CaptureScreenshot("Temp/CombatMageInfo.png");await Task.Delay(100);
            Check(card.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="50%"),"Mage info displays real magic resistance");
            SessionState.SetString(Key+".result","PASS: "+checks+" combat balance checks");Debug.Log(SessionState.GetString(Key+".result",""));
        }
        catch(Exception e){SessionState.SetString(Key+".result","FAILED: "+e.Message);Debug.LogException(e);}
        finally
        {
            foreach(var o in spawned)if(o!=null)Object.Destroy(o);spawned.Clear();SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
        }
    }
}
#endif
