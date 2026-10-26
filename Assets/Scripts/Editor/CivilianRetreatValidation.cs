using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameFoundation.MetaProgression;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CivilianRetreatValidation
{
 const string ScenePath="Assets/Editor/CivilianRetreatQA.unity";
 const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly Type Saves=typeof(GameFoundation.Saves.SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
 const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
 static CivilianRetreatValidation(){EditorApplication.playModeStateChanged+=OnState;}
 public static void Begin()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop the current game first");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);
  SessionState.SetString("CivilianPreviousScene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool("CivilianQA",true);SessionState.SetString("CivilianQAResult","RUNNING");
  EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;
 }
 static void OnState(PlayModeStateChange state)
 {
  if(!SessionState.GetBool("CivilianQA",false))return;
  if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;
  if(state==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("CivilianPreviousScene",""));AssetDatabase.DeleteAsset(ScenePath);SessionState.SetBool("CivilianQA",false);}
 }
 static void Set(object value,string field,object data)=>value.GetType().GetField(field,Fields).SetValue(value,data);
 static object Get(object value,string field)=>value.GetType().GetField(field,Fields).GetValue(value);
 static void Call(object value,string method)=>value.GetType().GetMethod(method,Fields).Invoke(value,null);
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Run()
 {
  bool previous=(bool)Saves.GetField("WritesSuspended",Static).GetValue(null);Saves.GetField("WritesSuspended",Static).SetValue(null,true);
  try
  {
   var humans=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Human.asset");
   if(humans==null)humans=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Human.prefab").GetComponent<HumanUnit>().GetCarriedResourceType();
   var resources=new GameObject("QA resources").AddComponent<GlobalResourceManager>();resources.SetResourceAmount(humans,7);
   var warehouse=new GameObject("QA portal").AddComponent<Warehouse>();Set(warehouse,"humanPrefab",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Human.prefab"));Set(warehouse,"humanResourceType",humans);
   var mineGo=new GameObject("QA mine");var mine=mineGo.AddComponent<ResourceRequester>();mine.requirements.Add(new ResourceRequirement{resourceType=humans,requiredAmount=1});Set(mine,"retainHumansBetweenCycles",true);
   var shelter=mineGo.AddComponent<ResourceWorkforce>();Call(shelter,"Start");
   Check(CombatTargetRegistry.ClosestHostileTarget(Vector2.zero,10)==null,"Empty mine must not be targeted");
   mine.requirements[0].currentAmount=1;Check(CombatTargetRegistry.ClosestHostileTarget(Vector2.zero,10)==mine.transform,"Occupied mine must be targeted");
   mine.requirements[0].currentAmount=0;Check(!CombatTargetRegistry.IsAlive(mine.transform),"Cached empty mine must become invalid immediately");
   Check(CombatTargetRegistry.ClosestHostileTarget(Vector2.zero,10)==null,"Vacated mine must be skipped");
   var forestGo=new GameObject("QA forest");var forest=forestGo.AddComponent<ResourceRequester>();forest.requirements.Add(new ResourceRequirement{resourceType=humans,requiredAmount=2,currentAmount=2});
   var workforce=forestGo.AddComponent<ResourceWorkforce>();Call(workforce,"Start");Call(workforce,"Update");
   var workers=Object.FindObjectsByType<ResourceWorker>();Check(workers.Length==2,"Resident workers were not created");
   var enemy=new GameObject("QA attacker");enemy.tag="Enemy1";var ai=enemy.AddComponent<EnemyAI>();Set(ai,"_target",workers[0].transform);Set(ai,"_nextAttackTime",-1f);
   Call(ai,"HandleCombat");Check(forest.ResidentHumanCount==0,"Attack must evacuate all exposed workers before damage");
   var returning=Object.FindObjectsByType<HumanUnit>();Check(returning.Length==2,"Two workers must physically return to portal");
   Check(OrderManager.CountHumansAwayFromPortal(humans)==2,"Living returnees must still block escape");
   foreach(var human in returning)human.GetComponent<Health>().TakeDamage(100000,DamageType.Physical);
   Check(resources.GetResourceAmount(humans)==5,"Deaths must reduce population exactly once");
   Check(OrderManager.CountHumansAwayFromPortal(humans)==0,"Dying returnees must not block escape");
   var rangedForestGo=new GameObject("QA ranged forest");var rangedForest=rangedForestGo.AddComponent<ResourceRequester>();rangedForest.requirements.Add(new ResourceRequirement{resourceType=humans,requiredAmount=1,currentAmount=1});
   var rangedWorkforce=rangedForestGo.AddComponent<ResourceWorkforce>();Call(rangedWorkforce,"Start");Call(rangedWorkforce,"Update");
   var exposed=Array.Find(Object.FindObjectsByType<ResourceWorker>(),w=>ReferenceEquals(Get(w,"owner"),rangedWorkforce));
   var rangedGo=new GameObject("QA ranged attacker");rangedGo.tag="Enemy1";var ranged=rangedGo.AddComponent<EnemyAI_Ranged>();Set(ranged,"_target",exposed.transform);Set(ranged,"_nextAttackTime",-1f);Call(ranged,"HandleCombat");
   Check(rangedForest.ResidentHumanCount==0,"Ranged attack must also evacuate exposed workers");
   foreach(var human in Object.FindObjectsByType<HumanUnit>())if(!human.GetComponent<Health>().IsDead)Object.DestroyImmediate(human.gameObject);
   var escape=new GameObject("QA escape").AddComponent<WorldEscapeController>();escape.enabled=false;Set(escape,"humanResource",humans);Set(escape,"_startingHumans",7);
   Call(escape,"RefreshProgress");Check((bool)Get(escape,"_showingStatistics"),"Returning five of seven after two deaths must finish");escape.StopAllCoroutines();
   Set(escape,"_showingStatistics",false);resources.SetResourceAmount(humans,0);Call(escape,"RefreshProgress");Check((bool)Get(escape,"_showingStatistics"),"No survivors must not deadlock escape");escape.StopAllCoroutines();
   SessionState.SetString("CivilianQAResult","PASS: occupied/empty/vacated mine targeting; melee and ranged attacks evacuate exposed workers before damage; deaths counted once; dying units excluded; escape completes with casualties and zero survivors.");
  }
  catch(Exception error){SessionState.SetString("CivilianQAResult","FAIL: "+error);Debug.LogException(error);}
  finally{Saves.GetField("WritesSuspended",Static).SetValue(null,previous);Time.timeScale=1;EditorApplication.isPlaying=false;}
 }
}
