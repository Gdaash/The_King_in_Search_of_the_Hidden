#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
public static class CrystalLightValidation
{
 static IEnumerator routine;static List<string> report=new();
 static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);Debug.Log("Crystal validation: "+message);}
 static IEnumerator Wait(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
 static void Tick(){try{if(!EditorApplication.isPlaying)throw new Exception("Play stopped");if(!routine.MoveNext()){EditorApplication.update-=Tick;System.IO.File.WriteAllLines("Temp/CrystalValidation.txt",report);Debug.Log("Crystal validation completed");}}catch(Exception e){EditorApplication.update-=Tick;report.Add("FAIL "+e);System.IO.File.WriteAllLines("Temp/CrystalValidation.txt",report);Debug.LogException(e);}}
 public static void SeedAndPlay(){BuildingUpgradeValidation.Begin();foreach(var id in ScientificUpgrades.Flashlights)SaveSlotPrefs.SetInt(id+"_Purchased",1);SaveSlotPrefs.SetInt("Global_Resource_MagicOre",20);SaveSlotPrefs.Save();}
 public static void Run(){report.Clear();routine=Checks();EditorApplication.update+=Tick;}
 static IEnumerator Checks(){
 var c=UnityEngine.Object.FindFirstObjectByType<WorldFlashlightAvailability>();
 Check(c.CellCount==6,"five existing upgrades give six cells");
 Check(Enumerable.Range(0,6).All(i=>c.State(i)==WorldFlashlightAvailability.CellState.Ready),"all cells start full");
 Check(!c.SelectAt(new Vector2(1000,1000)),"empty ground consumes nothing");
 var hexes=HexLightUnlocker.ActiveInstances.Where(h=>!h.IsUnlocked()&&!h.IsUnlocking()&&h.GetComponentInParent<HexBlocker>()!=null&&!h.GetComponentInParent<HexBlocker>().IsBlocked).OrderBy(h=>h.transform.position.sqrMagnitude).Take(2).ToArray();
 Check(hexes.Length==2,"two selectable closed hexes exist");
 Check(c.SelectAt(hexes[0].transform.position)&&c.SelectAt(hexes[1].transform.position),"two hexes light independently");
 Check(c.Charge(0)==0&&c.Charge(1)==0&&c.State(2)==WorldFlashlightAvailability.CellState.Ready,"only selected cells spent");
 GameSpeedControls.SetSimulationSpeed(0);float remaining=hexes[0].TimeRemaining;
 var wait=Wait(.35f);while(wait.MoveNext())yield return null;
 Check(Mathf.Approximately(remaining,hexes[0].TimeRemaining)&&c.Charge(0)==0,"pause stops opening and recharge");
 GameSpeedControls.SetSimulationSpeed(4);wait=Wait(2);while(wait.MoveNext())yield return null;
 Check(hexes.All(h=>h.IsUnlocked()),"both hexes complete using configured duration");
 Check(c.State(0)==WorldFlashlightAvailability.CellState.Charging&&c.Charge(0)>0&&c.Charge(0)<.2f,"charge resumes after beam is released at 30 seconds per full cell");
 var stone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Stone 3.prefab"),new Vector3(50,0),Quaternion.identity).GetComponent<ResourceRequester>();
 yield return null;
 Check(c.SelectAt(stone.transform.position),"building can reserve a ready cell");
 Check(c.State(2)==WorldFlashlightAvailability.CellState.WaitingForResources&&c.Charge(2)==1,"waiting reserves without spending charge");
 var beam=GameObject.Find("Flashlight (2)").GetComponent<CrystalLightBeam>();
 Check(beam.Flag.gameObject.activeInHierarchy&&beam.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>().All(l=>!l.enabled),"waiting frame is visible with all lights off");
 Check(c.SelectAt(stone.transform.position)&&c.State(2)==WorldFlashlightAvailability.CellState.Ready,"second click cancels waiting");
 foreach(var req in stone.requirements)while(req.currentAmount<req.requiredAmount)stone.DeliverResource(req.resourceType);
 Check(!stone.IsProcessing&&!stone.GetComponentInChildren<TimerController>().IsRunning,"late delivery after cancellation cannot start unpaid production");
 Check(c.SelectAt(stone.transform.position)&&c.State(2)==WorldFlashlightAvailability.CellState.Working&&c.Charge(2)==0,"reselecting retained resources starts and spends exactly once");
 Check(!c.SelectAt(stone.transform.position),"working cycle cannot be restarted or cancelled");
 Check(beam.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>().All(l=>l.enabled),"beam and ground light begin with production");
 GameSpeedControls.SetSimulationSpeed(0);wait=Wait(.2f);while(wait.MoveNext())yield return null;
 Check(c.Charge(2)==0,"occupied cell does not recharge");
 var resources=GlobalResourceManager.Instance;resources.SetResourceAmount(c.RechargeResource,0);
 Check(!c.RechargeAll(),"recharge rejects insufficient ore");resources.SetResourceAmount(c.RechargeResource,20);
 Check(c.RechargeAll()&&resources.GetResourceAmount(c.RechargeResource)==19,"full recharge charges one ore");
 Check(c.Charge(2)==1&&c.State(2)==WorldFlashlightAvailability.CellState.Working,"paid refill preserves active beam reservation");
 Check(!c.RechargeAll()&&resources.GetResourceAmount(c.RechargeResource)==19,"full battery never charges twice");
 GameSpeedControls.SetSimulationSpeed(4);wait=Wait(1.7f);while(wait.MoveNext())yield return null;
 Check(!stone.IsProcessing&&c.State(2)==WorldFlashlightAvailability.CellState.Ready&&!stone.HasLogisticFlag(),"one production cycle releases the refilled cell and flag");
 var mine=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Magic Ore Mine.prefab"),new Vector3(55,0),Quaternion.identity).GetComponentsInChildren<ResourceRequester>().First(x=>x.requirements.Count==3);yield return null;
 Check(c.SelectAt(mine.transform.position),"multi-resource mine can reserve a cell");
 mine.DeliverResource(mine.requirements[0].resourceType);
 Check(!mine.IsProcessing&&c.State(0)==WorldFlashlightAvailability.CellState.WaitingForResources,"partial delivery does not light mine");
 foreach(var req in mine.requirements)while(req.currentAmount<req.requiredAmount)mine.DeliverResource(req.resourceType);
 Check(mine.IsProcessing&&c.State(0)==WorldFlashlightAvailability.CellState.Working,"all mine resources start beam and production together");
 GameSpeedControls.SetSimulationSpeed(1);ScreenCapture.CaptureScreenshot("E:/The_King_in_Search_of_the_Hidden/Temp/crystal-six-cells.png");yield return null;yield return null;
 var escape=UnityEngine.Object.FindFirstObjectByType<WorldEscapeController>();var so=new SerializedObject(escape);((UnityEngine.UI.Button)so.FindProperty("escapeButton").objectReferenceValue).onClick.Invoke();
 Check(c.Escaped&&!c.CanRecharge&&!c.SelectAt(stone.transform.position),"escape disables new activations and recharge");
 Check(UnityEngine.Object.FindObjectsByType<CrystalLightBeam>(FindObjectsInactive.Include,FindObjectsSortMode.None).All(b=>!b.Flag.gameObject.activeInHierarchy),"escape immediately extinguishes every light and flag");
 }
}
#endif
