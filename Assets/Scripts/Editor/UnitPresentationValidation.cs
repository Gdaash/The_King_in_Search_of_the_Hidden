#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using GameFoundation.Base;
public static class UnitPresentationValidation
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool x,string m){if(!x)throw new Exception(m);}
 public static void Run()
 {
  float speed=Time.timeScale;Time.timeScale=1;
  var job=new GameObject("Worker facing QA job").AddComponent<ResourceRequester>();
  var go=new GameObject("Worker facing QA",typeof(SpriteRenderer));var worker=go.AddComponent<ResourceWorker>();worker.Initialize(null,job,true);
  var update=typeof(ResourceWorker).GetMethod("Update",Flags);
  go.transform.position=new Vector3(-100,0);update.Invoke(worker,null);Check(go.GetComponent<SpriteRenderer>().flipX,"Worker moving right must face right");
  go.transform.position=new Vector3(100,0);update.Invoke(worker,null);Check(!go.GetComponent<SpriteRenderer>().flipX,"Worker moving left must face left");
  UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(job.gameObject);Time.timeScale=speed;
  foreach(string name in new[]{"Octopus","RedArcher"})
  {
   var path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/Prefabs/Units"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/"+name+".prefab"));var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   var bar=asset.GetComponentInChildren<HealthBar>(true);var unit=UnityEngine.Object.Instantiate(asset);var instance=unit.GetComponentInChildren<HealthBar>(true);
   typeof(HealthBar).GetMethod("LateUpdate",Flags).Invoke(instance,null);var expected=Vector3.Scale(bar.transform.localScale,new Vector3(1.1f,1.7f,1));var actual=instance.transform.localScale;
   Check(Mathf.Abs(Mathf.Abs(actual.x)-Mathf.Abs(expected.x))<.00001f&&Mathf.Abs(Mathf.Abs(actual.y)-Mathf.Abs(expected.y))<.00001f,name+": health bar size mismatch");UnityEngine.Object.Destroy(unit);
  }
  foreach(var sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())sprite.enabled=false;
  foreach(var particles in UnityEngine.Object.FindObjectsByType<ParticleSystem>())particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var canvas=new GameObject("Blacksmith layout QA",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  EquipmentUpgradeValidation.Run(canvas.transform);
  SessionState.SetString("UnitPresentationQAResult","PASS: worker faces both movement directions; enemy and archer bars x1.1/y1.7; blacksmith crafting and all 25 upgrades, RU/EN and UI icon sizing");
 }
}
#endif
