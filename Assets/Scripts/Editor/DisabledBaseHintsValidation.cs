using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFoundation.Base;
using GameFoundation.Localization;

[InitializeOnLoad]
public static class DisabledBaseHintsValidation
{
 const string Path="Assets/Editor/DisabledBaseHintsQA.unity";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static DisabledBaseHintsValidation(){EditorApplication.playModeStateChanged+=state=>{
  if(!SessionState.GetBool("BaseHintsQA",false))return;
  if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;
  if(state==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("BaseHintsPrevious",""));AssetDatabase.DeleteAsset(Path);SessionState.SetBool("BaseHintsQA",false);}
 };}
 public static void Begin()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop current game first");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SaveScene(scene,Path);EditorSceneManager.CloseScene(scene,true);
  SessionState.SetString("BaseHintsPrevious",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool("BaseHintsQA",true);SessionState.SetString("BaseHintsResult","RUNNING");EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);EditorApplication.isPlaying=true;
 }
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Run()
 {
  var saves=typeof(GameFoundation.Saves.SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");var writes=saves.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic);bool previous=(bool)writes.GetValue(null);writes.SetValue(null,true);
  try
  {
   var language=new GameObject("QA language").AddComponent<LocalizationService>();var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");language.SetTable(table);
   var canvas=new GameObject("QA canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
   var tooltip=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip.prefab"),canvas.transform).GetComponent<TooltipManager>();
   var label=(Text)typeof(TooltipManager).GetField("tooltipText",Private).GetValue(tooltip);
   var resources=new GameObject("QA resources").AddComponent<GlobalResourceManager>();
   var popup=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Blacksmith Popup.prefab"),canvas.transform);popup.SetActive(true);
   var production=popup.GetComponent<BlacksmithProductionView>();var recipe=typeof(BlacksmithProductionView).GetField("swordRecipe",Private).GetValue(production);
   var recipeType=recipe.GetType();var input=(ResourceType)recipeType.GetField("input").GetValue(recipe);var button=(Button)recipeType.GetField("produceButton").GetValue(recipe);
   resources.SetResourceAmount(input,0);Check(!button.interactable,"No resources must disable crafting");
   var hint=button.GetComponent<BaseDisabledButtonHint>();Check(hint!=null,"Crafting did not attach its hint");
   foreach(string code in new[]{"ru","en"})
   {
    typeof(LocalizationService).GetField("<Language>k__BackingField",Private).SetValue(language,code);
    foreach(BaseDisabledButtonHint.Reason reason in Enum.GetValues(typeof(BaseDisabledButtonHint.Reason)))Check(table.Get(BaseDisabledButtonHint.Key(reason),code)!=BaseDisabledButtonHint.Key(reason),"Missing translation "+reason+" "+code);
    ExecuteEvents.Execute<IPointerEnterHandler>(button.gameObject,new PointerEventData(null),ExecuteEvents.pointerEnterHandler);
    Check(label.text==table.Get(BaseDisabledButtonHint.Key(BaseDisabledButtonHint.Reason.Resources),code),"Disabled crafting hover is not localized");
    BaseDisabledButtonHint.Set(button,BaseDisabledButtonHint.Reason.Housing);hint.OnPointerEnter(null);Check(label.text==table.Get(BaseDisabledButtonHint.Key(BaseDisabledButtonHint.Reason.Housing),code),"Housing reason not displayed");
    BaseDisabledButtonHint.Set(button,BaseDisabledButtonHint.Reason.Resources);hint.OnPointerExit(null);
   }
   resources.SetResourceAmount(input,100);Check(button.interactable,"Crafting must unlock after resource delivery");
   hint.OnPointerEnter(null);Check(typeof(TooltipManager).GetField("_currentTarget",Private).GetValue(tooltip)==null,"Available action must not display a blocked hint");
   resources.SetResourceAmount(input,0);hint.OnPointerEnter(null);popup.SetActive(false);Check(typeof(TooltipManager).GetField("_currentTarget",Private).GetValue(tooltip)==null,"Closing a popup must hide its hint");
   SessionState.SetString("BaseHintsResult","PASS: actual blacksmith disabled hover, 16 RU/EN reasons, housing message, resource-driven unlock, and popup-close cleanup; saves untouched.");
  }
  catch(Exception error){SessionState.SetString("BaseHintsResult","FAIL: "+error);Debug.LogException(error);}
  finally{writes.SetValue(null,previous);EditorApplication.isPlaying=false;}
 }
}
