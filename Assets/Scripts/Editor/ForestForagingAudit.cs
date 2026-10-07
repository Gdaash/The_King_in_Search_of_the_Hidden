#if UNITY_EDITOR
using System;using System.IO;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEngine.EventSystems;using GameFoundation.Base;using GameFoundation.MetaProgression;using Prefs=GameFoundation.Saves.SaveSlotPrefs;using Object=UnityEngine.Object;
public static class ForestForagingAudit
{
 static T Popup<T>() where T:Component { foreach(var item in Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(item.transform.parent==null||item.transform.parent.gameObject.activeInHierarchy)return item; throw new Exception("No active scene popup "+typeof(T).Name); }
 static int checks;static string Report=>"Temp/ForestForagingAudit.txt";
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;File.AppendAllText(Report,"PASS "+label+"\n");}
 static void Set(Object o,string key,Object value){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 public static async Task RunPlay()
 {
  File.WriteAllText(Report,"Forest foraging audit\n");
  SceneManager.LoadScene("Base");await Task.Delay(800);
  var settings=AssetDatabase.LoadAssetAtPath<ForestForagingSettings>("Assets/Resources/Decrees/Forest Foraging Settings.asset");
  var def=AssetDatabase.LoadAssetAtPath<RoyalDecreeDefinition>("Assets/Resources/Decrees/forest_foraging.asset");
  var day=DayCycleService.Instance;var resources=GlobalResourceManager.Instance;
  Check(day!=null&&resources!=null,"Base services");
  Prefs.DeleteKey("foundation.forestForaging");
  foreach(var type in new[]{settings.humans,settings.swordsmen,settings.archers})resources.SetResourceAmount(type,3);
  resources.SetResourceAmount(settings.food,0);resources.SetResourceAmount(settings.influence,4);
  RoyalDecreeService.SetEnabled(def.id,false);
  Check(!RoyalDecreeService.TryEnable(def),"decree rejects insufficient influence");
  resources.SetResourceAmount(settings.influence,30);
  Check(!ForestForagingService.TryStart(settings),"disabled decree blocks foraging");
  Check(RoyalDecreeService.TryEnable(def)&&resources.GetResourceAmount(settings.influence)==25,"activation costs five");
  Check(!RoyalDecreeService.TryEnable(def)&&resources.GetResourceAmount(settings.influence)==25,"duplicate enable cannot charge twice");
  var castle=Popup<RoyalDecreePopupView>();castle.Open();
  RoyalDecreeRow forest=null;foreach(var row in castle.GetComponentsInChildren<RoyalDecreeRow>())if(row.decree.id==def.id)forest=row;
  Check(forest!=null,"new decree wired");
  forest.Click();Check(castle.disableConfirmation.activeSelf&&RoyalDecreeService.IsEnabled(def.id),"disable requires confirmation");
  castle.cancelDisableButton.onClick.Invoke();Check(RoyalDecreeService.IsEnabled(def.id),"cancel keeps decree enabled");
  forest.Click();castle.confirmDisableButton.onClick.Invoke();Check(!RoyalDecreeService.IsEnabled(def.id)&&resources.GetResourceAmount(settings.influence)==25,"disable does not refund: active="+RoyalDecreeService.IsEnabled(def.id)+" balance="+resources.GetResourceAmount(settings.influence)+" hierarchy="+castle.gameObject.activeInHierarchy);
  Check(RoyalDecreeService.TryEnable(def)&&resources.GetResourceAmount(settings.influence)==20,"reenable costs again");castle.Close();
  resources.SetResourceAmount(settings.food,9);Check(!ForestForagingService.CanStart(settings),"no hunger blocks expedition");
  resources.SetResourceAmount(settings.food,0);resources.SetResourceAmount(settings.influence,4);Check(!ForestForagingService.TryStart(settings),"expedition rejects insufficient influence");
  resources.SetResourceAmount(settings.influence,20);
  var view=Popup<ForestForagingView>();view.gameObject.SetActive(true);view.Refresh();
  Check(view.offer.activeSelf&&view.block.activeSelf,"offer shown with hunger and decree");
  UnityEngine.Random.InitState(1954);view.send.onClick.Invoke();view.Refresh();
  var result=ForestForagingService.Current;
  Check(result.sent==9&&result.survived+result.dead==9,"all hungry people and troops independently resolved");
  Check(result.food>=result.survived&&result.food<=result.survived*2,"one to two food per survivor");
  Check(resources.GetResourceAmount(settings.influence)==15,"expedition costs five once");
  Check(view.timer.activeSelf&&!view.offer.activeSelf&&!view.results.activeSelf,"timer replaces offer");
  Check(!EventSystem.current || !EventSystem.current.enabled,"UI blocked");
  int today=day.Day;day.NextDay();Check(day.Day==today,"day cannot advance while busy");
  Check(!ForestForagingService.TryStart(settings)&&resources.GetResourceAmount(settings.influence)==15,"second click blocked");
  var stored = JsonUtility.FromJson<ForestForagingService.Result>(Prefs.GetString("foundation.forestForaging"));
  Check(stored.sent==result.sent&&stored.food==result.food&&!stored.completed,"pending outcome saved before countdown ends");
  Time.timeScale=0;
  SceneManager.LoadScene("Base");await Task.Delay(150);
  view=Popup<ForestForagingView>();view.gameObject.SetActive(true);view.Refresh();
  Check(ForestForagingService.Current.food==stored.food&&ForestForagingService.Current.dead==stored.dead,"pending scene reload cannot reroll");
  await Task.Delay(2300);view.Refresh();
  Check(!ForestForagingService.IsPending,"countdown works at time scale zero");
  Time.timeScale=1;
  Check(ForestForagingService.Current.completed,"timer resolves");
  Check(resources.GetResourceAmount(settings.humans)==3-result.humanDeaths&&resources.GetResourceAmount(settings.swordsmen)==3-result.swordsmanDeaths&&resources.GetResourceAmount(settings.archers)==3-result.archerDeaths,"losses removed from correct resource groups");
  Check(resources.GetResourceAmount(settings.food)==result.food,"food awarded once");
  Check(day.GetFoodForecast().Starving==Mathf.Max(0,9-result.dead-result.food),"new starvation forecast");
  Check(view.results.activeSelf&&!view.offer.activeSelf&&!view.timer.activeSelf,"result replaces timer and offer");
  Check(Object.FindAnyObjectByType<EventSystem>().enabled,"UI unblocked");
  Check(!ForestForagingService.CompleteIfReady(settings)&&resources.GetResourceAmount(settings.food)==result.food,"completion cannot award twice");
  view.gameObject.SetActive(false);view.gameObject.SetActive(true);view.Refresh();
  Check(view.results.activeSelf&&!ForestForagingService.CanStart(settings),"cancel and reopen preserves result");
  SceneManager.LoadScene("Base");await Task.Delay(500);
  Check(ForestForagingService.Current.completed&&ForestForagingService.Current.food==result.food&&!ForestForagingService.CanStart(settings),"scene reload preserves daily result");
  day.NextDay();resources.SetResourceAmount(settings.humans,3);resources.SetResourceAmount(settings.swordsmen,0);resources.SetResourceAmount(settings.archers,0);resources.SetResourceAmount(settings.food,0);resources.SetResourceAmount(settings.influence,30);
  Check(ForestForagingService.CanStart(settings),"new day permits another attempt");
  var test=Object.Instantiate(settings);test.duration=.1f;test.survivalChance=0;
  Check(ForestForagingService.TryStart(test),"zero-survival expedition starts");await Task.Delay(200);
  ForestForagingService.CompleteIfReady(test);
  Check(ForestForagingService.Current.dead==3&&resources.GetResourceAmount(settings.humans)==0&&resources.GetResourceAmount(settings.food)==0,"all-dead boundary");
  day.NextDay();resources.SetResourceAmount(settings.humans,3);test.survivalChance=1;
  Check(ForestForagingService.TryStart(test),"all-survivor expedition starts");await Task.Delay(200);ForestForagingService.CompleteIfReady(test);
  Check(ForestForagingService.Current.survived==3&&resources.GetResourceAmount(settings.humans)==3&&resources.GetResourceAmount(settings.food)>=3,"all-survivor boundary");
  Object.Destroy(test);
  File.AppendAllText(Report,"PASSED "+checks+" checks\n");
 }
}
#endif
