#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GameFoundation.MetaProgression;
using Object=UnityEngine.Object;
public static class DepletionCrystalValidation
{
 static readonly List<GameObject> depleted=new();
 static PortalTowerProgression progression; static int beforeDepletion;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 static void Call(object o,string n)=>o.GetType().GetMethod(n,Flags).Invoke(o,null);
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Prepare()
 {
  depleted.Clear();
  progression=new GameObject("Depletion crystal QA").AddComponent<PortalTowerProgression>();progression.enabled=false;
  Set(progression,"balance",AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset"));
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("6981f204087f19d41984117826313482"));
  Set(progression,"experienceCrystalPrefab",source.GetComponent<PortalExperienceCrystal>());
  Set(progression,"<CrystalTechnologyAvailable>k__BackingField",true);
  CrystalAbundanceValidation.Begin(progression); beforeDepletion=Object.FindObjectsByType<ResourceItem>().Count(r=>r.type==progression.ExperienceCrystalResource);
  foreach(string name in new[]{"Forest 1","Stone 1","Animals/Chickens 1","Animals/Boars 1"})
  {
   var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/"+name+".prefab"));depleted.Add(go);
   var requester=go.GetComponent<ResourceRequester>();Call(requester,"Start");
   var timers=go.GetComponentsInChildren<TimerController>(true);
   var production=timers.Single(t=>Enumerable.Range(0,t.OnTimerEnd.GetPersistentEventCount()).Any(i=>t.OnTimerEnd.GetPersistentTarget(i)==requester&&t.OnTimerEnd.GetPersistentMethodName(i)=="FinishProcessing"));
   Check(production.CrystalOwner==requester,name+": production timer lost owner");
   foreach(var timer in timers)if(timer!=production)Check(timer.CrystalOwner==null,name+": cleanup timer waits for workers");
   foreach(var timer in timers)Call(timer,"Start");
   ((List<ResourceOutput>)typeof(ResourceRequester).GetField("outputResources",Flags).GetValue(requester)).Clear(); // Avoid unrelated resources, units and inventory writes in this fixture.
   production.OnTimerEnd.Invoke();
   foreach(var timer in timers)if(timer!=production&&timer.IsRunning){Set(timer,"_currentTime",0f);Call(timer,"Update");}
   Check(!go.activeSelf,name+": depleted object did not disappear");
  }
 }
 public static void Verify()
 {
  int count=Object.FindObjectsByType<ResourceItem>(FindObjectsInactive.Exclude).Count(r=>r.type==progression.ExperienceCrystalResource)-beforeDepletion;
  Check(count>=8&&count<=24,"Expected 2 to 6 ground crystals for each of four depleted hexes; got "+count);
  foreach(var go in depleted)Check(!go.activeSelf,"Depleted object reactivated");
  SessionState.SetString("DepletionCrystalQAResult","PASS: Forest, Stone, Chickens, Boars final production callbacks deactivate objects and drop 2 to 6 upgraded ground crystals per hex");
 }
}
#endif


