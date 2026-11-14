#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameFoundation;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using Object=UnityEngine.Object;
public static class TowerBalanceProbe
{
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static string Status="idle";
 public static List<string> Results=new();
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 public static async void Run(int[] counts, int[] ranks, int seed=123)
 {
  Results.Clear();
  try {
   foreach(int rank in ranks) foreach(int count in counts){Status=$"rank {rank}, count {count}";await Battle(count,rank,seed);}
   Status="done";
  }catch(Exception e){Status=e.ToString();Debug.LogException(e);}
  System.IO.File.WriteAllLines(".ai/tower-upgrades/probe.txt",Results.Concat(new[]{Status}));
 }
 static async Task Battle(int count,int rank,int seed)
 {
  typeof(PlaytestSettings).GetField("disabledForRun",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
  foreach(var r in ResourceCatalog.All) if(r.isMilitaryUnit||r.Id=="Human"||r.Id=="Cart"){
   SaveSlotPrefs.SetInt("Global_Resource_"+r.Id,0);
   if(GlobalResourceManager.Instance!=null)GlobalResourceManager.Instance.SetResourceAmount(r,0);
  }
  SaveSlotPrefs.SetInt("foundation.arsenal.enhancement.bolts.kills",rank>=1?1:0);
  SaveSlotPrefs.SetInt("foundation.arsenal.enhancement.bolts.ore",rank>=2?1:0);
  SaveSlotPrefs.SetInt("foundation.arsenal.loadout.1",1);
  SaveSlotPrefs.SetInt("foundation.arsenal.loadout.start",1);
  var op=SceneManager.LoadSceneAsync("World");while(!op.isDone)await Task.Delay(30);await Task.Delay(400);
  var a=AlarmSystem.Instance;var p=PortalTowerProgression.Instance;
  Call(a,"StopDirectedWaves");
  var tower=Object.FindObjectsByType<ArcherTower>(FindObjectsSortMode.None).First(x=>x.PortalProgression==p);
  var hp=tower.GetComponent<Health>();
  var original=Resources.Load<AlarmDifficultyTable>("World/AlarmDifficultyTable").GetThresholds(1)[0].waves[0].units[0].prefab;
  var tier=new AlarmThreshold{alarmValue=20,waveCountdown=.2f,waves=new List<AlarmWaveComposition>{new AlarmWaveComposition{name="Probe",groups=1,units=new List<AlarmWaveUnit>{new AlarmWaveUnit{prefab=original,count=count}}}}};
  Set(a,"thresholds",new List<AlarmThreshold>{tier});
  UnityEngine.Random.InitState(seed);
  p.SelectWeapon(PortalTowerBalance.Weapon.Bolts);
  Call(p,"SetEnergy",3000,false);
  a.SetAlarm(20);
  float start=Time.time,deadline=Time.realtimeSinceStartup+45;bool attacked=false;
  float loss=0,prev=hp.CurrentHealth;int peak=0;
  while(Time.realtimeSinceStartup<deadline && Time.time-start<200 && hp!=null&&!hp.IsDead){
   GameSpeedControls.SetSimulationSpeed(6);
   if(p.UpgradeCrystalChoice.HasValue)p.DeclineUpgradeCrystal();
   if(a.CurrentWaveStage==AlarmSystem.WaveStage.Attacking)attacked=true;
   if(hp.CurrentHealth<prev)loss+=prev-hp.CurrentHealth;prev=hp.CurrentHealth;
   peak=Math.Max(peak,GameObject.FindGameObjectsWithTag("Enemy1").Count(x=>x.GetComponent<Health>() is Health h&&!h.IsDead));
   if(attacked&&a.CurrentWaveStage==AlarmSystem.WaveStage.Rest)break;
   await Task.Delay(40);
  }
  Results.Add($"rank={rank} count={count} seed={seed} HP={(hp==null?0:hp.CurrentHealth)}/{(hp==null?500:hp.MaxHealth)} damage={loss} peak={peak} elapsed={Time.time-start} stage={a.CurrentWaveStage} weapon={p.ActiveWeapon}");
  System.IO.File.WriteAllLines(".ai/tower-upgrades/probe.txt",Results);
  GameSpeedControls.SetSimulationSpeed(1);
 }
}
#endif
