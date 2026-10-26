using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFoundation.Saves;
using GameFoundation.Localization;
using GameFoundation.Settings;
using TMPro;
[InitializeOnLoad]
public static class SaveMenuValidation
{
 const string ScenePath="Assets/Editor/SaveMenuQA.unity";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static SaveMenuValidation(){EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool("SaveMenuQA",false))return;if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;if(s==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("SaveMenuPrevious",""));AssetDatabase.DeleteAsset(ScenePath);SessionState.SetBool("SaveMenuQA",false);}};}
 public static void Begin(){if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);SessionState.SetString("SaveMenuPrevious",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetString("SaveMenuResult","RUNNING");SessionState.SetBool("SaveMenuQA",true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;}
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 static T Field<T>(object value,string name)=> (T)value.GetType().GetField(name,Private).GetValue(value);
 static void Run()
 {
  var saves=typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");var writes=saves.GetField("WritesSuspended",BindingFlags.NonPublic|BindingFlags.Static);bool oldWrites=(bool)writes.GetValue(null);writes.SetValue(null,true);
  bool languageExisted=PlayerPrefs.HasKey("foundation.language");string oldLanguage=PlayerPrefs.GetString("foundation.language","");
  try
  {
   var storage=new Dictionary<string,string>{{"foundation.slot.1.created","1"},{"foundation.slot.1.saveData","DATA"},{"foundation.slot.2.created","1"},{"foundation.slot.2.saveData","OTHER"},{"Global_Resource_Human","7"}};
   Action<string,int> set=(key,value)=>storage[key]=value.ToString();Action<string> delete=key=>storage.Remove(key);int flushes=0;Action flush=()=>flushes++;
   saves.GetMethod("EraseSlotStorage",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{1,set,delete,flush});
   Check(!storage.ContainsKey("foundation.slot.1.created")&&!storage.ContainsKey("foundation.slot.1.saveData")&&storage["foundation.slot.1.reset"]=="1","Clear must erase only selected slot and block legacy resurrection");Check(storage["foundation.slot.2.saveData"]=="OTHER"&&storage["Global_Resource_Human"]=="7"&&flushes==1,"Other saves and settings must survive deletion");
   PlayerPrefs.DeleteKey("foundation.language");var language=new GameObject("QA language").AddComponent<LocalizationService>();var table=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset"));language.SetTable(table);
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Screens/Main Menu Screen.prefab"));var menu=root.GetComponentInChildren<MainMenuController>(true);var slots=root.GetComponentInChildren<SaveSlotsPopupView>(true);var first=root.GetComponentsInChildren<LanguageSelectionList>(true).Single(l=>l.firstLaunch);
   Check(first.gameObject.activeInHierarchy,"First launch must show language selection");menu.OpenSlots();Check(!slots.gameObject.activeSelf,"Language choice must precede save selection");Capture(root,slots,"language",false);
   var english=first.container.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMP_Text>()?.text=="English");english.onClick.Invoke();Check(!first.gameObject.activeSelf&&language.Language=="en"&&!LanguageSelectionList.RequiredChoicePending,"Language choice must persist and close automatically");
   menu.OpenSlots();Check(slots.gameObject.activeSelf,"Save selection must open after language choice");
   int occupied=Enumerable.Range(1,3).Count(SaveSlotPrefs.SlotExists);Check(Mathf.Abs(slots.window.rect.height-(436+occupied*100))<.1f,"Save popup must fit visible delete actions");
   for(int i=0;i<3;i++)Check(slots.deleteButtons[i].gameObject.activeSelf==SaveSlotPrefs.SlotExists(i+1),"Delete visibility must follow save existence");
   int existing=Enumerable.Range(1,3).FirstOrDefault(SaveSlotPrefs.SlotExists);
   if(existing>0){slots.RequestDelete(existing);Check(slots.confirmation.activeSelf,"Delete must require confirmation");Capture(root,slots,"delete-confirmation",false);slots.confirmNo.onClick.Invoke();Check(!slots.confirmation.activeSelf&&SaveSlotPrefs.SlotExists(existing),"No must preserve save");}
   slots.closeButton.onClick.Invoke();Check(!slots.gameObject.activeSelf,"Save close button must close popup");menu.OpenSlots();slots.closeButton.onClick.Invoke();Check(!slots.gameObject.activeSelf,"Close must work on repeated opening");
   var settings=Field<SettingsPopupController>(menu,"settingsPopup");menu.OpenSettings();var list=settings.GetComponent<LanguageSelectionList>();Check(list.container.GetComponentsInChildren<Button>().Length==table.languages.Count,"Settings and first launch must use same language list");
   var toggle=Field<Toggle>(settings,"fullscreenToggle");bool old=toggle.isOn;toggle.SetIsOnWithoutNotify(!old);toggle.SetIsOnWithoutNotify(old);Field<Button>(settings,"closeButton").onClick.Invoke();Check(!settings.gameObject.activeSelf,"Close must still work after interacting with a checkbox");
   table.languages.Add("ja");menu.OpenSettings();Check(list.container.GetComponentsInChildren<Button>().Length==table.languages.Count,"New language must automatically appear in settings");settings.Close();
   menu.OpenSlots();Capture(root,slots);slots.closeButton.onClick.Invoke();
   SessionState.SetString("SaveMenuResult","PASS: first-run language gate/choice, automatic extra language, slot layout/delete visibility, delete cancellation, repeated close, settings close after checkbox and isolated storage deletion; real saves not deleted.");
  }
  catch(Exception error){SessionState.SetString("SaveMenuResult","FAIL: "+error);Debug.LogException(error);}
  finally{if(languageExisted)PlayerPrefs.SetString("foundation.language",oldLanguage);else PlayerPrefs.DeleteKey("foundation.language");PlayerPrefs.Save();writes.SetValue(null,oldWrites);EditorApplication.isPlaying=false;}
 }
 static void Capture(GameObject root,SaveSlotsPopupView slots,string file="slots",bool verifyClose=true)
 {
  var camera=new GameObject("QA camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.03f,.06f);var rt=new RenderTexture(1440,1080,24);camera.targetTexture=rt;
  var canvas=root.GetComponentInChildren<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();
  if(verifyClose){var eventSystem=new GameObject("QA events").AddComponent<EventSystem>();var pointer=new PointerEventData(eventSystem){position=RectTransformUtility.WorldToScreenPoint(camera,slots.closeButton.transform.TransformPoint(((RectTransform)slots.closeButton.transform).rect.center))};var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==slots.closeButton,"Close button must be topmost actual pointer hit: "+string.Join(",",hits.Select(h=>h.gameObject.name)));UnityEngine.Object.Destroy(eventSystem.gameObject);}
  var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1440,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1080),0,0);image.Apply();System.IO.Directory.CreateDirectory("Art/SaveMenuQA");System.IO.File.WriteAllBytes("Art/SaveMenuQA/"+file+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(camera.gameObject);
 }
}



