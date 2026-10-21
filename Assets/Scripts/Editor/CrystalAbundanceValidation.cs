#if UNITY_EDITOR
using System;
using System.Reflection;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEditor;
public static class CrystalAbundanceValidation
{
 static readonly Type Saves=typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
 static readonly BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
 static bool active,wasSuspended;static object memoryEntry;static string oldValue;
 static void Check(bool x,string message){if(!x)throw new Exception(message);}
 static void Memory(int value)=>memoryEntry.GetType().GetField("value").SetValue(memoryEntry,value.ToString());
 public static void Begin(PortalTowerProgression progression)
 {
  wasSuspended=(bool)Saves.GetField("WritesSuspended",Static).GetValue(null);Saves.GetField("WritesSuspended",Static).SetValue(null,true);active=true;
  string key=ScientificUpgrades.CrystalAbundance+"_Purchased";SaveSlotPrefs.GetInt(key,0);
  memoryEntry=Saves.GetMethod("Find",Static).Invoke(null,new object[]{SaveSlotPrefs.SelectedSlot,key});oldValue=(string)memoryEntry.GetType().GetField("value").GetValue(memoryEntry);
  var stats=AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
  typeof(PortalTowerProgression).GetField("scientificStats",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progression,stats);
  var entry=stats.UpgradeTable.Find(ScientificUpgrades.CrystalAbundance);
  Check(entry!=null&&entry.effectValue==1&&entry.icon!=null&&entry.costResource!=null,"Crystal abundance catalog/icon/cost missing");
  Memory(0);Check(stats.ExtraHexCrystals==0&&progression.HexCrystalDropRange==new Vector2Int(1,5),"Base drop range changed");
  Memory(1);Check(stats.ExtraHexCrystals==1&&progression.HexCrystalDropRange==new Vector2Int(2,6),"Purchased upgrade must shift both endpoints by one");
  int before=UnityEngine.Object.FindObjectsByType<ResourceItem>().Length;progression.AwardHexExperience(new Vector3(10,0));int count=UnityEngine.Object.FindObjectsByType<ResourceItem>().Length-before;
  Check(count>=2&&count<=6,"Empty hex drop did not use upgraded range");
  var loc=AssetDatabase.LoadAssetAtPath<GameFoundation.Localization.LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
  foreach(string keySuffix in new[]{"skill."+entry.id+".title","skill."+entry.id+".description","laboratory.group."+entry.GroupId})
  {var text=loc.entries.Find(e=>e.key==keySuffix);Check(text!=null&&text.values[loc.languages.IndexOf("ru")].Length>0&&text.values[loc.languages.IndexOf("en")].Length>0,"Missing RU/EN localization");}
  SessionState.SetString("CrystalAbundanceQAResult","PASS: permanent research ID, icon/cost, RU/EN localization, baseline 1–5, upgraded 2–6 and empty hex ground drops");
 }
 public static void Cleanup()
 {
  if(!active)return;
  if(memoryEntry!=null)memoryEntry.GetType().GetField("value").SetValue(memoryEntry,oldValue);
  Saves.GetMethod("ResetCache",Static)?.Invoke(null,null);Saves.GetField("WritesSuspended",Static).SetValue(null,wasSuspended);active=false;
 }
}
#endif
